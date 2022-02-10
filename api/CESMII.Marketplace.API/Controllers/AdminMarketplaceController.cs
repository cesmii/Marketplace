using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common.Enums;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize(), Route("api/admin/marketplace")]
    public class AdminMarketplaceController : BaseController<AdminMarketplaceController>
    {
        private readonly IDal<MarketplaceItem, AdminMarketplaceItemModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;

        public AdminMarketplaceController(IDal<MarketplaceItem, AdminMarketplaceItemModel> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            ConfigUtil config, ILogger<AdminMarketplaceController> logger)
            : base(config, logger)
        {
            _dal = dal;
            _dalLookup = dalLookup;
        }

        #region Admin UI
        [HttpPost, Route("init")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [ProducesResponseType(200, Type = typeof(AdminMarketplaceItemModel))]
        [ProducesResponseType(400)]
        public IActionResult Init()
        {
            var result = new AdminMarketplaceItemModel();

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
            result.Abstract = "";
            result.Description = "";
            result.Version = "";
            result.PublishDate = DateTime.Now.Date;
            
            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [ProducesResponseType(200, Type = typeof(AdminMarketplaceItemModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminMarketplaceController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminMarketplaceController|GetByID|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //if item deleted, then don't return item
            if (!result.IsActive)
            {
                _logger.LogWarning($"AdminMarketplaceController|GetByID|Cannot edit deleted item: {model.ID}");
                return BadRequest($"Cannot edit deleted item: ID: {result.ID}, Display name: {result.DisplayName}");
            }

            return Ok(result);
        }

        [HttpPost, Route("copy")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [ProducesResponseType(200, Type = typeof(AdminMarketplaceItemModel))]
        [ProducesResponseType(400)]
        public IActionResult CopyItem([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminMarketplaceController|CopyItem|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminMarketplaceController|CopyItem|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //clear out key values, then return as a new item
            result.ID = "";
            result.Created = DateTime.UtcNow;
            result.Updated = DateTime.UtcNow;
            result.Name = $"{result.Name}-copy";
            result.DisplayName = $"{result.DisplayName} (Copy)";
            result.Version = "";

            return Ok(result);
        }

        /// <summary>
        /// Update an existing MarketplaceItem that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Update")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] AdminMarketplaceItemModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminMarketplaceController|Update|Invalid model");
                return BadRequest("Marketplace|Update|Invalid model");
            }
            var record = _dal.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"AdminMarketplaceController|Update|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            else if (!IsValidNameUnique(model))
            {
                _logger.LogWarning($"AdminMarketplaceController|Update|Name {model.Name} is already in use.");
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
                    _logger.LogWarning($"AdminMarketplaceController|Update|Could not update marketplaceItem. Invalid id:{model.ID}.");
                    return BadRequest("Could not update profile MarketplaceItem. Invalid id.");
                }
                _logger.LogInformation($"AdminMarketplaceController|Update|Updated MarketplaceItem. Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was updated.",
                    Data = model.ID
                });
            }

        }

        /// <summary>
        /// Delete an existing profile. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Delete")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {
            var result = await _dal.Delete(model.ID.ToString(), UserID);
            if (result < 0)
            {
                _logger.LogWarning($"AdminMarketplaceController|Delete|Could not delete marketplaceItem. Invalid id:{model.ID}.");
                return BadRequest("Could not delete profile item. Invalid id.");
            }
            _logger.LogInformation($"AdminMarketplaceController|Delete|Deleted marketplaceItem. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }

        /// <summary>
        /// Add an item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] AdminMarketplaceItemModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminMarketplaceController|Add|Invalid model");
                return BadRequest("Marketplace|Add|Invalid model");
            }
            else if (!IsValidNameUnique(model))
            {
                _logger.LogWarning($"AdminMarketplaceController|Add|Name {model.Name} is already in use.");
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
                    _logger.LogWarning($"AdminMarketplaceController|Add|Could not add MarketplaceItem");
                    return BadRequest("Could not add MarketplaceItem. Invalid id.");
                }
                _logger.LogInformation($"AdminMarketplaceController|Add|Add MarketplaceItem item. Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = result //id of added value
                });
            }

        }

        private bool IsValidNameUnique(AdminMarketplaceItemModel model)
        {
            //name is supposed to be unique. Note name is different than display name.
            //if we get a match for something other than this id, return false
            var numItems = _dal.Count(x => x.IsActive && !x.ID.Equals(model.ID) &&
                x.Name.ToLower().Equals(model.Name.ToLower()));
            return numItems == 0;
        }

        #endregion

    }


}
