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
using CESMII.Marketplace.Api.Shared.Models;

namespace CESMII.Marketplace.Service
{
    // TBD - How do we hook to an oncompleted event and then send confirmation email once onComplete is done.
    // Note there is a check status method that has the email. If there is a Stripe event or callback, then we get
    // the session object and get the customer email.  
    public class StripeService : IECommerceService<CartModel>
    {
        private readonly IDal<Cart, CartModel> _dal;
        private readonly IDal<StripeAuditLog, StripeAuditLogModel> _stripeLogDal;
        private readonly ILogger<StripeService> _logger;
        private readonly IOrganizationService<OrganizationModel> _organizationService;
        private readonly StripeConfig _config;

        public StripeService(IOrganizationService<OrganizationModel> organizationService, IDal<Cart, CartModel> dal, ILogger<StripeService> logger, IDal<StripeAuditLog, StripeAuditLogModel> stripeLogDal, ConfigUtil configUtil)
        {
            _dal = dal;
            _organizationService = organizationService;
            _stripeLogDal = stripeLogDal;
            _logger = logger;
            _config = configUtil.StripeSettings;
        }

        public async Task<CheckoutInitModel> DoCheckoutAnonymous(CartModel item)
        {
            return await DoCheckout(item, null);
        }

        public async Task<CheckoutInitModel> DoCheckout(CartModel cart, UserModel user)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                UiMode = "embedded",
                //append check out session id
                ReturnUrl = cart.ReturnUrl.TrimEnd(Convert.ToChar("/")) + "/{CHECKOUT_SESSION_ID}",
                LineItems = new List<SessionLineItemOptions>()
            };

            foreach (var cartItem in cart.Items)
            {
                if (cartItem.SelectedPrice == null)
                {
                    _logger.LogError($"StripeService|DoCheckout|Cannot get price with null or empty payment price id.|Item: {cartItem.MarketplaceItem.Name}");
                    throw new ArgumentException($"Cannot get price with null or empty payment price id for {cartItem.MarketplaceItem.Name}.");
                }

                var price = await GetPriceById(cartItem.SelectedPrice.PriceId);
                if (price == null)
                {
                    _logger.LogError($"StripeService|DoCheckout|Cannot get Stripe price for |Item: {cartItem.MarketplaceItem.Name} with payment price id {cartItem.SelectedPrice.PriceId}.");
                    throw new ArgumentException($"Cannot get Stripe price info for {cartItem.MarketplaceItem.Name}.");
                }

                options.Mode = price.Type == "one_time" ? "payment" : "subscription";

                options.LineItems.Add(new SessionLineItemOptions
                {
                    Price = cartItem.SelectedPrice.PriceId,
                    Quantity = cartItem.Quantity,
                });
            }

            try
            {
                //an anonymous user may be checking out. they won't have credits. that is an ok scenario.
                if (user != null && cart.UseCredits)
                {
                    //if they want to use credits && have credits to use, apply here. 
                    var organization = _organizationService.GetByName(user.Organization.Name);
                    if (organization == null || organization.Credits <= 0)
                    {
                        _logger.LogError("StripeService|DoCheckout|Cannot use credits.");
                        throw new ArgumentException("Cannot use credits.");
                    }

                    //take lesser of credits available or total cost
                    var numCredits = CalculateCredits(organization.Credits, cart);

                    var couponService = new CouponService();
                    var coupon = await couponService.CreateAsync(new CouponCreateOptions
                    {
                        AmountOff = numCredits,
                        Duration = "once",
                        Currency = "usd",
                        MaxRedemptions = 1,
                        Name = "Credits Applied"
                    });

                    organization.Credits -= numCredits;

                    await _organizationService.Update(organization, user.ID);

                    var discount = new SessionDiscountOptions { Coupon = coupon.Id };
                    options.Discounts = new List<SessionDiscountOptions> { discount };
                }

                var service = new Stripe.Checkout.SessionService();
                var session = await service.CreateAsync(options);
                await _stripeLogDal.Add(new StripeAuditLogModel
                {
                    Type = "CheckoutSessionCreation",
                    Message = session.ToJson(),
                    CartModel = cart,
                    Session = session,
                    SessionCreateOptions = options
                }, user == null ? null : user.ID);

                return new CheckoutInitModel()
                {
                    ApiKey = _config.PublishKey,
                    SessionId = session.ClientSecret
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeService|DoCheckout|Error occured while checkout.");

                throw;
            }
        }

        private long CalculateCredits(long credits, CartModel cart)
        {
            if (!cart.UseCredits) return 0;
            //sum total cost, note subscription cost is just whatever the amount is
            var totalCost = cart.Items.Sum(x => x.SelectedPrice.Amount * x.Quantity);
            return credits >= totalCost ? totalCost : credits;
        }

        public async Task<CheckoutStatusModel> GetCheckoutStatus(string sessionId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            var svcSession = new Stripe.Checkout.SessionService();
            var data = svcSession.Get(sessionId);

            if (data == null)
            {
                _logger.LogWarning($"StripeService|GetCheckoutStatus|No checkout sessions found with id {sessionId}.");
                return null;
            }

            return new CheckoutStatusModel() { SessionId = sessionId, Status = data.Status, Data = data };
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

        public async Task<IEnumerable<Stripe.Price>> GetPrices()
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

        public async Task<Stripe.Price> DeletePrice(Stripe.Price price)
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
            };

            var serviceProduct = new ProductService();

            //check for product 
            Product product = await serviceProduct.CreateAsync(itemAdd);

            _logger.LogInformation($"StripeService|AddProduct|Product added: {product.Id}|{product.Name}.");
            await _stripeLogDal.Add(new StripeAuditLogModel { Type = "AddProduct", Message = product.ToJson() }, "");

            var priceService = new PriceService();
            foreach (var price in item.Prices)
            {
                var priceOption = new PriceCreateOptions
                {
                    Product = product.Id,
                    UnitAmountDecimal = price.Amount,
                    Nickname = price.Description,
                    Currency = "usd",
                };
                // TODO add subscription type.

                var newPrice = await priceService.CreateAsync(priceOption);
                price.PriceId = newPrice.Id;
            }

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
                Description = item.Abstract == null ? string.Empty : item.Abstract.Replace("<p>", "").Replace("</p>", ""),
            };

            var serviceProduct = new ProductService();

            //check for product 
            var product = await serviceProduct.UpdateAsync(item.PaymentProductId, itemUpdate);

            _logger.LogInformation($"StripeService|UpdateProduct|Product updated: {product.Id}|{product.Name}.");

            // TODO Prices
            //var prices = await GetPricesByProductId(item.PaymentProductId);
            //if (prices.Count(pr => pr.UnitAmountDecimal == item.Price) == 0)
            //{
            //    var servicePrice = new PriceService();
            //    var optionsPrice = new PriceCreateOptions
            //    {
            //        UnitAmount = item.Price,
            //        Currency = "usd",
            //        Product = item.PaymentProductId,
            //        Active = true
            //    };

            //    product.DefaultPrice = await servicePrice.CreateAsync(optionsPrice);
            //}

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

        private async Task<IEnumerable<Stripe.Price>> GetPricesByProductId(string paymentProductId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            // Fetch all prices
            var priceService = new PriceService();
            var prices = await priceService.ListAsync(
                new PriceListOptions { Active = true, Product = paymentProductId }
            );

            return prices.ToList();
        }

        private async Task<Stripe.Price?> GetPriceById(string paymentPriceId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            // Fetch all prices
            var priceService = new PriceService();
            var price = await priceService.GetAsync(paymentPriceId);

            if (price == null)
                return null;

            return price;
        }

        private async Task<Stripe.Price?> GetPriceByProductId(string paymentProductId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            // Fetch all prices
            var priceService = new PriceService();
            var prices = await priceService.ListAsync(
                new PriceListOptions { Active = true, Product = paymentProductId }
            );

            if (prices == null)
                return null;

            return prices.FirstOrDefault();
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
