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
    [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped)), Route("api/admin/externalsource")]
    public class AdminExternalSourceController : BaseController<AdminExternalSourceController>
    {

        private readonly IDal<ExternalSource, ExternalSourceModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly IDal<MarketplaceItem, MarketplaceItemModel> _dalMarketplaceItem;

        public AdminExternalSourceController(
            IDal<ExternalSource, ExternalSourceModel> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<MarketplaceItem, MarketplaceItemModel> dalMarketplaceItem,
            UserDAL dalUser,
            ConfigUtil config, ILogger<AdminExternalSourceController> logger)
            : base(config, logger, dalUser)
        {
            _dal = dal;
            _dalLookup = dalLookup;
            _dalMarketplaceItem = dalMarketplaceItem;
        }

        [HttpPost, Route("search")]
        [ProducesResponseType(200, Type = typeof(DALResult<ExternalSourceModel>))]
        [ProducesResponseType(400)]
        public IActionResult Search([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|Search|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            //get all active and those matching certain look up types. 
            Func<ExternalSource, bool> predicate = x => x.IsActive;
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
        [ProducesResponseType(200, Type = typeof(ExternalSourceModel))]
        [ProducesResponseType(400)]
        public IActionResult Init()
        {
            var result = new ExternalSourceModel();

            //default some values
            result.Name = "";
            result.AdminTypeName = "";
            result.TypeName = "";
            result.BaseUrl = "";
            result.Code = "";
            result.Enabled = true;
            result.Data = "{}";
            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        [ProducesResponseType(200, Type = typeof(ExternalSourceModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|GetByID|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            return Ok(result);
        }

        [HttpPost, Route("copy")]
        [ProducesResponseType(200, Type = typeof(ExternalSourceModel))]
        [ProducesResponseType(400)]
        public IActionResult CopyItem([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|CopyItem|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|CopyItem|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //clear out key values, then return as a new item
            result.ID = "";
            result.Name = $"{result.Name}-copy";
            result.AdminTypeName = $"{result.AdminTypeName} (Copy)";
            result.TypeName = $"{result.TypeName} (Copy)";
            result.Code = "";

            return Ok(result);
        }

        /// <summary>
        /// Update an existing ExternalSource that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Update")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] ExternalSourceModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminExternalSourceController|Update|Invalid model");
                return BadRequest("ExternalSource|Update|Invalid model");
            }
            var record = _dal.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"AdminExternalSourceController|Update|No records found matching this ID: {model.ID}");
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
                    _logger.LogWarning($"AdminExternalSourceController|Update|Could not update item. Invalid id:{model.ID}.");
                    return BadRequest("Could not update ExternalSource. Invalid id.");
                }
                _logger.LogInformation($"AdminExternalSourceController|Update|Updated ExternalSource Id:{model.ID}.");

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
                _logger.LogWarning($"AdminExternalSourceController|Delete|Could not delete item. Invalid id:{model.ID}.");
                return BadRequest("Could not delete profile item. Invalid id.");
            }
            _logger.LogInformation($"AdminExternalSourceController|Delete|Deleted ExternalSource. Id:{model.ID}.");

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
        public async Task<IActionResult> Add([FromBody] ExternalSourceModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminExternalSourceController|Add|Invalid model");
                return BadRequest("AdminExternalSource|Add|Invalid model");
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
                    _logger.LogWarning($"AdminExternalSourceController|Add|Could not add item");
                    return BadRequest("Could not add item. Invalid id.");
                }
                _logger.LogInformation($"AdminExternalSourceController|Add|Add ExternalSource. Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = result //id of added value
                });
            }
        }

        private string IsValidItem(ExternalSourceModel model)
        {
            var result = string.Empty;
            if (!IsValidTypeName(model.AdminTypeName))
            {
                var msg = "Invalid Admin Type Name. Admin type name can only contain characters, periods and underscores. ";
                result = msg;
                _logger.LogWarning($"AdminExternalSourceController|IsValidItem|{msg}");
            }
            if (!IsValidTypeName(model.TypeName))
            {
                var msg = "Invalid Type Name. Type name can only contain characters, periods and underscores. ";
                result += msg;
                _logger.LogWarning($"AdminExternalSourceController|IsValidItem|{msg}");
            }
            if (!IsValidCodeUnique(model))
            {
                result += "Code must be unique. ";
                _logger.LogWarning($"AdminExternalSourceController|IsValidItem|Code {model.Code} is already in use.");
            }
            if (!IsValidDataJson(model))
            {
                var msg = "Data must be structured as Json. Please correct the data value.";
                result += string.IsNullOrEmpty(result) ? "" : ", " + msg;
                _logger.LogWarning($"AdminExternalSourceController|IsValidItem|{msg}");
            }

            return result;
        }

        private bool IsValidTypeName(string val)
        {
            if (string.IsNullOrEmpty(val)) return true;
            //no spaces, starts with char, no numbers, allows periods, underscores
            //var format = /^[a-zA-Z\._]+$/; 
            return System.Text.RegularExpressions.Regex.IsMatch(val, @"^[a-zA-Z\._]+$");
        }

        private bool IsValidCodeUnique(ExternalSourceModel model)
        {
            //name is supposed to be unique. Note name is different than display name.
            //if we get a match for something other than this id, return false
            var numItems = _dal.Count(x => x.IsActive && !x.ID.Equals(model.ID) &&
                x.Code.ToLower().Equals(model.Code.ToLower()));
            return numItems == 0;
        }

        private bool IsValidDataJson(ExternalSourceModel model)
        {
            try
            {
                Newtonsoft.Json.JsonConvert.DeserializeObject(model.Data);
                return true;
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                _logger.LogWarning($"AdminExternalSourceController|IsValidDataJson|{e.Message}.");
                return false;
            }
        }


    }
}
