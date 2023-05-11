namespace CESMII.Marketplace.Api.Shared.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.Common.Utils;
    using CESMII.Marketplace.DAL;
    using CESMII.Marketplace.DAL.Models;

    public class UserAzureADMapping
    {
        private readonly RequestDelegate _next;

        public UserAzureADMapping(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILogger<UserAzureADMapping> logger, UserDAL dalUser)
        {
            //if authenticated (by Azure AD) and if we can get the user's local app id, then we add the claim
            //downstream - on login, the local app id is created on endpoint called after onLogin.
            //downstream - the id set here is retrieved and used throughout app as we relate our data to a locally
            //maintained user id. In that table, there is an Azure object id which maps us to Azure.
            if (context.User != null && context.User.Identity.IsAuthenticated)
            {
                var u = GetAppUser(context.User, logger, dalUser);

                if (u != null)
                {
                    var permission = EnumUtils.GetEnumDescription(PermissionEnum.UserAzureADMapped);
                    string strOrg = (u.Organization == null) ? "" :
                                    (u.Organization.Name == null) ? "" :
                                    u.Organization.Name.ToString();
                    string jsonSMIP = u.SmipSettings == null ? null : JsonConvert.SerializeObject(u.SmipSettings);

                    var claims = new List<Claim>()
                    {
                        new Claim($"{permission}", u.ID.ToString()),        // key = UserAzureADMapped
                        new Claim($"{permission}_org", strOrg),             // key = UserAzureADMapped_org
                        new Claim($"{permission}_smipsettings", jsonSMIP)   // key = UserAzureADMapped_org
                    };

                    var appIdentity = new ClaimsIdentity(claims);
                    context.User.AddIdentity(appIdentity);
                }
            }

            await _next(context);
        }


        protected UserModel GetAppUser(ClaimsPrincipal user, ILogger<UserAzureADMapping> logger, UserDAL dalUser)
        {
            //Get Object id from user.identity. Then try and lookup in the local db to get the local id associated with 
            //that Azure object id. If not present yet, the onAADLogin endpoint will add it there. 
            var oId = user.GetUserIdAAD();

            // ObjectID could be null when record added by Self-Service Sign-Up API connector.
            var matches = dalUser.Where(x => x.ObjectIdAAD != null && x.ObjectIdAAD.ToLower().Equals(oId), null, null).Data;
            switch (matches.Count)
            {
                case 1:
                    return matches[0];
                case 0:
                    return null;
                default:
                    string strMessage = $"GetAppUserId||More than one user document record found with Azuer Object Id = {oId}.";
                    logger.LogWarning(strMessage);
                    throw new InvalidOperationException(strMessage);
            }
        }
    }



}