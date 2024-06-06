using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

using Stripe;
using Stripe.Checkout;
using Stripe.FinancialConnections;

using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Models;
using CESMII.Marketplace.Service.Models;
using CESMII.Common.SelfServiceSignUp.Services;
using CESMII.Marketplace.JobManager;
using CESMII.Marketplace.JobManager.Models;
using CESMII.Marketplace.Common.Enums;
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

        //on complete actions
        private readonly MailConfig _mailConfig;
        private readonly MailRelayService _mailRelayService;
        private readonly IJobFactory _jobFactory;
        private readonly IDal<JobDefinition, JobDefinitionModel> _dalJobDefinition;
        private readonly IDal<JobLog, JobLogModel> _dalJobLog;


        public StripeService(IOrganizationService<OrganizationModel> organizationService,
            IDal<Cart, CartModel> dal,
            ILogger<StripeService> logger,
            IDal<StripeAuditLog, StripeAuditLogModel> stripeLogDal,
            ConfigUtil configUtil,
            MailRelayService mailRelayService,
            IJobFactory jobFactory,
            IDal<JobDefinition, JobDefinitionModel> dalJobDefinition,
            IDal<JobLog, JobLogModel> dalJobLog)
        {
            _dal = dal;
            _organizationService = organizationService;
            _stripeLogDal = stripeLogDal;
            _logger = logger;
            _config = configUtil.StripeSettings;
            //
            _mailRelayService = mailRelayService;
            _mailConfig = configUtil.MailSettings;
            _jobFactory = jobFactory;
            _dalJobDefinition = dalJobDefinition;
            _dalJobLog = dalJobLog;
        }

        /// <summary>
        /// User data is embedded within cart model. 
        /// If user is a guest, user id is null and org id is null.
        /// If user is authenticated, we populate that info in the cart.CheckoutUser
        /// </summary>
        /// <param name="cart"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<CheckoutInitModel> DoCheckout(CartModel cart)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                UiMode = "embedded",
                //append check out session id
                ReturnUrl = cart.ReturnUrl.TrimEnd(Convert.ToChar("/")) + "/{CHECKOUT_SESSION_ID}",
                LineItems = new List<SessionLineItemOptions>(),
                ExpiresAt = DateTime.Now.AddMinutes(30) //TODO: make this configurable in appSettings
            };

            // For anonymous users, save the cart details first into the database and use that after successful checkout.
            if (string.IsNullOrEmpty(cart.ID) || string.IsNullOrEmpty(cart.CheckoutUser?.ID))
            {
                cart.ID = await _dal.Add(cart, null);
            }
            //update cart id to client reference id - tie this cart to the checkout session
            options.ClientReferenceId = cart.ID;

            //show user entry fields in checkout screen if anonymous user
            if (string.IsNullOrEmpty(cart.CheckoutUser?.ID))
            {
                PrepareCustomFields(options);
            }
            //set customer info this way
            if (!string.IsNullOrEmpty(cart.CheckoutUser?.Email))
            {
                options.CustomerEmail = cart.CheckoutUser.Email;
            }

            //prepare cart items
            await PrepareCartItems(cart, options);

            try
            {
                cart.CreditsApplied = null; //just make sure nothing is assigning this. We calc and assign here.

                OrganizationModel? organization = !string.IsNullOrEmpty(cart.CheckoutUser?.Organization?.Name) ?
                        _organizationService.GetByName(cart.CheckoutUser.Organization.Name) : null;
                //prepare credits
                await PrepareCredits(cart, options, organization);

                var service = new Stripe.Checkout.SessionService();
                var session = await service.CreateAsync(options);
                await _stripeLogDal.Add(new StripeAuditLogModel
                {
                    Type = "CheckoutSessionCreation",
                    Message = session.ToJson(),
                    CartModel = cart,
                    Session = session,
                    SessionCreateOptions = options
                }, string.IsNullOrEmpty(cart.CheckoutUser?.ID) ? null : cart.CheckoutUser.ID);

                // Update sessionId, credits applied (potentially assigned above) and OrganizationId for Webhook
                cart.SessionId = session.Id;
                if (organization != null && cart.CheckoutUser != null)
                {
                    cart.CheckoutUser.Organization.ID = organization.ID;
                }
                await _dal.Update(cart, cart.CheckoutUser?.ID);

                //
                return new CheckoutInitModel()
                {
                    ApiKey = _config.PublishKey,
                    SessionId = session.Id,
                    ClientSecret = session.ClientSecret,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeService|DoCheckout|Error occured while checkout.");

                throw;
            }
        }

        public async Task<bool> StripeWebhook(Controller controller, string json, string header)
        {
            var stripeEvent = EventUtility.ConstructEvent(json, header, _config.WebhookSecretKey);

            // Handle the event
            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                if (session == null)
                    return false;

                var cart = GetBySessionId(session.Id);

                await _stripeLogDal.Add(new StripeAuditLogModel
                {
                    Type = "CheckoutSessionCompleted",
                    Message = session.ToJson(),
                    CartModel = cart,
                    Session = session
                }, null);

                if (cart == null)
                    return true;

                // Update cart status
                cart.Status = Common.Enums.CartStatusEnum.Completed;
                cart.Completed = DateTime.Now;
                //if it was anonymous, then pull Checkoutuser info from Stripe
                if (string.IsNullOrEmpty(cart.CheckoutUser?.ID))
                {
                    cart.CheckoutUser = MapToModelUserCheckout(session);
                }
                await Update(cart, cart.CreatedById);

                //send notification emails, run on complete jobs if needed
                await ExecuteOnCompleteActions(controller, cart, session);

                if (string.IsNullOrEmpty(cart.CheckoutUser.Organization?.ID))
                    return true;

                var organization = _organizationService.GetById(cart.CheckoutUser.Organization.ID);
                if (organization == null)
                    return true;

                // Update/deduct organization credits to deduct amount used.
                if (cart.CreditsApplied.HasValue)
                {
                    organization.Credits = Convert.ToInt64(organization.Credits + "00") - cart.CreditsApplied.Value;
                }
                await _organizationService.Update(organization, cart.CreatedById);
            }
            /* TODO: Should we handle these other event complete scenarios
                // Handle the event
                else if (stripeEvent.Type == Events.CheckoutSessionAsyncPaymentSucceeded)
                {
                    //do same as checkout session completed?
                }
                else if (stripeEvent.Type == Events.CheckoutSessionExpired)
                {
                    //remove cart if it is present
                }
                // ... handle other event types
                else
                {
                    //add log warning message that an unhandled stripe event was fired.
                }
             
             */

            return false;
        }


        #region doCheckout helper methods
        private static void PrepareCustomFields(Stripe.Checkout.SessionCreateOptions options)
        {
            //set custom fields data using checkout user data
            options.PhoneNumberCollection = new SessionPhoneNumberCollectionOptions() { Enabled = true };
            options.CustomFields = new List<SessionCustomFieldOptions>
            {
                new SessionCustomFieldOptions
                {
                    Key = "firstName",
                    Label = new SessionCustomFieldLabelOptions
                    {
                        Type = "custom",
                        Custom = "First name",
                    },
                    Type = "text",
                },new SessionCustomFieldOptions
                {
                    Key = "lastName",
                    Label = new SessionCustomFieldLabelOptions
                    {
                        Type = "custom",
                        Custom = "Last name",
                    },
                    Type = "text",
                },new SessionCustomFieldOptions
                {
                    Key = "companyName",
                    Label = new Stripe.Checkout.SessionCustomFieldLabelOptions
                    {
                        Type = "custom",
                        Custom = "Company name",
                    },
                    Type = "text",
                }
            };
        }

        private async Task PrepareCartItems(CartModel cart, Stripe.Checkout.SessionCreateOptions options)
        {
            //prepare cart items
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

                if (options.Mode == "payment") {
                    options.InvoiceCreation = new SessionInvoiceCreationOptions { Enabled = true };
                }

                options.LineItems.Add(new SessionLineItemOptions
                {
                    Price = cartItem.SelectedPrice.PriceId,
                    Quantity = cartItem.Quantity,
                });

                if (!string.IsNullOrEmpty(cartItem.MarketplaceItem.ECommerce.TermsOfService))
                {
                    if (options.CustomText == null) options.CustomText = new SessionCustomTextOptions();
                    options.CustomText.TermsOfServiceAcceptance = new SessionCustomTextTermsOfServiceAcceptanceOptions()
                    { Message = cartItem.MarketplaceItem.ECommerce.TermsOfService };

                    if (cartItem.MarketplaceItem.ECommerce.TermsOfServiceIsRequired)
                    {
                        if (options.ConsentCollection == null) options.ConsentCollection = new SessionConsentCollectionOptions();
                        options.ConsentCollection = new SessionConsentCollectionOptions() { TermsOfService = "required" };
                    }
                }
            }
        }

        private async Task PrepareCredits(CartModel cart, Stripe.Checkout.SessionCreateOptions options,
            OrganizationModel? organization)
        {
            //an anonymous user may be checking out. they won't have credits. that is an ok scenario.
            if (!string.IsNullOrEmpty(cart.CheckoutUser?.ID) && cart.UseCredits)
            {
                //if they want to use credits && have credits to use, apply here. 
                if (organization == null || organization.Credits <= 0)
                {
                    _logger.LogError("StripeService|DoCheckout|Cannot use credits.");
                    throw new ArgumentException("Cannot use credits.");
                }

                //take lesser of credits available or total cost
                cart.CreditsApplied = CalculateCredits(organization.Credits, cart);

                var couponService = new CouponService();
                var coupon = await couponService.CreateAsync(new CouponCreateOptions
                {
                    AmountOff = cart.CreditsApplied,
                    Duration = "once",
                    Currency = "usd",
                    MaxRedemptions = 1,
                    Name = "Credits Applied"
                });

                var discount = new SessionDiscountOptions { Coupon = coupon.Id };
                options.Discounts = new List<SessionDiscountOptions> { discount };
            }
        }
        #endregion

        #region onCheckoutComplete Methods 
        private UserCheckoutModel MapToModelUserCheckout(Stripe.Checkout.Session session)
        {
            return new UserCheckoutModel() {
                FirstName = session.CustomFields.FirstOrDefault(cf => cf.Key == "firstName")?.Text.Value,
                LastName = session.CustomFields.FirstOrDefault(cf => cf.Key == "lastName")?.Text.Value,
                Organization = new OrganizationCheckoutModel()
                { Name = session.CustomFields.FirstOrDefault(cf => cf.Key == "companyName")?.Text.Value },
                Email = session.CustomerDetails?.Email,
                Phone = session.CustomerDetails?.Phone
            };
        }

        private async Task ExecuteOnCompleteActions(Controller controller, CartModel cart, Stripe.Checkout.Session session)
        {
            //TODO: new email template - send one purchase successful email to purchaser with a view like the whole checkout screen
            await SendEmailReceipt(controller, cart, session);

            foreach (var cartItem in cart.Items)
            {
                //if fail, log but keep going - handled below
                await OnCheckoutCompleteJob(cartItem, cartItem.MarketplaceItem, cart.CheckoutUser);
                await SendEmailPurchaseNotification(controller, cartItem, cart.CheckoutUser);
            }
        }

        /// <summary>
        /// Send this to the person who made the purchase. This should
        /// have all items in one email and look like our checkout screen.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="cart"></param>
        /// <returns></returns>
        private async Task SendEmailReceipt(Controller controller, CartModel cart, Stripe.Checkout.Session session)
        {
            //TODO: Check if Stripe can send out a confirmation email instead of us?
            /*
            try
            {
                if (string.IsNullOrEmpty(session.InvoiceId))
                    return;

                //For subscription, Stripe will automatically sends the invoice to the customer.
                //For onetime payment we need to send explicitly
                if (session.Mode != "payment")
                    return;

                var invoice = await new InvoiceService().SendInvoiceAsync(session.InvoiceId, new InvoiceSendOptions {  });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeService|SendMailCheckoutCompleted|Error occured while sending invoice.");
            }
            */

            //TODO: pass in cart info so we can include total quantity of item, total price of each items
            try
            {
                //receipt for recipient to mirror the cart and purchase info
                var body = await Api.Shared.Extensions.ViewExtensions.RenderViewAsync(controller,
                    "~/Views/Template/ECommerceReceipt.cshtml", new PurchaseReceiptModel() { Cart = cart, BaseUrl = _mailConfig.BaseUrl});

                var message = new MailMessage
                {
                    From = new MailAddress(_mailConfig.MailFromAddress),
                    Subject = $"CESMII | SM Marketplace | Thank you for your purchase ",
                    Body = body,
                    IsBodyHtml = true,
                };

                //send to purchaser
                message.To.Add(new MailAddress(cart.CheckoutUser.Email));

                //add Bcc of CESMII folks for awareness and troubleshooting
                foreach (var email in _mailConfig.ECommerceToAddresses)
                {
                    message.Bcc.Add(new MailAddress(email));
                }

                await _mailRelayService.SendEmail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeService|SendEmailReceipt|Error occured while sending email.");
            }

        }

        /// <summary>
        /// Target Audience is CESMII people and the vendor who is selling item
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="marketplaceItem"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task SendEmailPurchaseNotification(Controller controller, CartItemModel item, UserCheckoutModel user)
        {
            //TODO: pass in cart info so we can include total quantity of item, total price of each items
            try
            {
                //notify each vendor and CESMII that an item was purchased. Do this for each item in the cart
                var body = await Api.Shared.Extensions.ViewExtensions.RenderViewAsync(controller, 
                    "~/Views/Template/ECommercePurchaseNotification.cshtml", new PurchaseNotificationModel() 
                    { CartItem = item, CheckoutUser = user, BaseUrl = _mailConfig.BaseUrl });

                var message = new MailMessage
                {
                    From = new MailAddress(_mailConfig.MailFromAddress),
                    Subject = $"CESMII | SM Marketplace | {item.MarketplaceItem.DisplayName} purchased ",
                    Body = body,
                    IsBodyHtml = true,
                };

                //determine who should receive from vendor 
                foreach (var email in item.MarketplaceItem.Emails)
                {
                    if (email.PublishType?.ToLower() == "ecommerce" || email.PublishType?.ToLower() == "all")
                    {
                        message.To.Add(new MailAddress(email.EmailAddress, email.RecipientName));
                    }
                }

                //who should receive notification of any item purchased
                foreach (var email in _mailConfig.ECommerceToAddresses)
                {
                    message.CC.Add(new MailAddress(email));
                }

                await _mailRelayService.SendEmail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeService|SendEmailPurchaseNotification|Error occured while sending email.");
            }
        }

        /// <summary>
        /// For each marketplace item in cart, do a custom post checkout method
        /// to perform custom actions - if a job def is assigned
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task OnCheckoutCompleteJob(CartItemModel item, MarketplaceItemCheckoutModel marketplaceItem, UserCheckoutModel user)
        {
            try
            {
                //check if job is assigned for this it
                var job = _dalJobDefinition.Where(x => x.MarketplaceItemId.ToString().Equals(marketplaceItem.ID) && x.IsActive &&
                            x.ActionType == (int)JobActionTypeEnum.ECommerceOnComplete, null, 1).Data.FirstOrDefault();

                if (job == null) return;

                if (job.RequiresAuthentication && string.IsNullOrEmpty(user?.ID))
                {
                    _logger.LogError($"StripeService|onCheckoutCompleteJob|Job requires an authenticated user");
                    return;
                }

                //create jobpayload model
                var payload = new { CartItem = item, CheckoutUser = user };
                var jobData = new JobPayloadModel()
                {
                    JobDefinitionId = job.ID,
                    MarketplaceItemId = marketplaceItem.ID,
                    Payload = Newtonsoft.Json.JsonConvert.SerializeObject(payload)
                };
                //execute job
                await _jobFactory.ExecuteJob(jobData, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeService|OnCheckoutCompleteJob|Error occured OnCheckoutCompleteJob.");
            }
        }
        #endregion

        private long CalculateCredits(long credits, CartModel cart)
        {
            if (!cart.UseCredits) return 0;
            //sum total cost, note subscription cost is just whatever the amount is
            var totalCost = cart.Items.Sum(x => x.SelectedPrice.Amount * x.Quantity);
            return credits >= totalCost ? totalCost : Convert.ToInt64(credits + "00");
        }

        #region Stripe API Calls
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
            item.ECommerce.PaymentProductId = product.Id;

            _logger.LogInformation($"StripeService|AddProduct|Product added: {product.Id}|{product.Name}.");
            await _stripeLogDal.Add(new StripeAuditLogModel { Type = "AddProduct", Message = product.ToJson() }, "");

            foreach (var price in item.ECommerce.Prices)
            {
                await AddStripePrice(item, price);
            }

            //TBD - now set price for item - pull info from marketplace item
            //TBD - update product id into paymentProductId field and either save here or save in calling method.
            return product;
        }

        public async Task<Product> UpdateProduct(AdminMarketplaceItemModel item, string userId)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;

            //cannot updateProduct if paymentProductId is null or empty
            if (string.IsNullOrEmpty(item.ECommerce.PaymentProductId))
            {
                _logger.LogError("StripeService|UpdateProduct|Cannot update product with null payment product id.");
                throw new ArgumentException("Cannot update product with null payment product id.");
            }

            // TODO Prices
            var stripPrices = await GetPricesByProductId(item.ECommerce.PaymentProductId);

            foreach (var price in item.ECommerce.Prices)
            {
                if (!string.IsNullOrEmpty(price.PriceId) && stripPrices.Count(pr => pr.Id == price.PriceId && pr.UnitAmountDecimal == Convert.ToDecimal(price.Amount + "00")) == 1)
                {
                    continue;
                }

                await AddStripePrice(item, price);
            }

            // TODO Delete removed prices.
            // Archive the prices which are deleted or updated.
            foreach (var stripePrice in stripPrices)
            {
                if (item.ECommerce.Prices.Count(pr => stripePrice.Id == pr.PriceId && stripePrice.UnitAmountDecimal == Convert.ToDecimal(pr.Amount + "00")) == 1)
                {
                    continue;
                }

                await ArchiveStripePrice(stripePrice);
            }

            //map stuff from marketplace item to ProductUpdateOptions object
            var itemUpdate = new ProductUpdateOptions
            {
                Name = item.DisplayName ?? string.Empty,
                Description = item.Abstract == null ? string.Empty : item.Abstract.Replace("<p>", "").Replace("</p>", ""),
            };

            var serviceProduct = new ProductService();

            //check for product 
            var product = await serviceProduct.UpdateAsync(item.ECommerce.PaymentProductId, itemUpdate);

            _logger.LogInformation($"StripeService|UpdateProduct|Product updated: {product.Id}|{product.Name}.");

            await _stripeLogDal.Add(new StripeAuditLogModel { Type = "UpdateProduct", Message = product.ToJson() }, "");

            return product;
        }

        private static async Task AddStripePrice(AdminMarketplaceItemModel item, ProductPrice price)
        {
            var priceService = new PriceService();

            var priceOption = new PriceCreateOptions
            {
                Product = item.ECommerce.PaymentProductId,
                UnitAmountDecimal = Convert.ToDecimal(price.Amount + "00"),
                Nickname = price.Caption,
                Currency = "usd",
            };

            // TODO add subscription type.
            if (price.BillingPeriod == "Yearly")
            {
                priceOption.Recurring = new PriceRecurringOptions { Interval = "year", IntervalCount = 1 };
            }
            else if (price.BillingPeriod == "Monthly")
            {
                priceOption.Recurring = new PriceRecurringOptions { Interval = "month", IntervalCount = 1 };
            }

            var newPrice = await priceService.CreateAsync(priceOption);
            price.PriceId = newPrice.Id;
        }

        private static async Task ArchiveStripePrice(Price price)
        {
            var priceService = new PriceService();
            await priceService.UpdateAsync(price.Id, new PriceUpdateOptions { Active = false });
        }


        public Task<bool> UpdateAllProducts(MarketplaceItemModel item, string userId)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Cart Svc Methods
        public CartModel? GetByUserId(string userId)
        {
            var usid = MongoDB.Bson.ObjectId.Parse(userId);
            var cartModels = _dal.Where(x => x.CreatedById == usid && x.IsActive, null, null, false, false).Data;
            return cartModels == null || cartModels.Count == 0 ? null : cartModels[0];
        }

        public CartModel? GetBySessionId(string sessionId)
        {
            var cartModels = _dal.Where(x => x.SessionId == sessionId, null, null, false, false).Data;
            return cartModels == null || cartModels.Count == 0 ? null : cartModels[0];
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
        #endregion

        #region Stripe API helper methods
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
        #endregion
    }
}
