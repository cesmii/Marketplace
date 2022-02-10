﻿namespace CESMII.Marketplace.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class UserModel : AbstractModel
    {
        //in base class
        //public int ID { get; set; }

        [Required(ErrorMessage = "Required")]
        [RegularExpression(@"^\S*$", ErrorMessage = "No spaces allowed")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Required")]
        //[RegularExpression(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", ErrorMessage = "Invalid format")]
        [EmailAddress(ErrorMessage = "Invalid Format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Required")]
        public string LastName { get; set; }

        public string FullName {
            get {
                if (string.IsNullOrEmpty(this.FirstName)) return this.LastName;
                if (string.IsNullOrEmpty(this.LastName)) return this.FirstName;
                return $"{this.FirstName} {this.LastName}";
            }
        }

        public bool IsActive { get; set; }

        public DateTime Created { get; set; }

        public DateTime? LastLogin { get; set; }

        public DateTime? RegistrationComplete { get; set; }

        /// <summary>
        /// This list of permission names is used in the sign in manager to assign claims
        /// </summary>
        public List<string> PermissionNames { get; set; }

        /// <summary>
        /// This list of permission ids is used in the multi-select of the admin user edit
        /// </summary>
        public List<string> PermissionIds { get; set; }

        public OrganizationModel Organization { get; set; }

    }

    public class UserSimpleModel : AbstractModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(this.FirstName)) return this.LastName;
                if (string.IsNullOrEmpty(this.LastName)) return this.FirstName;
                return $"{this.FirstName} {this.LastName}";
            }
        }
        public OrganizationModel Organization { get; set; }
    }
}