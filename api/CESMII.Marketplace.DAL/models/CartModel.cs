namespace CESMII.Marketplace.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.Common.Models;

    public class CartModel : AbstractModel
    {
        public string Name { get; set; }

        public CartStatusEnum Status { get; set; } = CartStatusEnum.Pending;

        public string CreatedById { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime Created { get; set; }

        public string UpdatedById { get; set; }

        /// <summary>
        /// This will be populated if user decides to pursue checkout as a guest.
        /// </summary>
        public GuestUserModel GuestUser { get; set; }

        public bool UseCredits { get; set; } = false;

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? Updated { get; set; }
        public bool IsActive { get; set; }

        public DateTime? Completed { get; set; }

        public string ReturnUrl { get; set; }
        public string SessionId { get; set; }
        public string OraganizationId { get; set; }

        public virtual List<CartItemModel> Items { get; set; }
    }
}
