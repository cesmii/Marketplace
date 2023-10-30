using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Extensions;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.DAL.ExternalSources;
using CESMII.Marketplace.DAL.ExternalSources.Models;
using CESMII.Common.SelfServiceSignUp.Services;
using Microsoft.AspNetCore.Authorization;
using CESMII.Marketplace.Common.Enums;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class ExternalSourceController : BaseController<ExternalSourceController>
    {
        private readonly IExternalSourceFactory<MarketplaceItemModel> _sourceFactory;
        private readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;
        private readonly IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> _dalAnalytics;
        private readonly IDal<RequestInfo, RequestInfoModel> _dalRequestInfo;
        private readonly MailRelayService _mailRelayService;

        public ExternalSourceController(
            IExternalSourceFactory<MarketplaceItemModel> sourceFactory,
            IDal<ExternalSource, ExternalSourceModel> dalExternalSource,
            IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel> dalAnalytics,
            IDal<RequestInfo, RequestInfoModel> dalRequestInfo,
            MailRelayService mailRelayService,
            UserDAL dalUser,
            ConfigUtil config, ILogger<ExternalSourceController> logger)
            : base(config, logger, dalUser)
        {
            _sourceFactory = sourceFactory;
            _dalExternalSource = dalExternalSource;
            _dalAnalytics = dalAnalytics;
            _dalRequestInfo = dalRequestInfo;
            _mailRelayService = mailRelayService;
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

            //get the dal, then perform the get by id call
            var dalSource = await GetExternalSourceDAL(model.Code);
            var result = await dalSource.GetById(model.ID);

            if (result == null)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|Source not found. {model.SourceId}|{model.ID}");
                return BadRequest($"Item not found.");
            }

            await IncrementAnalytics(result, true, false);

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
                _logger.LogWarning($"ExternalSourceController|Export|Invalid model (null)");
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
                _logger.LogWarning($"ExternalSourceController|Export|Invalid model (Missing External Source Info)");
                return Ok(
                    new ResultMessageExportModel()
                    {
                        IsSuccess = false,
                        Message = "Invalid model (Missing SM Profile Info)"
                    }
                );
            }

            //get external source item (ie. nodeset) download
            var dalSource = await GetExternalSourceDAL(model.ExternalSource.Code);
            var result = await dalSource.Export(model.ExternalSource.ID);
            if (result == null)
            {
                _logger.LogWarning($"External source item not found: Id: {model.ExternalSource.ID}, SourceId: {model.ExternalSource.SourceId}, Code:{model.ExternalSource.Code}");
                return Ok(
                    new ResultMessageExportModel()
                    {
                        IsSuccess = false,
                        Message = "Download - no item found."
                    }
                );
            }

            await IncrementAnalytics(result.Item, false, true);

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
                    await SaveRequestInfo(model, result.Item);

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
                Data = result.Data,
                //return with result for caching client side. 
                Uid = model.Uid,
                Warnings = null
            });
        }

        /*
        /// <summary>
        /// Admin Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// The admin difference is that it will not include CloudLib profiles in the search
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Search/Admin")]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped))]
        [ProducesResponseType(200, Type = typeof(DALResult<AdminMarketplaceItemModel>))]
        public async Task<IActionResult> AdminSearch([FromBody] MarketplaceSearchModel model)
        {
            throw new InvalidOperationException("AdminSearch - no longer supported from here");
            if (model == null)
            {
                _logger.LogWarning($"ExternalSourceController|AdminSearch|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //Search external source
            //get external source dal 
            var dalSource = await GetExternalSourceDAL("cloudlib");
            var result = await dalSource.Where(model.Query, new SearchCursor() { Skip = model.Skip, Take = model.Take });
            return Ok(result);
        }
        */

        private async Task<IExternalDAL<MarketplaceItemModel>> GetExternalSourceDAL(string code)
        {
            code = code.ToLower();
            //get external source config, then instantiate object using info in the config by 
            //calling external source factory.
            //get by name - we have to ensure our external sources config data maintains a unique name.
            var sources = _dalExternalSource.Where(x => x.Code.ToLower().Equals(code), null, null, false, false).Data;
            if (sources == null || sources.Count == 0)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|Invalid source : {code}");
                throw new ArgumentException("Invalid source code");
            }
            else if (sources.Count > 1)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|External Source. Too many matches for {code}");
                throw new ArgumentException("Too many source code matches");
            }

            var src = sources[0];
            if (!src.Enabled)
            {
                _logger.LogWarning($"ExternalSourceController|GetByID|External Source. Source not enabled. {code}");
                throw new ArgumentException("Invalid source code");
            }

            //now perform the get by id call
            return await _sourceFactory.InitializeSource(src);
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


        protected async Task SaveRequestInfo(RequestInfoModel model, MarketplaceItemModel item)
        {
            var result = await _dalRequestInfo.Add(model, null);
            //error occurs on Add, don't stop download.
            if (String.IsNullOrEmpty(result))
            {
                _logger.LogWarning($"ExternalSourceController|Add|Could not add RequestInfo");
            }
            else
            {
                _logger.LogInformation($"ExternalSourceController|Add|Add RequestInfo. Id:{model.ID}.");
            }

            //Email to CESMII to notify them of new inquiry.
            //Don't fail to user submitting request if email send fails, log critical. 
            try
            {
                //populate some fields that may not be present on the add model. (request type code, created date). 
                var modelNew = _dalRequestInfo.GetById(result);

                //we are adding a request info with an smprofile, get the associated sm profile.
                modelNew.ExternalSource = new ExternalSourceSimpleInfo() 
                { Code = item.ExternalSource.Code, ID = item.ExternalSource.ID, SourceId = item.ExternalSource.SourceId };

                var subject = REQUESTINFO_SUBJECT.Replace("{{RequestType}}", "Download Nodeset Notification");
                var body = await this.RenderViewAsync("~/Views/Template/RequestInfo.cshtml", modelNew);
                var emailResult = await EmailRequestInfo(subject, body, _mailRelayService);

                if (!emailResult)
                {
                    _logger.LogCritical($"ExternalSourceController|Add|RequestInfo Item added (good)|Error: send failed.");
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"ExternalSourceController|Add|RequestInfo Item added (good)|Error: Email send error: {e.Message}.");
            }
        }

        private async Task IncrementAnalytics(MarketplaceItemModel item, bool incrementPageVisitCount, bool incrementDownloadCount)
        {
            //Increment download Count
            //Check if CloudLib item is there if not add a new one then increment count and save
            MarketplaceItemAnalyticsModel analytic = _dalAnalytics.Where(x =>
                x.ExternalSource != null && !string.IsNullOrEmpty(x.ExternalSource.SourceId) && !string.IsNullOrEmpty(x.ExternalSource.ID) &&
                x.ExternalSource.ID == item.ExternalSource.ID && x.ExternalSource.SourceId == item.ExternalSource.SourceId, null, null, false)
                .Data.FirstOrDefault();
            if (analytic == null)
            {
                analytic = new MarketplaceItemAnalyticsModel() { ExternalSource = item.ExternalSource, 
                    PageVisitCount = incrementPageVisitCount ? 1 : 0,
                    DownloadCount = incrementDownloadCount ? 1 : 0, 
                };
                await _dalAnalytics.Add(analytic, null);
            }
            else
            {
                if (incrementPageVisitCount) analytic.PageVisitCount += 1;
                if (incrementDownloadCount) analytic.DownloadCount += 1;
                await _dalAnalytics.Update(analytic, null);
            }
        }
    }
}
