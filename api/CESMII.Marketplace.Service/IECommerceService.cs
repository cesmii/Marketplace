using Stripe;

using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Service.Models;
using Stripe.FinancialConnections;

namespace CESMII.Marketplace.Service
{
    //TBD - update this to reflect calls to the Stripe API as well as the Cart DAL
    public interface IECommerceService<TModel> where TModel : CartModel
    {
        /// <summary>
        /// Stripe will publish events to Weebhook
        /// </summary>
        /// <param name="json"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        Task<bool> StripeWebhook(string json, string header);

        /// <summary>
        /// Initiate the checkout flow with Stripe
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<CheckoutInitModel> DoCheckout(TModel item, UserModel user);

        /// <summary>
        /// Get checkout status from Stripe
        /// </summary>
        /// <returns></returns>
        Task<CheckoutStatusModel> GetCheckoutStatus(string sessionId);

        /// <summary>
        /// Initiate the checkout flow with Stripe - for anonymous user
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<CheckoutInitModel> DoCheckoutAnonymous(TModel item);

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
        Task<Product> AddProduct(AdminMarketplaceItemModel item, string userId);

        /// <summary>
        /// Update the product in the Stripe product catalog.
        /// This is called after user saves marketplace item from admin screen. 
        /// This should only be called if the item has a ProductPaymentId (ie a Stripe product id) to be used to map the catalog.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<Product> UpdateProduct(AdminMarketplaceItemModel item, string userId);

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
        /// Get cart
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        CartModel? GetByUserId(string userId);

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

        /// <summary>
        /// Get all payments from the Stripe.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<IEnumerable<PaymentIntent>> GetPayments();

        /// <summary>
        /// Get payment by id from the Stripe.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<PaymentIntent> GetPaymentById(string paymentId);

        /// <summary>
        /// Get all payment methods from the Stripe.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<StripeList<PaymentMethod>> GetPaymentMethods();

        /// <summary>
        /// Get all transactions from the Stripe.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<StripeList<Transaction>> GetTransactions();

        /// <summary>
        /// Get all invoice list from the Stripe.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<StripeList<InvoiceItem>> GetInvoiceList();

        /// <summary>
        /// Get all sessions from the Stripe.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<StripeList<Stripe.Checkout.Session>> GetSessions();

        /// <summary>
        /// Get session by id from the Stripe.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<Stripe.Checkout.Session> GetSessionById(string sessionId);

        /// <summary>
        /// Get session items by id from the Stripe.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<StripeList<LineItem>> GetSessionItemsById(string sessionId);
    }

}
