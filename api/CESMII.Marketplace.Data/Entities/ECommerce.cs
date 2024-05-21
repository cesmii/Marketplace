namespace CESMII.Marketplace.Data.Entities
{
    using System.Collections.Generic;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// This is nested within MarketplaceItem
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ECommerce
    {
        public bool AllowPurchase { get; set; } = false;
        public string PaymentProductId { get; set; }
        public List<ProductPrice> Prices { get; set; } = new List<ProductPrice>();
        /// <summary>
        /// This is optional. This would be stuff to display that should inform purchaser of any introductory
        /// instructions. This would appear below the item heading and before the priding info in the cart item.
        /// </summary>
        public string PurchaseInstructions { get; set; }

        /// <summary>
        /// This is optional. This would be stuff to display that should inform purchaser of additional
        /// constraints. This would appear after the item in the cart item.
        /// </summary>
        public string FinePrint { get; set; }

        public string TermsOfService { get; set; }
        public bool TermsOfServiceIsRequired { get; set; } = false;

        public string OnCheckoutCompleteJobId { get; set; }
    }

    /// <summary>
    /// This contains the structure to use for prices to a marketplace item. 
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ProductPrice
    {
        public long Amount { get; set; }
        public string BillingPeriod { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public string PriceId { get; set; }
    }
}