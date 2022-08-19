namespace CESMII.Marketplace.Api.Shared.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;

    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.Common.Utils;
    using CESMII.Marketplace.DAL.Models;

    public static class UserExtension
    {
        public static bool HasPermission(this ClaimsPrincipal user, PermissionEnum permission)
        {
            return user.HasClaim(ClaimTypes.Role, EnumUtils.GetEnumDescription(permission));
        }

        /*
        /// <summary>
        /// If the user has any permission considered an admin permission, then return true.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool HasAdminPermission(this ClaimsPrincipal user)
        {
            //TODO: Update enum permissions to match what is coming from AAD
            if (user.HasClaim(ClaimTypes.Role, PermissionEnum.CanManageMarketplace.ToString())) return true;
            if (user.HasClaim(ClaimTypes.Role, PermissionEnum.CanManageSystemSettings.ToString())) return true;
            if (user.HasClaim(ClaimTypes.Role, PermissionEnum.CanManageUsers.ToString())) return true;
            return false;
        }

        // Boolean to determine if a user is currently impersonating another user. False if cannot parse/find.
        public static bool IsImpersonating(this ClaimsPrincipal user)
        {
            return user.HasClaim(CustomClaimTypes.IsImpersonating, true.ToString());
        }

        public static string ImpersonationTargetUserID(this ClaimsPrincipal user)
        {
            // Only attempt to parse the target user id if the user has the is impersonation claim.
            if (user.IsImpersonating())
            {
                // Attempt to parse and return if successful.
                return user.FindFirst(CustomClaimTypes.TargetUserID).Value;
            }

            // Otherwise return null.
            return null;
        }
        */

        public static UserModel GetUserAAD(this ClaimsPrincipal user)
        {
            var result = new UserModel()
            {
                ObjectIdAAD = user.FindFirst(x => x.Type.Contains("objectidentifier")).Value,
                UserName = user.FindFirst(ClaimTypes.Name).Value,
                LastName = user.FindFirst(ClaimTypes.Surname).Value,
                FirstName = user.FindFirst(ClaimTypes.GivenName).Value,
                DisplayName = user.FindFirst(x => x.Type.Equals("name")).Value,
                Email = user.FindFirst(ClaimTypes.Email) == null ?
                         user.FindFirst(ClaimTypes.Upn).Value : //user principle name
                         user.FindFirst(ClaimTypes.Email).Value,
                TenantId = user.FindFirst(x => x.Type.Contains("tenantid")).Value,
                Roles = string.Join(", ", user.FindAll(x => x.Type.Contains("role")).Select(x => x.Value).ToArray()),
                Scope = user.FindFirst(x => x.Type.Contains("scope")).Value
            };
            return result;
        }

        public static string GetUserIdAAD(this ClaimsPrincipal user)
        {
            return user.FindFirst(x => x.Type.Contains("objectidentifier")).Value;
        }

        /*
        [System.Obsolete("GetUserID is obsolete", true)]
        public static string GetUserID(this ClaimsPrincipal user)
        {
            if (user.IsImpersonating())
            {
                return user.ImpersonationTargetUserID();
            }

            // Value cannot be null if user is authorized. If it is null, let it error occur as this would be a serious problem.
            //TBD - why is user.Identity.Name == null, There are claims but this is not transferring over to the expected identity.name as the user's id 
            return user.FindFirst(ClaimTypes.Sid).Value;
        }
        */

        /*
        /// <summary>
        /// This method allows for a simple access to the real user's ID regardless of impersonation.
        /// </summary>
        /// <param name="user">The User</param>
        /// <returns>The real, non-impersonated UserID.</returns>
        public static string GetRealUserID(this ClaimsPrincipal user)
        {
            //TBD - why is user.Identity.Name == null, There are claims but this is not transferring over to the expected identity.name as the user's id 
            //return int.Parse(user.Identity.Name);
            return user.FindFirst(ClaimTypes.Sid).Value;
        }
        */
    }
}
