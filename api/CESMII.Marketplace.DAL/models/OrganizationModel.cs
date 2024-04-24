namespace CESMII.Marketplace.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class OrganizationModel : AbstractModel
    {
        [Required(ErrorMessage = "Required")]
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public long Credits { get; set; } = 0;
    }
}