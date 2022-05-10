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
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class ProfileController : BaseController<ProfileController>
    {
        private readonly ICloudLibDAL<MarketplaceItemModel> _dalCloudLib;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;

        public ProfileController(
            ICloudLibDAL<MarketplaceItemModel> dalCloudLib,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            ConfigUtil config, ILogger<ProfileController> logger)
            : base(config, logger)
        {
            _dalCloudLib = dalCloudLib;
            _dalAnalytics = dalAnalytics;
        }

        [HttpPost, Route("GetByID")]
        [ProducesResponseType(200, Type = typeof(MarketplaceItemModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByID([FromBody] IdStringWithTrackingModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = await _dalCloudLib.GetById(model.ID);

            MarketplaceItemAnalyticsModel analytic = null;
            if (result == null)
            {
                _logger.LogWarning($"ProfileController|GetById|No records found matching this ID: {model.ID}");
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

                    // _dalAnalytics.Update(analytic, model.ID);
                    // _dalAnalytics.Update(analytic, analytic.ID.ToString());
                }
                result.Analytics = analytic;
            }
            //TBD
            //get related items
            //var util = new MarketplaceUtil(_dal, _dalAnalytics);
            //result.SimilarItems = util.SimilarItems(result);

            return Ok(result);
        }

        /// <summary>
        /// Admin Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// The admin difference is that it will not include CloudLib profiles in the search
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Search/Admin")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [ProducesResponseType(200, Type = typeof(DALResult<MarketplaceItemModel>))]
        public async Task<IActionResult> AdminSearch([FromBody] MarketplaceSearchModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|AdminSearch|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //Search CloudLib.
            var result = await _dalCloudLib.Where(model.Query);
            return Ok(result);
        }

        /// <summary>
        /// Download a nodeset xml from CloudLib
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Returns the OPC UA models in XML format</returns>
        [HttpPost, Route("Export")]
        [ProducesResponseType(200, Type = typeof(ResultMessageExportModel))]
        public async Task<IActionResult> Export([FromBody] IdStringWithTrackingModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|Export|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = await _dalCloudLib.Export(model.ID);

            MarketplaceItemAnalyticsModel analytic = null;
            if (result == null)
            {
                _logger.LogWarning($"ProfileController|GetById|No nodeset data found matching this ID: {model.ID}");
                return Ok(
                    new ResultMessageExportModel()
                    {
                        IsSuccess = false,
                        Message = "Profile not found."
                    }
                );
            }
            if (model.IsTracking)
            {
                //Increment download Count
                //Check if CloudLib item is there if not add a new one then increment count and save
                analytic = _dalAnalytics.Where(x => x.CloudLibId.ToString() == model.ID, null, null, false).Data.FirstOrDefault();

                if (analytic == null)
                {
                    analytic = new MarketplaceItemAnalyticsModel() { CloudLibId = model.ID, PageVisitCount = 1, DownloadCount = 1 };
                    await _dalAnalytics.Add(analytic, null);

                }
                else
                {
                    analytic.DownloadCount += 1;
                    await _dalAnalytics.Update(analytic, model.ID);
                }
            }

            return Ok(new ResultMessageExportModel()
            {
                IsSuccess = true,
                Message = "",
                Data = result,
                Warnings = null
            });
        }
    }
}
