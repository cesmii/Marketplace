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

        public AuthController(UserDAL dal, ConfigUtil config, ILogger<AuthController> logger) 
            : base(config, logger, dal)
        {
        }

        [HttpPost, Route("onAADLogin")]
        public IActionResult OnAADLogin()
        {
            //extract user name from identity passed in via token
            //check if that user record is in DB. If not, add it.
            //InitLocalUser: this property checks for user, adds to db and returns a fully formed user model if one does not exist. 
            var user = InitLocalUser(); 
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = $"On AAD Login, marketplace user {user.ObjectIdAAD} was initialized." });
        }


        /// <summary>
        /// On successful Azure AD login, front end will call this to initialize the user in our DB (if not already there).
        /// Once this happens, then subsequent calls will expect user record is already and just ask for id. We won't have multiple 
        /// parallel calls trying to create user locally.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected UserModel InitLocalUser()
        {
            UserModel result = null;

            //extract user name from identity passed in via token
            //check if that user record is in DB. If not, add it.
            var userAAD = User.GetUserAAD();
            var matches = _dalUser.Where(x => x.ObjectIdAAD.ToLower().Equals(userAAD.ObjectIdAAD), null, null).Data;
            switch (matches.Count)
            {
                case 1:
                    result = matches[0];
                    result.LastLogin = DateTime.UtcNow;
                    result.DisplayName = userAAD.DisplayName;
                    _dalUser.Update(matches[0], userAAD.ObjectIdAAD).Wait();
                    break;
                case 0:
                    result = new UserModel()
                    {
                        ObjectIdAAD = userAAD.ObjectIdAAD,
                        DisplayName = userAAD.DisplayName,
                        LastLogin = DateTime.UtcNow
                    };
                    result.ID = _dalUser.Add(result, null).Result;
                    break;
                default:
                    _logger.LogWarning($"InitLocalUser||More than one Profile designer user record found with user name {userAAD.ObjectIdAAD}.");
                    throw new ArgumentNullException($"InitLocalUser: More than one Profile designer record user found with user name {userAAD.ObjectIdAAD}.");
            }

            return result;

        }
 
    }
}
