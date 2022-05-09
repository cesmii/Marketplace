using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class ProfileController : BaseController<ProfileController>
    {
        private readonly ICloudLibDAL<MarketplaceItemModel> _dalCloudLib;

        public ProfileController(
            ICloudLibDAL<MarketplaceItemModel> dalCloudLib,
            ConfigUtil config, ILogger<ProfileController> logger)
            : base(config, logger)
        {
            _dalCloudLib = dalCloudLib;
        }

        [HttpPost, Route("GetByID")]
        [ProducesResponseType(200, Type = typeof(MarketplaceItemModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdStringWithTrackingModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"SmProfileController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dalCloudLib.GetById(model.ID);
            return Ok(result);
        }

    }
}
