using System;
using System.Collections.Generic;
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
using CESMII.Marketplace.Data.Extensions;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped)), Route("api/admin/jobdefinition")]
    public class AdminJobDefinitionController : BaseController<AdminJobDefinitionController>
    {

        private readonly IDal<JobDefinition, JobDefinitionModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly IDal<MarketplaceItem, MarketplaceItemModel> _dalMarketplaceItem;

        public AdminJobDefinitionController(
            IDal<JobDefinition, JobDefinitionModel> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<MarketplaceItem, MarketplaceItemModel> dalMarketplaceItem,
            UserDAL dalUser,
            ConfigUtil config, ILogger<AdminJobDefinitionController> logger) 
            : base(config, logger, dalUser)
        {
            _dal = dal;
            _dalLookup = dalLookup;
            _dalMarketplaceItem = dalMarketplaceItem;
        }

        [HttpPost, Route("search")]
        [ProducesResponseType(200, Type = typeof(DALResult<JobDefinitionModel>))]
        [ProducesResponseType(400)]
        public IActionResult Search([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminJobDefinitionController|Search|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            //get all active and those matching certain look up types. 
            Func<JobDefinition, bool> predicate = x => x.IsActive;
            //now trim further by name if needed. 
            if (!string.IsNullOrEmpty(model.Query))
            {
                predicate = predicate.And(x => x.Name.ToLower().Contains(model.Query) ||
                                x.TypeName.ToLower().Contains(model.Query));
            }
            var result = _dal.Where(predicate, model.Skip, model.Take, true, false);

            return Ok(result);
        }

        [HttpPost, Route("init")]
        [ProducesResponseType(200, Type = typeof(JobDefinitionModel))]
        [ProducesResponseType(400)]
        public IActionResult Init()
        {
            var result = new JobDefinitionModel();

            //default some values
            result.Name = "";
            result.TypeName = "";
            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        [ProducesResponseType(200, Type = typeof(JobDefinitionModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminJobDefinitionController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminJobDefinitionController|GetByID|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            return Ok(result);
        }

        [HttpPost, Route("copy")]
        [ProducesResponseType(200, Type = typeof(JobDefinitionModel))]
        [ProducesResponseType(400)]
        public IActionResult CopyItem([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminJobDefinitionController|CopyItem|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminJobDefinitionController|CopyItem|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //clear out key values, then return as a new item
            result.ID = "";
            result.Name = $"{result.Name}-copy";
            result.TypeName = $"{result.TypeName} (Copy)";

            return Ok(result);
        }

        /// <summary>
        /// Update an existing JobDefinition that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Update")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] JobDefinitionModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminJobDefinitionController|Update|Invalid model");
                return BadRequest("JobDefinition|Update|Invalid model");
            }
            var record = _dal.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"AdminJobDefinitionController|Update|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            var isValidMessage = IsValidItem(model);
            if (!string.IsNullOrEmpty(isValidMessage))
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = isValidMessage
                });
            }
            else
            {
                var result = await _dal.Update(model, UserID);
                if (result < 0)
                {
                    _logger.LogWarning($"AdminJobDefinitionController|Update|Could not update item. Invalid id:{model.ID}.");
                    return BadRequest("Could not update JobDefinition. Invalid id.");
                }
                _logger.LogInformation($"AdminJobDefinitionController|Update|Updated JobDefinition Id:{model.ID}.");

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
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {
            //attempt delete
            var result = await _dal.Delete(model.ID.ToString(), UserID);
            if (result < 0)
            {
                _logger.LogWarning($"AdminJobDefinitionController|Delete|Could not delete item. Invalid id:{model.ID}.");
                return BadRequest("Could not delete profile item. Invalid id.");
            }
            _logger.LogInformation($"AdminJobDefinitionController|Delete|Deleted JobDefinition. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }

        /// <summary>
        /// Add an item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] JobDefinitionModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminJobDefinitionController|Add|Invalid model");
                return BadRequest("AdminJobDefinition|Add|Invalid model");
            }

            var isValidMessage = IsValidItem(model);
            if (!string.IsNullOrEmpty(isValidMessage))
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = isValidMessage
                });
            }
            else
            {
                var result = await _dal.Add(model, UserID);
                if (String.IsNullOrEmpty(result))
                {
                    _logger.LogWarning($"AdminJobDefinitionController|Add|Could not add item");
                    return BadRequest("Could not add item. Invalid id.");
                }
                _logger.LogInformation($"AdminJobDefinitionController|Add|Add JobDefinition. Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = result //id of added value
                });
            }
        }

        private string IsValidItem(JobDefinitionModel model)
        {
            var result = string.Empty;
            if (!IsValidTypeName(model))
            {
                var msg = "Invalid Type Name. Type name can only contain characters, periods and underscores.";
                result = msg;
                _logger.LogWarning($"AdminJobDefinitionController|Add|{msg}");
            }
            if (!IsValidDataJson(model))
            {
                var msg = "Data must be structured as Json. Please correct the data value.";
                result += string.IsNullOrEmpty(result) ? "" : ", " + msg;
                _logger.LogWarning($"AdminJobDefinitionController|Add|{msg}");
            }

            return result;
        }

        private bool IsValidTypeName(JobDefinitionModel model)
        {
            //no spaces, starts with char, no numbers, allows periods, underscores
            //var format = /^[a-zA-Z\._]+$/; 
            return System.Text.RegularExpressions.Regex.IsMatch(model.TypeName, @"^[a-zA-Z\._]+$");
        }

        private bool IsValidDataJson(JobDefinitionModel model)
        {
            try
            {
                Newtonsoft.Json.JsonConvert.DeserializeObject(model.Data);
                return true;
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                _logger.LogWarning($"AdminJobDefinitionController|IsValidDataJson|{e.Message}.");
                return false;
            }
        }


    }
}
