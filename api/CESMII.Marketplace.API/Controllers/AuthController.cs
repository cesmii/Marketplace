using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.Marketplace.Common;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Api.Shared.Extensions;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize, Route("api/[controller]")]
    public class AuthController : BaseController<AuthController>
    {
        private OrganizationDAL _dalOrganization;

        public AuthController(UserDAL dal, OrganizationDAL dalOrg, ConfigUtil config, ILogger<AuthController> logger) 
            : base(config, logger, dal)
        {
            _dalOrganization = dalOrg;
        }

        [HttpPost, Route("onAADLogin")]
        public IActionResult OnAADLogin()
        {
            //extract user name from identity passed in via token
            //check if that user record is in DB. If not, add it.
            //InitLocalUser: this property checks for user, adds to db and returns a fully formed user model if one does not exist. 
            var userInfo = InitLocalUser();
            UserModel user = userInfo.Item1;
            String strError = userInfo.Item2;

            if (user != null)
            {
                return Ok(new ResultMessageModel() { IsSuccess = true, Message = $"On AAD Login, marketplace user {user.ObjectIdAAD} was initialized." });
            }
            else
            {
                return StatusCode(401, new ResultMessageModel() { IsSuccess = false, Message = strError });
            }
        }


        /// <summary>
        /// On successful Azure AD login, front end will call this to initialize the user in our DB (if not already there).
        /// Once this happens, then subsequent calls will expect user record is already and just ask for id. We won't have multiple 
        /// parallel calls trying to create user locally.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected (UserModel, string) InitLocalUser()
        {
            bool bCheckOrganization = false;
            bool bUpdateUser = false;
            bool bFound = false;
            bool bErrorCondition = false;
            string strError = null;

            UserModel um = null;

            // Extract user name from identity passed in via token
            var userAAD = User.GetUserAAD();

            // Check if that user record is in DB. 
            // If not, check whether Sssu has already created a user record for this user.
            var matches = _dalUser.Where(x => x.ObjectIdAAD != null && x.ObjectIdAAD.ToLower().Equals(userAAD.ObjectIdAAD), null, null).Data;
            if (matches.Count == 1)
            {
                um = matches[0];
                um.Email = userAAD.Email;
                um.DisplayName = userAAD.DisplayName;
                um.LastLogin = DateTime.UtcNow;

                bUpdateUser = true;         // Synch UserModel changes
                bCheckOrganization = true;  // Check the user's organization.
                bFound = true;              // No need for lookup by email address.
            }
            else if (matches.Count > 1)
            {
                bErrorCondition = true;
                strError = $"InitLocalUser||More than one Marketplace AAD record found with Object Id={userAAD.ObjectIdAAD}.";
                _logger.LogWarning(strError);
            }

            if (bErrorCondition)
                return (null, strError);

            // We didn't find user by Azure object id (oid), so let's try finding them by email address.
            if (!bFound)
            {
                // Is there a MongoDB record (document) for the user's email address?
                // ObjectID could be null when record added by Self-Service Sign-Up API connector.
                var listMatchEmailAddress = _dalUser.Where(x => x.Email.ToLower().Equals(userAAD.Email.ToLower()) && x.ObjectIdAAD == null, null).Data;
                if (listMatchEmailAddress.Count == 0)
                {
                    // Here for (1) manually created users, or (2) users did self-service sign-up from within Profile Designer.
                    um = new UserModel()
                    {
                        ObjectIdAAD = userAAD.ObjectIdAAD,
                        Email = userAAD.Email,
                        DisplayName = userAAD.DisplayName,
                        LastLogin = DateTime.UtcNow
                    };
                    um.ID = _dalUser.Add(um).Result;
                    um = _dalUser.GetById(um.ID);

                    bUpdateUser = false;         // No need to synch - we just wrote all we know about.
                    bCheckOrganization = true;   // Check the user's organization.
                }
                else if (listMatchEmailAddress.Count == 1)
                {
                    // For Marketplace self-service sign-up users, first time signing into Marketplace.
                    um = listMatchEmailAddress[0];
                    if (um.ObjectIdAAD == null)
                    {
                        um.ObjectIdAAD = userAAD.ObjectIdAAD;
                        um.Email = userAAD.Email;
                        um.DisplayName = userAAD.DisplayName;
                        um.LastLogin = DateTime.UtcNow;

                        bUpdateUser = true;         // Synch UserModel changes
                        bCheckOrganization = true;  // Check the user's organization.
                    }
                    else
                    {
                        bErrorCondition = true;
                        strError = $"InitLocalUser||Initialized Marketplace record found with email {userAAD.Email}. {listMatchEmailAddress.Count} records found. Existing object id = {um.ObjectIdAAD}";
                        _logger.LogWarning(strError);
                    }
                }
                else
                {
                    // When more than 1 record, it means they have signed up (and then left) more than 
                    // once. This is okay, but we pick the most recent one.
                    // listMatchEmailAddress.Sort((em1, em2) => DateTime?.Compare(em1.LastLogin, em2.LastLogin));
                    listMatchEmailAddress.Sort((em1, em2) =>
                    {
                        DateTime dt1 = new DateTime(em1.LastLogin.Value.Ticks);
                        DateTime dt2 = new DateTime(em2.LastLogin.Value.Ticks);
                        return DateTime.Compare(dt1, dt2);
                    });

                    int iItem = listMatchEmailAddress.Count - 1;
                    um = listMatchEmailAddress[iItem];
                    if (um.ObjectIdAAD == null)
                    {
                        um.ObjectIdAAD = userAAD.ObjectIdAAD;
                        um.Email = userAAD.Email;
                        um.DisplayName = userAAD.DisplayName;
                        um.LastLogin = DateTime.UtcNow;

                        bUpdateUser = true;         // Synch UserModel changes
                        bCheckOrganization = true;  // Check the user's organization.
                    }
                    else
                    {
                        bErrorCondition = true;
                        strError = $"InitLocalUser||More than one Marketplace user record found with email {userAAD.Email}. {listMatchEmailAddress.Count} records found. Existing object id = {um.ObjectIdAAD}";
                        _logger.LogWarning(strError);
                    }
                }
            }

            if (bErrorCondition)
                return (null, strError);

            // Check organzation and update it if needed.
            if (bCheckOrganization)
            {
                if (um.Organization == null && um.SelfServiceSignUp_Organization_Name != null)
                {
                    // Name to search for
                    string strFindOrgName = um.SelfServiceSignUp_Organization_Name;

                    // Search for organization
                    var listMatchOrganizationName = _dalOrganization.Where(x => x.Name.ToLower().Equals(strFindOrgName.ToLower()));
                    if (listMatchOrganizationName.Count == 0)
                    {
                        // Nothing in public.organization? Create a new record.
                        OrganizationModel om = new OrganizationModel()
                        {
                            Name = strFindOrgName
                        };

                        var idNewOrg = _dalOrganization.Add(om, null).Result;
                        om = _dalOrganization.GetById(idNewOrg);
                        um.Organization = om;
                        bUpdateUser = true;         // Synch UserModel changes
                    }
                    else if (listMatchOrganizationName.Count == 1)
                    {
                        // Found? Assign it.
                        um.Organization = listMatchOrganizationName[0];
                        bUpdateUser = true;         // Synch UserModel changes
                    }
                    else
                    {
                        // More than one -- oops. A problem.
                        bErrorCondition = true;
                        strError = $"InitLocalUser||More than one organization record found with Name = {strFindOrgName}. {listMatchOrganizationName.Count} records found.";
                        _logger.LogWarning(strError);
                    }
                }
            }

            if (bErrorCondition)
                return (null, strError);

            if (bUpdateUser)
            {
                _dalUser.Update(um, userAAD.ObjectIdAAD).Wait();
            }

            return (um,strError);

        }
 
    }
}
