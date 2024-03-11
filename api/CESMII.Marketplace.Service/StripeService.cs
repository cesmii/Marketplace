using Microsoft.Extensions.Logging;

using Stripe;

using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Service.Models;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Models;

namespace CESMII.Marketplace.Service
{
    // TBD - Remarks - Note this is prototype / sample code that is not fully implemented and not yet tested. Work in progress.
    public class StripeService : IECommerceService<CartModel>
    {
        private readonly IDal<Cart, CartModel> _dal;
        private readonly ILogger<StripeService> _logger;
        private readonly StripeConfig _config;

        public StripeService(IDal<Cart, CartModel> dal, ILogger<StripeService> logger, ConfigUtil configUtil)
        {
            _dal = dal;
            _logger = logger;
            _config = configUtil.StripeSettings;
        }

        public async Task<CheckoutModel> DoCheckout(CartModel item, string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> GetProduct(string paymentProductId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> AddProduct(MarketplaceItemModel item, string userId)
        {
            throw new NotImplementedException();
            /*
            StripeConfiguration.ApiKey = _config.SecretKey;

            //cannot updateProduct if paymentProductId is null or empty
            if (string.IsNullOrEmpty(item.PaymentProductId))
            {
                _logger.LogError("StripeService|UpdateProduct|Cannot update product with null payment product id.");
                throw new ArgumentException("Cannot update product with null payment product id.");
            }

            //map stuff from marketplace item to ProductUpdateOptions object
            var itemAdd = new ProductCreateOptions
            {
                Name = item.DisplayName,
                Description = item.Abstract,
                //DefaultPrice = //update marketplace item to support passing price
            };
            var serviceProduct = new ProductService();

            //check for product 
            Product product = await serviceProduct.AddAsync(item.PaymentProductId, itemAdd);

            _logger.LogInformation($"StripeService|AddProduct|Product added: {product.Id}|{product.Name}.");

            //TBD - now set price for item - pull info from marketplace item
            var optionsPrice = new PriceCreateOptions
            {
                UnitAmount = 1200,
                Currency = "usd",
                Recurring = new PriceRecurringOptions
                {
                    Interval = "month",
                },
                Product = product.Id
            };
            var servicePrice = new PriceService();
            Price price = servicePrice.Create(optionsPrice);
            _logger.LogInformation($"StripeService|AddProduct|Product price added: {product.Id}|{product.Name}.");
            
            //TBD - update product id into paymentProductId field and either save here or save in calling method.
            */
        }

        public async Task<bool> UpdateProduct(MarketplaceItemModel item, string userId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            //cannot updateProduct if paymentProductId is null or empty
            if (string.IsNullOrEmpty(item.PaymentProductId))
            {
                _logger.LogError("StripeService|UpdateProduct|Cannot update product with null payment product id.");
                throw new ArgumentException("Cannot update product with null payment product id.");
            }

            //map stuff from marketplace item to ProductUpdateOptions object
            var itemUpdate = new ProductUpdateOptions
            {
                Name = item.DisplayName,
                Description = item.Abstract,
                //DefaultPrice = //update marketplace item to support passing price
            };
            var serviceProduct = new ProductService();

            //check for product 
            Product product = await serviceProduct.UpdateAsync(item.PaymentProductId, itemUpdate);

            _logger.LogInformation($"StripeService|UpdateProduct|Product updated: {product.Id}|{product.Name}.");

            //TBD - now set price for item - pull info from marketplace item
            /*
            var optionsPrice = new PriceUpdateOptions
            {
                UnitAmount = 1200,
                Currency = "usd",
                Recurring = new PriceRecurringOptions
                {
                    Interval = "month",
                },
                Product = product.Id
            };
            var servicePrice = new PriceService();
            Price price = servicePrice.Update(optionsPrice);
            _logger.LogInformation($"StripeService|UpdateProduct|Product price updated: {product.Id}|{product.Name}.");
            */
            return true;
        }

        public Task<bool> UpdateAllProducts(MarketplaceItemModel item, string userId)
        {
            throw new NotImplementedException();
        }

        public CartModel GetById(string id) {
            return _dal.GetById(id);
        }

        public async Task<string> Add(CartModel item, string userId) { 
            return await _dal.Add(item, userId);
        }
        public async Task<int> Update(CartModel item, string userId) {
            return await _dal.Update(item, userId);
        }
        public async Task Delete(string id, string userId) {
            await _dal.Delete(id, userId);
        }

    }
}
