using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using CESMII.Marketplace.Api.Shared.Extensions;
using CESMII.Marketplace.Data.Repositories;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize(), Route("api/[controller]")]
    public class RequestInfoController : BaseController<RequestInfoController>
    {

        private readonly IDal<RequestInfo, RequestInfoModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
       
        public RequestInfoController(
            IDal<RequestInfo, RequestInfoModel> dal,
           // IDal<Publisher, AdminPublisherModel> dalAdmin,
            IDal<LookupItem, LookupItemModel> dalLookup,
            ConfigUtil config, ILogger<RequestInfoController> logger)
            : base(config, logger)
        {
            _dal = dal;
           _dalLookup = dalLookup;
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
        public IActionResult GetByID([FromBody] IdStringModel model)
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

                //TBD - add email capability here...

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
