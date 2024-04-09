using Microsoft.Extensions.Logging;

using Stripe;
using Stripe.Checkout;

using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Models;
using CESMII.Marketplace.Service.Models;
using Stripe.FinancialConnections;

namespace CESMII.Marketplace.Service
{
    // TBD - Remarks - Note this is prototype / sample code that is not fully implemented and not yet tested. Work in progress.
    public class StripeService : IECommerceService<CartModel>
    {
        private readonly IDal<Cart, CartModel> _dal;
        private readonly IDal<StripeAuditLog, StripeAuditLogModel> _stripeLogDal;
        private readonly ILogger<StripeService> _logger;
        private readonly StripeConfig _config;

        public StripeService(IDal<Cart, CartModel> dal, ILogger<StripeService> logger, IDal<StripeAuditLog, StripeAuditLogModel> stripeLogDal, ConfigUtil configUtil)
        {
            _dal = dal;
            _stripeLogDal = stripeLogDal;
            _logger = logger;
            _config = configUtil.StripeSettings;
        }

        public async Task<CheckoutInitModel> DoCheckout(CartModel item, string userId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                UiMode = "embedded",
                Mode = "payment",
                ReturnUrl = item.ReturnUrl,
                LineItems = new List<SessionLineItemOptions>()
            };

            foreach(var cartItem in item.Items)
            {
                var price = await GetPriceByProductId(cartItem.MarketplaceItem.PaymentProductId);
                if (price == null)
                {
                    _logger.LogError("StripeService|DoCheckout|Cannot get price with payment product id.");
                    throw new ArgumentException("Cannot get price with payment product id.");
                }

                options.LineItems.Add(new SessionLineItemOptions
                {
                    // Provide the exact Price ID (for example, pr_1234) of the product you want to sell
                    Price = price.Id,
                    Quantity = cartItem.Quantity,
                });
            }

            try
            {
                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = await service.CreateAsync(options);
                await _stripeLogDal.Add(new StripeAuditLogModel { Type = "CheckoutSessionCreation", Message = session.ToJson() }, userId);

                return new CheckoutInitModel() { 
                    ApiKey = _config.PublishKey,
                    SessionId = session.Id
                };
            } catch(Exception ex)
            {
                _logger.LogError(ex, "StripeService|DoCheckout|Error occured while checkout.");
                
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetProducts()
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            // Fetch all products
            var productService = new ProductService();
            var products = await productService.ListAsync(
                new ProductListOptions { Limit = 100 }
            );

            return products;
        }

        public async Task<IEnumerable<Price>> GetPrices()
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            // Fetch all products
            var priceService = new PriceService();
            var prices = await priceService.ListAsync(
                new PriceListOptions { Limit = 100 }
            );

            return prices;
        }

        public async Task<Product> GetProduct(string paymentProductId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            //cannot updateProduct if paymentProductId is null or empty
            if (string.IsNullOrEmpty(paymentProductId))
            {
                _logger.LogError("StripeService|GetProduct|Cannot get product with null payment product id.");
                throw new ArgumentException("Cannot get product with null payment product id.");
            }

            // Fetch all products
            var productService = new ProductService();
            var product = await productService.GetAsync(paymentProductId);

            return product;
        }
        
        public async Task<bool> DeleteProduct(string paymentProductId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            //cannot updateProduct if paymentProductId is null or empty
            if (string.IsNullOrEmpty(paymentProductId))
            {
                _logger.LogError("StripeService|DeleteProduct|Cannot delete product with null payment product id.");
                throw new ArgumentException("Cannot delete product with null payment product id.");
            }

            // Fetch all products
            var productService = new ProductService();
            var product = await productService.GetAsync(paymentProductId);
            if (product == null)
                return false;

            //map stuff from marketplace item to ProductUpdateOptions object
            var itemUpdate = new ProductUpdateOptions
            {
                Active = false,
                //DefaultPrice = //update marketplace item to support passing price
            };

            await productService.UpdateAsync(paymentProductId, itemUpdate);
            await _stripeLogDal.Add(new StripeAuditLogModel { Type = "ProductDelete", Message = product.ToJson() }, "");

            return true;
        }

        public async Task<Price> DeletePrice(Price price)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            // Fetch all products
            var priceService = new PriceService();
            var deletedPrice = await priceService.UpdateAsync(price.Id, new PriceUpdateOptions { });
            await _stripeLogDal.Add(new StripeAuditLogModel { Type = "PriceDelete", Message = deletedPrice.ToJson() }, "");

            return deletedPrice;
        }

        public async Task<Product> AddProduct(AdminMarketplaceItemModel item, string userId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            //map stuff from marketplace item to ProductUpdateOptions object
            var itemAdd = new ProductCreateOptions
            {
                Name = item.DisplayName ?? string.Empty,
                Description = item.Abstract == null ? string.Empty : item.Abstract.Replace("<p>", "").Replace("</p>", ""),
                DefaultPriceData = new ProductDefaultPriceDataOptions { UnitAmountDecimal = item.Price, Currency = "usd" }
            };
            
            var serviceProduct = new ProductService();

            //check for product 
            Product product = await serviceProduct.CreateAsync(itemAdd);

            _logger.LogInformation($"StripeService|AddProduct|Product added: {product.Id}|{product.Name}.");
            await _stripeLogDal.Add(new StripeAuditLogModel { Type = "AddProduct", Message = product.ToJson() }, "");

            //TBD - now set price for item - pull info from marketplace item
            //TBD - update product id into paymentProductId field and either save here or save in calling method.
            return product;
        }

        public async Task<Product> UpdateProduct(AdminMarketplaceItemModel item, string userId)
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
                Name = item.DisplayName ?? string.Empty,
                Description = item.Abstract == null ? string.Empty : item.Abstract.Replace("<p>","").Replace("</p>", ""),
                //DefaultPrice = //update marketplace item to support passing price
            };

            var serviceProduct = new ProductService();
            
            //check for product 
            var product = await serviceProduct.UpdateAsync(item.PaymentProductId, itemUpdate);

            _logger.LogInformation($"StripeService|UpdateProduct|Product updated: {product.Id}|{product.Name}.");

            var prices = await GetPricesByProductId(item.PaymentProductId);
            if (prices.Count(pr => pr.UnitAmountDecimal == item.Price) == 0)
            {
                var servicePrice = new PriceService();
                var optionsPrice = new PriceCreateOptions
                {
                    UnitAmount = item.Price,
                    Currency = "usd",
                    Product = item.PaymentProductId,
                    Active = true
                };

                product.DefaultPrice = await servicePrice.CreateAsync(optionsPrice);
            }

            _logger.LogInformation($"StripeService|UpdateProduct|Product price updated: {product.Id}|{product.Name}.");
            await _stripeLogDal.Add(new StripeAuditLogModel { Type = "UpdateProduct", Message = product.ToJson() }, "");

            return product;
        }

        public Task<bool> UpdateAllProducts(MarketplaceItemModel item, string userId)
        {
            throw new NotImplementedException();
        }

        public CartModel GetById(string id)
        {
            return _dal.GetById(id);
        }

        public async Task<string> Add(CartModel item, string userId)
        {
            return await _dal.Add(item, userId);
        }
        public async Task<int> Update(CartModel item, string userId)
        {
            return await _dal.Update(item, userId);
        }
        public async Task Delete(string id, string userId)
        {
            await _dal.Delete(id, userId);
        }

        private async Task<IEnumerable<Price>> GetPricesByProductId(string paymentProductId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            // Fetch all prices
            var priceService = new PriceService();
            var prices = await priceService.ListAsync(
                new PriceListOptions ()
            );

            return prices.Where(price => price.ProductId == paymentProductId);
        }

        private async Task<Price> GetPriceByProductId(string paymentProductId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            // Fetch all prices
            var priceService = new PriceService();
            var prices = await priceService.ListAsync(
                new PriceListOptions()
            );

            return prices.FirstOrDefault(price => price.ProductId == paymentProductId);
        }

        public async Task<IEnumerable<PaymentIntent>> GetPayments()
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            return await new PaymentIntentService().ListAsync();
        }

        public async Task<PaymentIntent> GetPaymentById(string paymentId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            return await new PaymentIntentService().GetAsync(paymentId);
        }

        public async Task<StripeList<PaymentMethod>> GetPaymentMethods()
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            return await new PaymentMethodService().ListAsync();
        }

        public async Task<StripeList<Transaction>> GetTransactions()
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            return await new TransactionService().ListAsync();
        }

        public async Task<StripeList<InvoiceItem>> GetInvoiceList()
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            return await new InvoiceItemService().ListAsync();
        }

        public async Task<StripeList<Stripe.Checkout.Session>> GetSessions()
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            return await new Stripe.Checkout.SessionService().ListAsync();
        }

        public async Task<Stripe.Checkout.Session> GetSessionById(string sessionId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            return await new Stripe.Checkout.SessionService().GetAsync(sessionId);
        }

        public async Task<StripeList<LineItem>> GetSessionItemsById(string sessionId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            return await new Stripe.Checkout.SessionService().ListLineItemsAsync(sessionId);
        }
    }
}
