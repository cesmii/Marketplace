using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.DAL.Models
{
    public class CartItemModel 
    {
        public MarketplaceItemCheckoutModel MarketplaceItem { get; set; }

        /// <summary>
        /// The info of the price id being used for this purchase. 
        /// This is an object we maintain on the marketplace item and is 
        /// selected by the user during add to cart process.
        /// </summary>
        public ProductPrice SelectedPrice { get; set; }

        public int Quantity { get; set; }

        /// <summary>
        /// Calculated Credits at the time it was added to the cart
        /// </summary>
        /// <remarks>Purchase may be a combination of credits and price</remarks>
        [System.Obsolete("This will go away in favor of a bool to use or not use credits.")]
        public int? Credits { get; set; }
    }
}
