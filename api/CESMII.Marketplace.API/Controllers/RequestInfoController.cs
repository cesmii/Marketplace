using System;
using System.Linq;
using System.Net.Mail;
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
using CESMII.Marketplace.Api.Shared.Extensions;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Utils;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize(), Route("api/[controller]")]
    public class RequestInfoController : BaseController<RequestInfoController>
    {

        private readonly IDal<RequestInfo, RequestInfoModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly ICloudLibDAL<MarketplaceItemModel> _dalCloudLib;
        private readonly MailRelayService _mailRelayService;

        public RequestInfoController(
            IDal<RequestInfo, RequestInfoModel> dal,
            // IDal<Publisher, AdminPublisherModel> dalAdmin,
            IDal<LookupItem, LookupItemModel> dalLookup,
            ICloudLibDAL<MarketplaceItemModel> dalCloudLib,
            ConfigUtil config, ILogger<RequestInfoController> logger,
            MailRelayService mailRelayService)
            : base(config, logger)
        {
            _dal = dal;
            _dalLookup = dalLookup;
            _dalCloudLib = dalCloudLib;
            _mailRelayService = mailRelayService;
        }


        [HttpPost, Route("Search")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageRequestInfo))]
        [ProducesResponseType(200, Type = typeof(DALResult<RequestInfoModel>))]
        [ProducesResponseType(400)]
        public IActionResult Search([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"RequestInfoController|Search|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            var result = string.IsNullOrEmpty(model.Query) ?
                _dal.Where(x => x.IsActive
                            , model.Skip, model.Take, true, false) :
                _dal.Where(x => x.IsActive && (
                            x.FirstName.ToLower().Contains(model.Query) |
                            x.LastName.ToLower().Contains(model.Query) |
                            x.CompanyName.ToLower().Contains(model.Query) |
                            x.CompanyUrl.ToLower().Contains(model.Query) |
                            x.Description.ToLower().Contains(model.Query))
                            , model.Skip, model.Take, true, false);

            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageRequestInfo))]
        [ProducesResponseType(200, Type = typeof(RequestInfoModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByID([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"RequestInfoController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"RequestInfoController|GetById|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //if we are getting a request info of smprofile type, then also get the associated sm profile.
            //get request type id based on request type code
            if (result.SmProfileId.HasValue)
            {
                result.SmProfile = await _dalCloudLib.GetById(result.SmProfileId.Value.ToString());
            }

            return Ok(result);
        }



        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        [AllowAnonymous()]
        // [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] RequestInfoModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("RequestInfoController|Add|Invalid model");
                return BadRequest("RequestInfo|Add|Invalid model");
            }
            else
            {
                var result = await _dal.Add(model, null);
                if (String.IsNullOrEmpty(result) == true)
                {
                    _logger.LogWarning($"RequestInfoController|Add|Could not add RequestInfo");
                    return BadRequest("Could not add RequestInfo. Invalid id.");
                }
                _logger.LogInformation($"RequestInfoController|Add|Add RequestInfo. Id:{model.ID}.");

                //Email to CESMII to notify them of new inquiry.
                //Don't fail to user submitting request if email send fails, log critical. 
                try
                {
                    //populate some fields that may not be present on the add model. (request type code, created date). 
                    var modelNew = _dal.GetById(result);

                    var emailResult = await EmailRequestInfo(modelNew);

                    if (!emailResult)
                    {
                        _logger.LogCritical($"RequestInfoController|Add|RequestInfo Item added (good)|Error: send failed.");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical($"RequestInfoController|Add|RequestInfo Item added (good)|Error: Email send error: {e.Message}.");
                }

                //TODO https://tmsrandstadusa.atlassian.net/jira/software/c/projects/CMPT/issues/CMPT-94
                // take model and email.
                // include most relevant information from requestinfomodel and possibly a link based on the type.

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = model.ID
                });
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet, Route("EmailTest")]
        [AllowAnonymous()]
        // [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> EmailTest()
        {
            var model = _dal.GetAll().FirstOrDefault();
            if (model == null)
            {
                return BadRequest("RequestInfo|EmailTest|Invalid model");
            }

            //Email to CESMII to notify them of new inquiry.
            //Don't fail to user submitting request if email send fails, log critical. 
            try
            {
                var emailResult = await EmailRequestInfo(model);

                if (!emailResult)
                {
                    _logger.LogCritical($"RequestInfoController|EmailTest|Error: send failed.");
                    return Ok(new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = "Send email failed",
                        Data = model.ID
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"RequestInfoController|EmailTest|Error: Email send error: {e.Message}.");
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = e.Message,
                    Data = model.ID
                });
            }

            return Ok(new ResultMessageWithDataModel()
            {
                IsSuccess = true,
                Message = "Item was Emailed.",
                Data = model.ID
            });
        }

        private async Task<bool> EmailRequestInfo(RequestInfoModel item)
        {
            var body = await this.RenderViewAsync("~/Views/Template/RequestInfo.cshtml", item);
            var message = new MailMessage
            {
                From = new MailAddress(_mailConfig.MailFromAddress),
                Subject = $"CESMII - SM Marketplace - Request Info Item Submitted",
                Body = body,
                IsBodyHtml = true
            };

            switch (_mailConfig.Provider)
            {
                case "SMTP":
                    if (!_mailRelayService.SendEmail(message))
                    {
                        return false;
                    }

                    break;
                case "SendGrid":
                    if (!_mailRelayService.SendEmailSendGrid(message).Result)
                    {
                        return false;
                    }
                    break;
                default:
                    throw new InvalidOperationException("The configured email provider is invalid.");
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Update")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageRequestInfo))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] RequestInfoModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("RequestInfoController|Update|Invalid model");
                return BadRequest("RequestInfo|Update|Invalid model");
            }
            var record = _dal.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"RequestInfoController|Update|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            else
            {
                var result = await _dal.Update(model, UserID);
                if (result < 0)
                {
                    _logger.LogWarning($"RequestInfoController|Update|Could not update item. Invalid id:{model.ID}.");
                    return BadRequest("Could not update profile Publisher. Invalid id.");
                }
                _logger.LogInformation($"RequestInfoController|Update|Updated Publisher Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was updated.",
                    Data = model.ID
                });
            }

        }

        /// <summary>
        /// Delete an existing item. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Delete")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageRequestInfo))]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {
            //attempt delete
            var result = await _dal.Delete(model.ID.ToString(), UserID);
            if (result < 0)
            {
                _logger.LogWarning($"RequestInfoController|Delete|Could not delete item. Invalid id:{model.ID}.");
                return BadRequest("Could not delete profile item. Invalid id.");
            }
            _logger.LogInformation($"RequestInfoController|Delete|Deleted Publisher. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }

    }
}
