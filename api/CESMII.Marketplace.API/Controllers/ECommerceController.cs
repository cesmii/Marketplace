using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Service;
using Stripe;

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
    }
}