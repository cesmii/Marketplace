using System;
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
using CESMII.Marketplace.Api.Shared.Extensions;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Common.SelfServiceSignUp.Services;
using CESMII.Marketplace.ExternalSources.Models;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class ExternalSourceController : BaseController<ExternalSourceController>
    {
        private readonly ExternalSources.IExternalSourceFactory<MarketplaceItemModel> _sourceFactory;
        private readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;

        public ExternalSourceController(
            ExternalSources.IExternalSourceFactory<MarketplaceItemModel> sourceFactory,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            UserDAL dalUser,
            ConfigUtil config, ILogger<ExternalSourceController> logger)
            : base(config, logger, dalUser)
        {
            _sourceFactory = sourceFactory;
            _dalExternalSource = dalExternalSource;
        }

        [HttpPost, Route("GetByID")]
        [ProducesResponseType(200, Type = typeof(MarketplaceItemModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByID([FromBody] ExternalSourceRequestModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //get external source config, then instantiate object using info in the config by 
            //calling external source factory.
            //get by name - we have to ensure our external sources config data maintains a unique name.
            var sources = _dalExternalSource.Where(x => x.Name.ToLower().Equals(model.SourceId.ToLower()), null, null, false, false).Data;
            if (sources == null || sources.Count == 0)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|Invalid source : {model.SourceId}");
                return BadRequest($"Invalid source id");
            }
            else if (sources.Count > 1)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|External Source. Too many matches for {model.SourceId}");
                return BadRequest($"External Source. Too many matches.");
            }

            var src = sources[0];
            if (!src.Enabled) {
                _logger.LogWarning($"ExternalSourceController|GetByID|External Source. Source not enabled. {model.SourceId}");
                return BadRequest($"Invalid source id");
            }

            //now perform the get by id call
            var dalSource = await _sourceFactory.InitializeSource(src);
            var result = await dalSource.GetById(model.ID);

            if (result == null)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|Source not found. {model.SourceId}|{model.ID}");
                return BadRequest($"Item not found.");
            }

            /*
            //TBD - analytics - future
            MarketplaceItemAnalyticsModel analytic = null;
            if (result == null)
            {
                _logger.LogWarning($"ExternalSourceController|GetById|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            if (model.IsTracking)
            {
                //Increment Page Count
                //Check if CloudLib item is there if not add a new one then increment count and save
                analytic = _dalAnalytics.Where(x => x.CloudLibId == model.ID, null, null, false).Data.FirstOrDefault();

                if (analytic == null)
                {
                    analytic = new MarketplaceItemAnalyticsModel() { CloudLibId = model.ID, PageVisitCount = 1 };
                    await _dalAnalytics.Add(analytic, null);

                }
                else
                {
                    analytic.PageVisitCount += 1;
                    await _dalAnalytics.Update(analytic, model.ID);
                }
                result.Analytics = analytic;
            }
            */

            return Ok(result);
        }

    }
}
