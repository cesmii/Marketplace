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
using CESMII.Marketplace.DAL.ExternalSources;
using CESMII.Marketplace.DAL.ExternalSources.Models;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class ExternalSourceController : BaseController<ExternalSourceController>
    {
        private readonly IExternalSourceFactory<MarketplaceItemModel> _sourceFactory;
        private readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;

        public ExternalSourceController(
            IExternalSourceFactory<MarketplaceItemModel> sourceFactory,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            UserDAL dalUser,
            ConfigUtil config, ILogger<ExternalSourceController> logger)
            : base(config, logger, dalUser)
        {
            _sourceFactory = sourceFactory;
            _dalExternalSource = dalExternalSource;
            _dalAnalytics = dalAnalytics;
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
            var sources = _dalExternalSource.Where(x => x.Code.ToLower().Equals(model.Code.ToLower()), null, null, false, false).Data;
            if (sources == null || sources.Count == 0)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|Invalid source : {model.Code}");
                return BadRequest($"Invalid source id");
            }
            else if (sources.Count > 1)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|External Source. Too many matches for {model.Code}");
                return BadRequest($"External Source. Too many matches.");
            }

            var src = sources[0];
            if (!src.Enabled) {
                _logger.LogWarning($"ExternalSourceController|GetByID|External Source. Source not enabled. {model.Code}");
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

            await IncrementAnalytics(result);

            return Ok(result);
        }

        /*One time data migration routine. Already run against prod db on 10/17
        [HttpPost, Route("ConvertAnalytics")]
        [ProducesResponseType(200, Type = typeof(MarketplaceItemModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ConvertAnalytics()
        {
            //convert analytics to new format
            var analytics = _dalAnalytics.Where(x =>
                x.ExternalSource == null && !string.IsNullOrEmpty(x.CloudLibId), null, null, false)
                .Data;
            if (analytics == null)
            {
            }
            else
            {
                foreach (var a in analytics)
                {
                    a.ExternalSource = new ExternalSourceSimple() { ID=a.CloudLibId, SourceId= "6525a74016a01652b87feae9", Code="cloudlib" };
                    await _dalAnalytics.Update(a, null);
                }
            }
            return Ok(analytics);
        }
        */

        private async Task IncrementAnalytics(MarketplaceItemModel item)
        {
            //Increment download Count
            //Check if CloudLib item is there if not add a new one then increment count and save
            MarketplaceItemAnalyticsModel analytic = _dalAnalytics.Where(x =>
                x.ExternalSource != null && !string.IsNullOrEmpty(x.ExternalSource.SourceId) && !string.IsNullOrEmpty(x.ExternalSource.ID) &&
                x.ExternalSource.ID == item.ExternalSource.ID && x.ExternalSource.SourceId == item.ExternalSource.SourceId, null, null, false)
                .Data.FirstOrDefault();
            if (analytic == null)
            {
                analytic = new MarketplaceItemAnalyticsModel() { ExternalSource = item.ExternalSource, PageVisitCount = 1 };
                await _dalAnalytics.Add(analytic, null);
            }
            else
            {
                analytic.PageVisitCount += 1;
                await _dalAnalytics.Update(analytic, null);
            }
        }
    }
}
