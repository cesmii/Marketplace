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
using CESMII.Marketplace.Service.Models;

namespace CESMII.Marketplace.Api.Controllers
{
    
    [Authorize, Route("api/[controller]")]
    public class ECommerceController : BaseController<ECommerceController>
    {
        private readonly IECommerceService<CartModel> _svc;

        public ECommerceController(IECommerceService<CartModel> svc, UserDAL dalUser, 
            ConfigUtil config, ILogger<ECommerceController> logger)
            : base(config, logger, dalUser)
        {
            _svc = svc;
        }


        [HttpGet, Route("checkout/init")]
        [Authorize(Policy = nameof(PermissionEnum.UserAzureADMapped))]
        [ProducesResponseType(200, Type = typeof(CheckoutModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Checkout([FromBody] CartModel model)
        {
            var result = await _svc.DoCheckout(model,UserID);
            if (result == null)
            {
                return BadRequest($"Could not initialize checkout.");
            }
            return Ok(result);
        }
    }

}
