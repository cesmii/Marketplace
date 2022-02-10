namespace CESMII.Marketplace.Data.Entities
{
    using CESMII.Marketplace.Common.Enums;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Permission : AbstractEntity 
    {
        public string Name { get; set; }

        public string CodeName { get; set; }

        public string Description { get; set; }

        public PermissionEnum PermissionEnum { get; set; }
        
    }
}