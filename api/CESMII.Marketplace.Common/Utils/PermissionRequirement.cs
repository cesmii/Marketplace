namespace CESMII.Marketplace.Common.Utils
{
    using Microsoft.AspNetCore.Authorization;

    using CESMII.Marketplace.Common.Enums;

    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(PermissionEnum permission)
        {
            Permission = permission;
        }

        public PermissionEnum Permission { get; }
    }
}
