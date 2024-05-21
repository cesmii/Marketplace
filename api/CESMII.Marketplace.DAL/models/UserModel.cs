﻿namespace CESMII.Marketplace.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class UserModel : AbstractModel
    {
        //in base class
        //public int ID { get; set; }

        /// <summary>
        /// Stored in the marketplace db. 
        /// Assume this is unique and won't change for a user. 
        /// populated by evaluating the claims passed in with token
        /// </summary>
        [Required(ErrorMessage = "Required")]
        [RegularExpression(@"^\S*$", ErrorMessage = "No spaces allowed")]
        public string ObjectIdAAD { get; set; }

        /// <summary>
        /// Not stored in the marketplace db. 
        /// Assume this could change so don't use as a mapping key.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Not stored in the marketplace db. 
        /// Only populated by evaluating the claims passed in with token
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Not stored in the marketplace db. 
        /// Only populated by evaluating the claims passed in with token
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Not stored in the marketplace db. 
        /// Only populated by evaluating the claims passed in with token
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Not stored in the marketplace db. 
        /// Only populated by evaluating the claims passed in with token
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Not stored in the marketplace db. 
        /// Only populated by evaluating the claims passed in with token
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Not stored in the marketplace db. 
        /// Only populated by evaluating the claims passed in with token
        /// TBD - enhance this to get a list of permission enums
        /// </summary>
        public string Roles { get; set; }

        /// <summary>
        /// Not stored in the marketplace db. 
        /// Only populated by evaluating the claims passed in with token
        /// </summary>
        public string Scope { get; set; }

        public DateTime? Created { get; set; }

        public DateTime? LastLogin { get; set; }

        public OrganizationModel Organization { get; set; }

        public Data.Entities.SmipSettings SmipSettings { get; set; }
        public string? SelfServiceSignUp_Organization_Name { get; set; }
        public bool? SelfServiceSignUp_IsCesmiiMember { get; set; }
    }

    public class UserSimpleModel : AbstractModel
    {
        public string UserIdAAD { get; set; }

        public OrganizationModel Organization { get; set; }
    }

    public class GuestUserCheckoutModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string OrganizationName { get; set; }
    }
}