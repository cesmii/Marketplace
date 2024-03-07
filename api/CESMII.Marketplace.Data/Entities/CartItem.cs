using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CESMII.Marketplace.Data.Entities
{
    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// This is nested within the cart document in the cart collection.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class CartItem
    {
        public BsonObjectId MarketplaceItemId { get; set; }
        /// <summary>
        /// Product identifier in Stripe
        /// </summary>
        public string StripeId { get; set; }
        public int Quantity { get; set; }
        /// <summary>
        /// Calculated Price at the time it was added to the cart
        /// </summary>
        /// <remarks>Purchase may be a combination of credits and price</remarks>
        public decimal Price { get; set; }
        /// <summary>
        /// Calculated Credits at the time it was added to the cart
        /// </summary>
        /// <remarks>Purchase may be a combination of credits and price</remarks>
        public int Credits { get; set; }


    }

}
