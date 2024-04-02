using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CESMII.Marketplace.API.Controllers
{
    [Route("api/[controller]")]
    public class StripeController : BaseController<StripeController>
    {
        private readonly IECommerceService<CartModel> _svc;

        public StripeController(IECommerceService<CartModel> svc, UserDAL dalUser,
            ConfigUtil config, ILogger<StripeController> logger)
            : base(config, logger, dalUser)
        {
            _svc = svc;
        }

        [HttpGet, Route("transactions")]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetTransactions()
        {
            var result = await _svc.GetTransactions();
            if (result == null)
            {
                return BadRequest($"Could not fetch Transactions.");
            }
            return Ok(result);
        }

        [HttpGet, Route("invoices")]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetInvoiceList()
        {
            var result = await _svc.GetInvoiceList();
            if (result == null)
            {
                return BadRequest($"Could not fetch invoices.");
            }
            return Ok(result);
        }
    }
}
