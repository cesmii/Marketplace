namespace CESMII.Marketplace.DAL.Models
{
    public class CartItemModel 
    {
        public MarketplaceItemCheckoutModel MarketplaceItem { get; set; }

        /// <summary>
        /// Product identifier in Stripe
        /// </summary>
        public int Quantity { get; set; }
        
        /// <summary>
        /// Calculated Credits at the time it was added to the cart
        /// </summary>
        /// <remarks>Purchase may be a combination of credits and price</remarks>
        public int Credits { get; set; }
    }
}
