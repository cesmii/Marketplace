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
        /// This is a simplified form of the user which contains basic 
        /// info to use within the checkout process. 
        /// If user is a guest user, user.ID and user.Organization.id will be null
        /// </summary>
        public UserCheckoutModel CheckoutUser { get; set; }

        public bool UseCredits { get; set; } = false;
        
        /// <summary>
        /// This is set in the StripeService.DoCheckout. 
        /// We calc and apply here. Then in the oncheckout complete hook we
        /// update the credits used amount only once the transaction completes.
        /// </summary>
        public long? CreditsApplied { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? Updated { get; set; }
        public bool IsActive { get; set; }

        public DateTime? Completed { get; set; }

        public string ReturnUrl { get; set; }
        public string SessionId { get; set; }

        public virtual List<CartItemModel> Items { get; set; }
    }
}
