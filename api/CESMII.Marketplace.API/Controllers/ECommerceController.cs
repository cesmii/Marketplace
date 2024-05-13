using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Service;
using Stripe;
using System.IO;
using System;

namespace CESMII.Marketplace.Api.Controllers
{

    [Route("api/[controller]")]
    public class ECommerceController : BaseController<ECommerceController>
    {
        private readonly IECommerceService<CartModel> _svc;
        private readonly IOrganizationService<OrganizationModel> _svcOrganization;

        public ECommerceController(IECommerceService<CartModel> svc, IOrganizationService<OrganizationModel> svcOrganization, 
            UserDAL dalUser,
            ConfigUtil config, ILogger<ECommerceController> logger)
            : base(config, logger, dalUser)
        {
            _svc = svc;
            _svcOrganization = svcOrganization;
        }

        [HttpPost, Route("checkout/init")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Checkout([FromBody] CartModel model)
        {
            var result = await (User.Identity.IsAuthenticated ?
                    _svc.DoCheckout(model, LocalUser) :
                    _svc.DoCheckoutAnonymous(model));
            if (result == null)
            {
                return BadRequest($"Could not initialize checkout.");
            }
            return Ok(new ResultMessageWithDataModel()
            {
                Data = result,
                IsSuccess = true,
                Message = "Check out started..."
            });
        }

        [HttpPost, Route("checkout/status")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CheckoutStatus([FromBody] IdStringModel model)
        {
            var result = await _svc.GetCheckoutStatus(model.ID);
            if (result == null)
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    Data = null,
                    IsSuccess = true,
                    Message = "Check out status unknown..."
                });
            }

            return Ok(new ResultMessageWithDataModel()
            {
                Data = result.Data,
                IsSuccess = true,
                Message = $"Check out status is {result.Status}."
            });
        }

        /// <summary>
        /// Allow Anonymous or Authorized user to call this.
        /// if anonymous, then null credits returned.
        /// If authorized, the remaining credit balance is returned.
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("credits")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public IActionResult GetAvailableCredits()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    Data = null,
                    IsSuccess = true,
                    Message = "Anonymous user, no credits available."
                });
            }
            // TODO call salesforce api to get credits.

            // Search for organization
            //var filter = new PagerFilterSimpleModel() { Query = LocalUser.Organization.Name, Skip = 0, Take = 9999 };
            //var listMatchOrganizationName = _svcOrganization.Search(filter).Data;
            var org = _svcOrganization.GetByName(LocalUser.Organization.Name);

            if (org == null)
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    Data = null,
                    IsSuccess = false,
                    Message = "Credits not found. The organization for this user could not be found."
                });
            }
            return Ok(new ResultMessageWithDataModel()
            {
                Data = org.Credits,
                IsSuccess = true,
                Message = "Credits fetched..."
            });
        }

        [HttpGet, Route("cart")]
        [ProducesResponseType(200, Type = typeof(Product))]
        [ProducesResponseType(400)]
        public IActionResult GetCart()
        {
            var result = User.Identity.IsAuthenticated ?
                    _svc.GetByUserId(UserID) :
                    null;

            if (result == null)
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = "CartModel not found.",
                });
            }

            return Ok(new ResultMessageWithDataModel()
            {
                IsSuccess = true,
                Message = "CartModel.",
                Data = result
            });
        }

        /// <summary>
        /// Add an Cart item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("cart/add")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] CartModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ECommerceController|Add|Invalid model");
                return BadRequest("Marketplace|Add|Invalid model");
            }
            else
            {
                string modelId;
                if (string.IsNullOrEmpty(model.ID))
                {
                    modelId = await _svc.Add(model, UserID);
                }
                else
                {
                    await _svc.Update(model, UserID);
                    modelId = model.ID;
                }

                if (String.IsNullOrEmpty(modelId))
                {
                    _logger.LogWarning($"ECommerceController|Add|Could not add CartModel");
                    return BadRequest("Could not add CartModel. Invalid id.");
                }
                _logger.LogInformation($"ECommerceController|Add|Add CartModel item. Id:{model.ID}.");

                var result = _svc.GetById(modelId);

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = result
                });
            }
        }

        /// <summary>
        /// Update an existing Cart item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("cart/update")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] CartModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ECommerceController|Update|Invalid model");
                return BadRequest("CartModel|Update|Invalid model");
            }

            var record = _svc.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"ECommerceController|Update|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }            
            else
            {
                if (model.Items.Count > 0)
                {
                    var result = await _svc.Update(model, UserID);
                    if (result < 0)
                    {
                        _logger.LogWarning($"ECommerceController|Update|Could not update CartModel. Invalid id:{model.ID}.");
                        return BadRequest("Could not update profile CartModel. Invalid id.");
                    }

                    _logger.LogInformation($"ECommerceController|Update|Updated CartModel. Id:{model.ID}.");
                    return Ok(new ResultMessageWithDataModel()
                    {
                        IsSuccess = true,
                        Message = "Item was updated.",
                        Data = model
                    });
                }
                else
                {
                    await _svc.Delete(model.ID, UserID);

                    return Ok(new ResultMessageWithDataModel()
                    {
                        IsSuccess = true,
                        Message = "Item was updated."
                    });
                }
            }
        }

        /// <summary>
        /// Delete an existing Cart. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("cart/delete")]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {           
            await _svc.Delete(model.ID, UserID);
            
            _logger.LogInformation($"ECommerceController|Delete|Deleted marketplaceItem. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }       

        [HttpGet, Route("products")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Product>))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetProducts()
        {
            var result = await _svc.GetProducts();
            if (result == null)
            {
                return BadRequest($"Could not fetch Products.");
            }
            return Ok(result);
        }

        [HttpGet, Route("product")]
        [ProducesResponseType(200, Type = typeof(Product))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetProductById(string paymentProductId)
        {
            var result = await _svc.GetProduct(paymentProductId);
            if (result == null)
            {
                return BadRequest($"Could not fetch Product.");
            }
            return Ok(result);
        }

        [HttpDelete, Route("product")]
        [ProducesResponseType(200, Type = typeof(bool))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeleteProduct(string paymentProductId)
        {
            var result = await _svc.DeleteProduct(paymentProductId);
            if (!result)
            {
                return BadRequest($"Could not delete Product.");
            }
            return Ok(result);
        }

        [HttpGet, Route("prices")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Price>))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetPrices()
        {
            var result = await _svc.GetPrices();
            if (result == null)
            {
                return BadRequest($"Could not fetch Prices.");
            }
            return Ok(result);
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> WebHook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var header = Request.Headers["Stripe-Signature"];

                await _svc.StripeWebhook(json, header);
                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine(e.StripeError.Message);
                return BadRequest();
            }
        }
    }
}