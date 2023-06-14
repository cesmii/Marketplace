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
using System.Collections.Generic;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped)), Route("api/admin/profile")]
    public class AdminProfileController : BaseController<AdminProfileController>
    {
        //private readonly IDal<MarketplaceItem, AdminMarketplaceItemModel> _dal;
        private readonly IAdminCloudLibDAL<AdminMarketplaceItemModelWithCursor> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;

        public AdminProfileController(IAdminCloudLibDAL<AdminMarketplaceItemModelWithCursor> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            UserDAL dalUser,
            ConfigUtil config, ILogger<AdminProfileController> logger)
            : base(config, logger, dalUser)
        {
            _dal = dal;
            _dalLookup = dalLookup;
        }

        #region Admin UI
        [HttpPost, Route("init")]
        [ProducesResponseType(200, Type = typeof(AdminMarketplaceItemModelWithCursor))]
        [ProducesResponseType(400)]
        public IActionResult Init()
        {
            var result = new AdminMarketplaceItemModelWithCursor();

            //pre-populate list of look up items for industry verts and categories
            //TBD - for now, we don't use this. uncomment this if we start capturing profile's verticals, processes
            /*
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
            */

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
        [ProducesResponseType(200, Type = typeof(AdminMarketplaceItemModelWithCursor))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByID([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminProfileController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = await _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"AdminProfileController|GetByID|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            return Ok(result);
        }

        /// <summary>
        /// Update an existing MarketplaceItem that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Upsert")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Upsert([FromBody] AdminMarketplaceItemModelWithCursor model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminProfileController|Update|Invalid model");
                return BadRequest("Marketplace|Update|Invalid model");
            }
            var record = await _dal.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"AdminProfileController|Update|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            else
            {
                var result = await _dal.Upsert(model, UserID);
                if (result < 0)
                {
                    _logger.LogWarning($"AdminProfileController|Update|Could not update marketplaceItem. Invalid id:{model.ID}.");
                    return BadRequest("Could not update profile MarketplaceItem. Invalid id.");
                }
                _logger.LogInformation($"AdminProfileController|Update|Updated MarketplaceItem. Id:{model.ID}.");

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
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {
            var result = await _dal.Delete(model.ID.ToString(), UserID);
            if (result < 0)
            {
                _logger.LogWarning($"AdminProfileController|Delete|Could not remove relationships. Invalid id:{model.ID}.");
                return BadRequest("Could not remove relationships. Invalid id.");
            }
            _logger.LogInformation($"AdminProfileController|Delete|Removed relationships. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Relationships were removed." });
        }

        /*
        /// <summary>
        /// Add an item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] AdminMarketplaceItemModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminProfileController|Add|Invalid model");
                return BadRequest("Marketplace|Add|Invalid model");
            }
            else
            {
                var result = await _dal.Add(model, UserID);
                if (String.IsNullOrEmpty(result))
                {
                    _logger.LogWarning($"AdminProfileController|Add|Could not add MarketplaceItem");
                    return BadRequest("Could not add MarketplaceItem. Invalid id.");
                }
                _logger.LogInformation($"AdminProfileController|Add|Add MarketplaceItem item. Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Item was added.",
                    Data = result //id of added value
                });
            }

        }
        */

        /// <summary>
        /// Admin Search for marketplace items matching criteria passed in. This is an advanced search and the front end
        /// would pass a collection of fields, operators, values to use in the search.  
        /// The admin difference is that it will not include CloudLib profiles in the search
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Search")]
        [ProducesResponseType(200, Type = typeof(List<MarketplaceItemModel>))]
        public async Task<IActionResult> Search([FromBody] MarketplaceSearchModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminProfileController|Search|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var cats = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.FirstOrDefault(x => x.EnumValue == LookupTypeEnum.Process).Items.Where(x => x.Selected).ToList();
            var verts = model.Filters.Count == 0 ? new List<LookupItemFilterModel>() : model.Filters.FirstOrDefault(x => x.EnumValue == LookupTypeEnum.IndustryVertical).Items.Where(x => x.Selected).ToList();

            //DAL gets only the items that have related items in local db. 
            var result = string.IsNullOrEmpty(model.Query) && cats.Count == 0 && verts.Count == 0 ?
                await _dal.GetAll() :
                await _dal.Where(model.Query, model.Skip,model.Take,null, null, false, null,  
                    cats.Select(x => x.Name.ToLower()).ToList(), verts.Select(x => x.Name.ToLower()).ToList());

            if (result == null)
            {
                _logger.LogWarning($"AdminProfileController|Search|No records found.");
                return BadRequest($"No records found.");
            }

            return Ok(result);
        }

        #endregion

    }


}
