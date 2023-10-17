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
using CESMII.Marketplace.DAL.ExternalSources;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Extensions;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Common.SelfServiceSignUp.Services;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class ProfileController : BaseController<ProfileController>
    {
        private readonly IExternalDAL<MarketplaceItemModel> _dalCloudLib;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;
        private readonly IDal<RequestInfo, RequestInfoModel> _dalRequestInfo;
        private readonly MailRelayService _mailRelayService;
        private readonly IExternalSourceFactory<MarketplaceItemModel> _sourceFactory;
        private readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;

        public ProfileController(
            IExternalDAL<MarketplaceItemModel> dalCloudLib,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            IDal<RequestInfo, RequestInfoModel> dalRequestInfo,
            UserDAL dalUser,
            IExternalSourceFactory<MarketplaceItemModel> sourceFactory,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            MailRelayService mailRelayService,
            ConfigUtil config, ILogger<ProfileController> logger)
            : base(config, logger, dalUser)
        {
            _dalCloudLib = dalCloudLib;
            _dalAnalytics = dalAnalytics;
            _dalRequestInfo = dalRequestInfo;
            _mailRelayService = mailRelayService;
            _dalExternalSource = dalExternalSource;
            _sourceFactory = sourceFactory;
        }

        /// <summary>
        /// Admin Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// The admin difference is that it will not include CloudLib profiles in the search
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Search/Admin")]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped))]
        [ProducesResponseType(200, Type = typeof(DALResult<MarketplaceItemModel>))]
        public async Task<IActionResult> AdminSearch([FromBody] MarketplaceSearchModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|AdminSearch|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //Search CloudLib.
            var result = await _dalCloudLib.Where(model.Query, new SearchCursor() { Skip = model.Skip, Take = model.Take });
            return Ok(result);
        }

        //TBD - move this over to external source controller.
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

            if (model.ExternalSource == null)
            {
                _logger.LogWarning($"ProfileController|Export|Invalid model (Missing SM Profile ID)");
                return Ok(
                    new ResultMessageExportModel()
                    {
                        IsSuccess = false,
                        Message = "Invalid model (Missing SM Profile Info)"
                    }
                );
            }

            //get nodeset to download
            var itemExport = await ExportExternalSourceItem(model.ExternalSource);

            if (itemExport == null)
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
            MarketplaceItemAnalyticsModel analytic = _dalAnalytics.Where(x => 
                x.ExternalSource != null && !string.IsNullOrEmpty(x.ExternalSource.SourceId) && !string.IsNullOrEmpty(x.ExternalSource.ID) &&
                x.ExternalSource.ID == model.ExternalSource.ID && x.ExternalSource.SourceId == model.ExternalSource.SourceId, null, null, false)
                .Data.FirstOrDefault();
            if (analytic == null)
            {
                analytic = new MarketplaceItemAnalyticsModel() { ExternalSource = model.ExternalSource, PageVisitCount = 1, DownloadCount = 1 };
                await _dalAnalytics.Add(analytic, null);
            }
            else
            {
                analytic.DownloadCount += 1;
                await _dalAnalytics.Update(analytic, null);
            }

            try
            {
                //add request info object so we capture who downloaded
                //if uid is populated, then decrypt and use that - it contains email, f name, l name.
                var delimiter = "$";
                var key = _configUtil.PasswordConfigSettings.EncryptionSettings.EncryptDecryptRequestInfoKey;
                if (!string.IsNullOrEmpty(key))
                {
                    string[] uid = String.IsNullOrEmpty(model.Uid) ? null : PasswordUtils.DecryptString(model.Uid, key).Split(delimiter);
                    model.Email = uid == null || uid.Length < 1 ? model.Email : uid[0];
                    model.FirstName = uid == null || uid.Length < 2 ? model.FirstName : uid[1];
                    model.LastName = uid == null || uid.Length < 1 ? model.LastName : uid[2];

                    //save the request info in the DB and notify via email
                    await SaveRequestInfo(model, itemExport.Item);

                    //encrypt data for repeat scenario
                    model.Uid = PasswordUtils.EncryptString($"{model.Email}{delimiter}{model.FirstName}{delimiter}{model.LastName}", key);
                }
                else
                {
                    _logger.LogError($"Cannot submit request info: no encryption key configured.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Cannot submit request info: internal error.");
            }
            return Ok(new ResultMessageExportModel()
            {
                IsSuccess = true,
                Message = "",
                Data = itemExport.Data,
                //return with result for caching client side. 
                Uid = model.Uid,
                Warnings = null
            });
        }

        protected async Task SaveRequestInfo(RequestInfoModel model, MarketplaceItemModel item)
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
                modelNew.ExternalSource = item.ExternalSource;

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

        private async Task<ExternalItemExportModel> ExportExternalSourceItem(ExternalSourceSimple item)
        {
            //get the source
            var src = _dalExternalSource.GetById(item.SourceId);
            if (src == null)
            {
                throw new ArgumentException($"External source not found: SourceId: {item.SourceId}, Code:{item.Code}");
            }

            //now get the source data 
            var dalSource = await _sourceFactory.InitializeSource(src);
            var result = await dalSource.Export(item.ID);
            if (result == null)
            {
                throw new ArgumentException($"External source item not found: Id: {item.ID}, SourceId: {item.SourceId}, Code:{item.Code}");
            }
            return result;
        }
    }
}
