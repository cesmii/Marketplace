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
using CESMII.Marketplace.Common.Utils;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class ProfileController : BaseController<ProfileController>
    {
        private readonly ICloudLibDAL<MarketplaceItemModel> _dalCloudLib;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;
        private readonly IDal<RequestInfo, RequestInfoModel> _dalRequestInfo;
        private readonly MailRelayService _mailRelayService;

        public ProfileController(
            ICloudLibDAL<MarketplaceItemModel> dalCloudLib,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            IDal<RequestInfo, RequestInfoModel> dalRequestInfo,
            MailRelayService mailRelayService,
            ConfigUtil config, ILogger<ProfileController> logger)
            : base(config, logger)
        {
            _dalCloudLib = dalCloudLib;
            _dalAnalytics = dalAnalytics;
            _dalRequestInfo = dalRequestInfo;
            _mailRelayService = mailRelayService;
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
                }
                result.Analytics = analytic;
            }

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
        public async Task<IActionResult> Export([FromBody] RequestInfoModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|Export|Invalid model (null)");
                return Ok(
                    new ResultMessageExportModel()
                    {
                        IsSuccess = false,
                        Message = "Invalid model (null)"
                    }
                );
            }

            if (!model.SmProfileId.HasValue)
            {
                _logger.LogWarning($"ProfileController|Export|Invalid model (Missing SM Profile ID)");
                return Ok(
                    new ResultMessageExportModel()
                    {
                        IsSuccess = false,
                        Message = "Invalid model (Missing SM Profile ID)"
                    }
                );
            }

            //get nodeset to download
            var smProfile = await _dalCloudLib.Export(model.SmProfileId.ToString());

            if (smProfile == null)
            {
                _logger.LogWarning($"ProfileController|GetById|No nodeset data found matching this ID: {model.ID}");
                return Ok(
                    new ResultMessageExportModel()
                    {
                        IsSuccess = false,
                        Message = "SM Profile not found."
                    }
                );
            }

            //Increment download Count
            //Check if CloudLib item is there if not add a new one then increment count and save
            MarketplaceItemAnalyticsModel analytic = _dalAnalytics.Where(x => !string.IsNullOrEmpty(x.CloudLibId) &&
                x.CloudLibId.ToString() == model.SmProfileId.ToString(), null, null, false).Data.FirstOrDefault();
            if (analytic == null)
            {
                analytic = new MarketplaceItemAnalyticsModel() { CloudLibId = model.SmProfileId.ToString(), PageVisitCount = 1, DownloadCount = 1 };
                await _dalAnalytics.Add(analytic, null);

            }
            else
            {
                analytic.DownloadCount += 1;
                await _dalAnalytics.Update(analytic, null);
            }

            //add request info object so we capture who downloaded
            await SaveRequestInfo(model, smProfile.Item );

            return Ok(new ResultMessageExportModel()
            {
                IsSuccess = true,
                Message = "",
                Data = smProfile.NodesetXml,
                Warnings = null
            });
        }

        protected async Task SaveRequestInfo(RequestInfoModel model, MarketplaceItemModel smProfile)
        {
            var result = await _dalRequestInfo.Add(model, null);
            //error occurs on Add, don't stop download.
            if (String.IsNullOrEmpty(result))
            {
                _logger.LogWarning($"ProfileController|Add|Could not add RequestInfo");
            }
            else
            {
                _logger.LogInformation($"ProfileController|Add|Add RequestInfo. Id:{model.ID}.");
            }

            //Email to CESMII to notify them of new inquiry.
            //Don't fail to user submitting request if email send fails, log critical. 
            try
            {
                //populate some fields that may not be present on the add model. (request type code, created date). 
                var modelNew = _dalRequestInfo.GetById(result);

                //we are adding a request info with an smprofile, get the associated sm profile.
                modelNew.SmProfile = smProfile;

                var subject = REQUESTINFO_SUBJECT.Replace("{{RequestType}}", "Download Nodeset Notification");
                var body = await this.RenderViewAsync("~/Views/Template/RequestInfo.cshtml", modelNew);
                var emailResult = await EmailRequestInfo(subject, body, _mailRelayService);

                if (!emailResult)
                {
                    _logger.LogCritical($"ProfileController|Add|RequestInfo Item added (good)|Error: send failed.");
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"ProfileController|Add|RequestInfo Item added (good)|Error: Email send error: {e.Message}.");
            }
        }
    }
}
