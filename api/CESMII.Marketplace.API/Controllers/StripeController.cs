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

        [HttpGet, Route("payments")]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetPayments()
        {
            var result = await _svc.GetPayments();
            if (result == null)
            {
                return BadRequest($"Could not fetch payments.");
            }
            return Ok(result);
        }

        [HttpGet, Route("payment")]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetPaymentById(string paymentId)
        {
            var result = await _svc.GetPaymentById(paymentId);
            if (result == null)
            {
                return BadRequest($"Could not fetch payments.");
            }
            return Ok(result);
        }

        [HttpGet, Route("paymentMethods")]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var result = await _svc.GetPaymentMethods();
            if (result == null)
            {
                return BadRequest($"Could not fetch payment methods.");
            }
            return Ok(result);
        }

        [HttpGet, Route("sessions")]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetSessions()
        {
            var result = await _svc.GetSessions();
            if (result == null)
            {
                return BadRequest($"Could not fetch sessions.");
            }
            return Ok(result);
        }

        [HttpGet, Route("session")]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetSessionById(string sessionId)
        {
            var result = await _svc.GetSessionById(sessionId);
            if (result == null)
            {
                return BadRequest($"Could not fetch sessions.");
            }
            return Ok(result);
        }

        [HttpGet, Route("sessionItems")]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetSessionItems(string sessionId)
        {
            var result = await _svc.GetSessionItemsById(sessionId);
            if (result == null)
            {
                return BadRequest($"Could not fetch sessions.");
            }
            return Ok(result);
        }
    }
}
