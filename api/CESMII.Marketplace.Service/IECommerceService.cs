using Stripe;
using Stripe.Checkout;

using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Service.Models;

namespace CESMII.Marketplace.Service
{
    //TBD - update this to reflect calls to the Stripe API as well as the Cart DAL
    public interface IECommerceService<TModel> where TModel : CartModel
    {
        /// <summary>
        /// Initiate the checkout flow with Stripe
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<CheckoutInitModel> DoCheckout(TModel item, string userId);

        Task<Session> SessionStatus(string session_id);

        /// <summary>
        /// Get all products from the Stripe product catalog.
        /// This is called after user saves marketplace item from admin screen when the marketplace item 
        /// does not have a ProductPaymentId
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<IEnumerable<Product>> GetProducts();

        /// <summary>
        /// Get a product from the Stripe product catalog.
        /// This is called after user saves marketplace item from admin screen when the marketplace item 
        /// does not have a ProductPaymentId
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<Product> GetProduct(string paymentProductId);

        /// <summary>
        /// Create the product in the Stripe product catalog.
        /// This is called after user saves marketplace item from admin screen when the marketplace item 
        /// does not have a ProductPaymentId
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<Product> CreateProduct(MarketplaceItemModel item);

        /// <summary>
        /// Delete the product in the Stripe product catalog.
        /// This is called after user saves marketplace item from admin screen when the marketplace item 
        /// does not have a ProductPaymentId
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<bool> DeleteProduct(string paymentProductId);

        /// <summary>
        /// Get all products from the Stripe product catalog.
        /// This is called after user saves marketplace item from admin screen when the marketplace item 
        /// does not have a ProductPaymentId
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<IEnumerable<Price>> GetPrices();

        /// <summary>
        /// Add the product in the Stripe product catalog.
        /// This is called after user saves marketplace item from admin screen when the marketplace item 
        /// does not have a ProductPaymentId
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<bool> AddProduct(MarketplaceItemModel item, string userId);

        /// <summary>
        /// Update the product in the Stripe product catalog.
        /// This is called after user saves marketplace item from admin screen. 
        /// This should only be called if the item has a ProductPaymentId (ie a Stripe product id) to be used to map the catalog.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<bool> UpdateProduct(MarketplaceItemModel item, string userId);

        /// <summary>
        /// Update the product in the Stripe product catalog.
        /// This is called after user saves marketplace item from admin screen. 
        /// This should only be called if the item has a ProductPaymentId (ie a Stripe product id) to be used to map the catalog.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<Price> DeletePrice(Price item);

        /// <summary>
        /// Add or update all products from the marketplace catalog to the Stripe product catalog.
        /// This is called by a button click from the admin front end. 
        /// Note if a marketplace item has do not sell flag, check the Stripe catalog and remove that item.
        /// The marketplace item will have a Stripe product id to be used to map the catalog.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<bool> UpdateAllProducts(MarketplaceItemModel item, string userId);

        /// <summary>
        /// Get cart
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        TModel GetById(string id);
        /// <summary>
        /// Add cart
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<string> Add(TModel item, string userId);
        /// <summary>
        /// Update cart
        /// </summary>
        /// <param name="item"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<int> Update(TModel item, string userId);
        /// <summary>
        /// Delete cart
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task Delete(string id, string userId);
    }

}
