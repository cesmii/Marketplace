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

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize(), Route("api/admin/publisher")]
    public class AdminPublisherController : BaseController<AdminPublisherController>
    {

        private readonly IDal<Publisher, AdminPublisherModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly IDal<MarketplaceItem, MarketplaceItemModel> _dalMarketplaceItem;

        public AdminPublisherController(
            IDal<Publisher, AdminPublisherModel> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<MarketplaceItem, MarketplaceItemModel> dalMarketplaceItem,
            ConfigUtil config, ILogger<AdminPublisherController> logger) 
            : base(config, logger)
        {
            _dal = dal;
            _dalLookup = dalLookup;
            _dalMarketplaceItem = dalMarketplaceItem;
        }


        [HttpPost, Route("init")]
        [Authorize(Policy = nameof(PermissionEnum.CanManagePublishers))]
        [ProducesResponseType(200, Type = typeof(AdminPublisherModel))]
        [ProducesResponseType(400)]
        public IActionResult Init()
        {
            var result = new AdminPublisherModel();

            //pre-populate list of look up items for industry verts and categories
            var lookupItems = _dalLookup.Where(x => x.LookupType.EnumValue == LookupTypeEnum.IndustryVertical
                || x.LookupType.EnumValue == LookupTypeEnum.Process, null, null, false, false).Data;
            result.IndustryVerticals = lookupItems.Where(x => x.LookupType.EnumValue == LookupTypeEnum.IndustryVertical)
                .Select(itm => new LookupItemFilterModel
                {
                    ID = itm.ID,
                    Name = itm.Name,
                    IsActive = itm.IsActive,
                    DisplayOrder = itm.DisplayOrder
                }).ToList();

            result.Categories = lookupItems.Where(x => x.LookupType.EnumValue == LookupTypeEnum.Process)
                .Select(itm => new LookupItemFilterModel
                {
                    ID = itm.ID,
                    Name = itm.Name,
                    IsActive = itm.IsActive,
                    DisplayOrder = itm.DisplayOrder
                }).ToList();

            //default some values
            result.Name = "";
            result.DisplayName = "";
            result.Description = "";
            result.CompanyUrl = "";
            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        [Authorize(Policy = nameof(PermissionEnum.CanManagePublishers))]
        [ProducesResponseType(200, Type = typeof(AdminPublisherModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminPublisherController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminPublisherController|GetByID|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //if item deleted, then don't return item
            if (!result.IsActive)
            {
                _logger.LogWarning($"AdminPublisherController|GetByID|Cannot edit deleted item: {model.ID}");
                return BadRequest($"Cannot edit deleted item: ID: {result.ID}, Display name: {result.DisplayName}");
            }

            return Ok(result);
        }

        [HttpPost, Route("copy")]
        [Authorize(Policy = nameof(PermissionEnum.CanManagePublishers))]
        [ProducesResponseType(200, Type = typeof(AdminPublisherModel))]
        [ProducesResponseType(400)]
        public IActionResult CopyItem([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminPublisherController|CopyItem|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminPublisherController|CopyItem|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //clear out key values, then return as a new item
            result.ID = "";
            result.Name = $"{result.Name}-copy";
            result.DisplayName = $"{result.DisplayName} (Copy)";

            return Ok(result);
        }

        /// <summary>
        /// Update an existing Publisher that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Update")]
        [Authorize(Policy = nameof(PermissionEnum.CanManagePublishers))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] AdminPublisherModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminPublisherController|Update|Invalid model");
                return BadRequest("Publisher|Update|Invalid model");
            }
            var record = _dal.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"AdminPublisherController|Update|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            else if (!IsValidNameUnique(model))
            {
                _logger.LogWarning($"AdminPublisherController|Update|Name {model.Name} is already in use.");
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = $"Name '{model.Name}' is not unique. Please enter a unique name."
                });
            }
            else
            {
                var result = await _dal.Update(model, UserID);
                if (result < 0)
                {
                    _logger.LogWarning($"AdminPublisherController|Update|Could not update item. Invalid id:{model.ID}.");
                    return BadRequest("Could not update profile Publisher. Invalid id.");
                }
                _logger.LogInformation($"AdminPublisherController|Update|Updated Publisher Id:{model.ID}.");

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
        [Authorize(Policy = nameof(PermissionEnum.CanManagePublishers))]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {
            //don't delete if this publisher has marketplace items.
            if (HasMarketplaceItems(model))
            {
                _logger.LogWarning($"AdminPublisherController|Delete|Publisher {model.ID} has active marketplace items and cannot be deleted.");
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = $"Publisher has active marketplace items and cannot be deleted."
                });
            }
            //attempt delete
            var result = await _dal.Delete(model.ID.ToString(), UserID);
            if (result < 0)
            {
                _logger.LogWarning($"AdminPublisherController|Delete|Could not delete item. Invalid id:{model.ID}.");
                return BadRequest("Could not delete profile item. Invalid id.");
            }
            _logger.LogInformation($"AdminPublisherController|Delete|Deleted Publisher. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }

        /// <summary>
        /// Add an item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        [Authorize(Policy = nameof(PermissionEnum.CanManagePublishers))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] AdminPublisherModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminPublisherController|Add|Invalid model");
                return BadRequest("AdminPublisher|Add|Invalid model");
            }
            else if (!IsValidNameUnique(model))
            {
                _logger.LogWarning($"AdminPublisherController|Add|Name {model.Name} is already in use.");
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = $"Name '{model.Name}' is not unique. Please enter a unique name."
                });
            }
            else
            {
                var result = await _dal.Add(model, UserID);
                if (String.IsNullOrEmpty(result) == true)
                {
                    _logger.LogWarning($"AdminPublisherController|Add|Could not add item");
                    return BadRequest("Could not add item. Invalid id.");
                }
                _logger.LogInformation($"AdminPublisherController|Add|Add Publisher. Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = result //id of added value
                });
            }

        }

        private bool IsValidNameUnique(AdminPublisherModel model)
        {
            //name is supposed to be unique. Note name is different than display name.
            //if we get a match for something other than this id, return false
            var numItems = _dal.Count(x => x.IsActive && !x.ID.Equals(model.ID) &&
                x.Name.ToLower().Equals(model.Name.ToLower()));
            return numItems == 0;
        }

        private bool HasMarketplaceItems(IdStringModel model)
        {
            //name is supposed to be unique. Note name is different than display name.
            //if we get a match for something other than this id, return false
            var numItems = _dalMarketplaceItem.Count(x => x.IsActive && !x.PublisherId.Equals(model.ID));
            return numItems > 0;
        }


    }
}
