using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Utils;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Extensions;
using CESMII.Marketplace.Service;
using Stripe;

namespace CESMII.Marketplace.Api.Controllers
{

    [Route("api/[controller]")]
    public class ECommerceController : BaseController<ECommerceController>
    {
        private readonly IECommerceService<CartModel> _svc;

        public ECommerceController(IECommerceService<CartModel> svc, UserDAL dalUser,
            ConfigUtil config, ILogger<ECommerceController> logger)
            : base(config, logger, dalUser)
        {
            _svc = svc;
        }

        [HttpPost, Route("checkout/init")]
        // [Authorize(Policy = nameof(PermissionEnum.UserAzureADMapped))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Checkout([FromBody] CartModel model)
        {
            var result = await _svc.DoCheckout(model, "");
            if (result == null)
            {
                return BadRequest($"Could not initialize checkout.");
            }
            return Ok(new ResultMessageWithDataModel() {
                Data = result,
                IsSuccess = true,
                Message = "Check out started..."
            } );
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

        [HttpPost, Route("product")]
        [ProducesResponseType(200, Type = typeof(Product))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateProduct([FromBody] MarketplaceItemModel marketplaceItemModel)
        {
            var result = await _svc.CreateProduct(marketplaceItemModel);
            if (result == null)
            {
                return BadRequest($"Could not create Product.");
            }
            return Ok(result);
        }

        [HttpDelete, Route("product")]
        [ProducesResponseType(200, Type = typeof(bool))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> DeleteProduct(string paymentProductId)
        {
            var result = await _svc.DeleteProduct(paymentProductId);
            if (result == null)
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