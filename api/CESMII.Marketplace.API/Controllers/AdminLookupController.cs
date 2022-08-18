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
using CESMII.Marketplace.Data.Extensions;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Common.Utils;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize(), Route("api/admin/lookup")]
    public class AdminLookupController : BaseController<AdminLookupController>
    {

        private readonly IDal<LookupItem, LookupItemModel> _dal;

        public AdminLookupController(
            IDal<LookupItem, LookupItemModel> dal,
            UserDAL dalUser,
            ConfigUtil config, ILogger<AdminLookupController> logger) 
            : base(config, logger, dalUser)
        {
            _dal = dal;
        }


        [HttpPost, Route("init")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(LookupItemModel))]
        [ProducesResponseType(400)]
        public IActionResult Init()
        {
            var result = new LookupItemModel();
            return Ok(result);
        }

        [HttpPost, Route("Search")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageRequestInfo))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(DALResult<LookupItemModel>))]
        [ProducesResponseType(400)]
        public IActionResult Search([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminLookupController|Search|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            //get all active and those matching certain look up types. 
            Func<LookupItem, bool> predicate = x => x.IsActive;
            predicate = predicate.And(x => x.LookupType != null && (
                                        x.LookupType.EnumValue == LookupTypeEnum.Process ||
                                        x.LookupType.EnumValue == LookupTypeEnum.IndustryVertical ||
                                        //x.LookupType.EnumValue == LookupTypeEnum.MarketplaceStatus ||
                                        x.LookupType.EnumValue == LookupTypeEnum.MembershipStatus ||
                                        x.LookupType.EnumValue == LookupTypeEnum.TaskStatus
                                    ));
            //now trim further by name if needed. 
            if (!string.IsNullOrEmpty(model.Query))
            {
                predicate = predicate.And(x => x.Name.ToLower().Contains(model.Query));
            }
            var result = _dal.Where(predicate, model.Skip, model.Take, true, false);

            return Ok(result);
        }

        [HttpPost, Route("types/all")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageRequestInfo))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(List<LookupTypeModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetLookupTypes()
        {
            //get look up types. 
            List<LookupTypeModel> result = new List<LookupTypeModel>() {
                new LookupTypeModel() { Name=EnumUtils.GetEnumDescription(LookupTypeEnum.Process), EnumValue = LookupTypeEnum.Process },
                new LookupTypeModel() { Name=EnumUtils.GetEnumDescription(LookupTypeEnum.IndustryVertical), EnumValue = LookupTypeEnum.IndustryVertical },
                //new LookupTypeModel() { Name=EnumUtils.GetEnumDescription(LookupTypeEnum.MarketplaceStatus), EnumValue = LookupTypeEnum.MarketplaceStatus },
                new LookupTypeModel() { Name=EnumUtils.GetEnumDescription(LookupTypeEnum.MembershipStatus), EnumValue = LookupTypeEnum.MembershipStatus },
                new LookupTypeModel() { Name=EnumUtils.GetEnumDescription(LookupTypeEnum.TaskStatus), EnumValue = LookupTypeEnum.TaskStatus }
            }
            .OrderBy(x => x.Name).ToList();

            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(LookupItemModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminLookupController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminLookupController|GetByID|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //if item deleted, then don't return item
            if (!result.IsActive)
            {
                _logger.LogWarning($"AdminLookupController|GetByID|Cannot edit deleted item: {model.ID}");
                return BadRequest($"Cannot edit deleted item: ID: {result.ID}, Display name: {result.Name}");
            }

            return Ok(result);
        }

        [HttpPost, Route("copy")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(LookupItemModel))]
        [ProducesResponseType(400)]
        public IActionResult CopyItem([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminLookupController|CopyItem|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminLookupController|CopyItem|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //clear out key values, then return as a new item
            result.ID = "";
            result.Name = $"{result.Name}-copy";

            return Ok(result);
        }

        /// <summary>
        /// Update an existing Publisher that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Update")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] LookupItemModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminLookupController|Update|Invalid model");
                return BadRequest("Publisher|Update|Invalid model");
            }
            var record = _dal.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"AdminLookupController|Update|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            else if (!IsValidNameUnique(model))
            {
                _logger.LogWarning($"AdminLookupController|Update|Name {model.Name} is already in use.");
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
                    _logger.LogWarning($"AdminLookupController|Update|Could not update item. Invalid id:{model.ID}.");
                    return BadRequest("Could not update profile Publisher. Invalid id.");
                }
                _logger.LogInformation($"AdminLookupController|Update|Updated Publisher Id:{model.ID}.");

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
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {
            //don't delete if this publisher has marketplace items.
            if (HasItems(model))
            {
                _logger.LogWarning($"AdminLookupController|Delete|Publisher {model.ID} has active marketplace items and cannot be deleted.");
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
                _logger.LogWarning($"AdminLookupController|Delete|Could not delete item. Invalid id:{model.ID}.");
                return BadRequest("Could not delete profile item. Invalid id.");
            }
            _logger.LogInformation($"AdminLookupController|Delete|Deleted Publisher. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }

        /// <summary>
        /// Add an item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] LookupItemModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminLookupController|Add|Invalid model");
                return BadRequest("AdminLookupController|Add|Invalid model");
            }
            else if (!IsValidNameUnique(model))
            {
                _logger.LogWarning($"AdminLookupController|Add|Name {model.Name} is already in use.");
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = $"Name '{model.Name}' is not unique. Please enter a unique name."
                });
            }
            else
            {
                var result = await _dal.Add(model, UserID);
                if (String.IsNullOrEmpty(result))
                {
                    _logger.LogWarning($"AdminLookupController|Add|Could not add item");
                    return BadRequest("Could not add item. Invalid id.");
                }
                _logger.LogInformation($"AdminLookupController|Add|Add Publisher. Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = result //id of added value
                });
            }

        }

        private bool IsValidNameUnique(LookupItemModel model)
        {
            //name is supposed to be unique. Note name is different than display name.
            //if we get a match for something other than this id, return false
            var numItems = _dal.Count(x => x.IsActive && !x.ID.Equals(model.ID) &&
                x.Name.ToLower().Equals(model.Name.ToLower()) && 
                x.LookupType.EnumValue == model.LookupType.EnumValue);
            return numItems == 0;
        }

        private bool HasItems(IdStringModel model)
        {
            //it will be difficult to maintain checking all the other data collections for lookup usage
            //we will do a soft delete and leave the item present but hot shown in view. 
            //The admin will have to then go back and update any area which may have used that value. 
            return false;
        }


    }
}
