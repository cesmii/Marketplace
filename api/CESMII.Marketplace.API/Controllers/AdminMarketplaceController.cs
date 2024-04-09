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
using CESMII.Marketplace.Service;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize(Roles = "cesmii.marketplace.marketplaceadmin", Policy = nameof(PermissionEnum.UserAzureADMapped)), Route("api/admin/marketplace")]
    public class AdminMarketplaceController : BaseController<AdminMarketplaceController>
    {
        private readonly IDal<MarketplaceItem, AdminMarketplaceItemModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly IECommerceService<CartModel> _stripeService;

        public AdminMarketplaceController(IDal<MarketplaceItem, AdminMarketplaceItemModel> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IECommerceService<CartModel> stripeService,
            UserDAL dalUser,
            ConfigUtil config, ILogger<AdminMarketplaceController> logger)
            : base(config, logger, dalUser)
        {
            _dal = dal;
            _dalLookup = dalLookup;
            _stripeService = stripeService;
        }

        #region Admin UI
        [HttpPost, Route("init")]
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
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] AdminMarketplaceItemModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminMarketplaceController|Update|Invalid model");
                return BadRequest("Marketplace|Update|Invalid model");
            }

            if (model.AllowPurchase && model.Price == 0)
            {
                _logger.LogWarning("AdminMarketplaceController|Update|Price is missing");
                return BadRequest("AdminMarketplaceController|Update|Price is missing");
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
                //TBD - add call to Stripe service to add/update item
                //get back Stripe price id and save to our DB.
                //if Stripe fails, catch exception, log exception, and still save to our db
                try
                {
                    if (string.IsNullOrEmpty(model.PaymentProductId))
                    {
                        var newlyCreatedProduct = await _stripeService.AddProduct(model, UserID);
                        model.PaymentProductId = newlyCreatedProduct.Id;
                    }
                    else
                    {
                        await _stripeService.UpdateProduct(model, UserID);
                    }
                } catch (Exception ex)
                {
                    _logger.LogError(ex, "AdminMarketplaceController|Update|Failed to update the product in Stripe");
                }

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
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {
            //TBD - add call to Stripe service to remove item from catalog - only if it has payment product id
            //if Stripe fails, catch exception, log exception, and still save to our db
            
            try
            {
                var adminMarketplaceItem = _dal.GetById(model.ID);
                if (adminMarketplaceItem !=null && adminMarketplaceItem.IsActive && !string.IsNullOrEmpty(adminMarketplaceItem.PaymentProductId))
                {
                    await _stripeService.DeleteProduct(adminMarketplaceItem.PaymentProductId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminMarketplaceController|Delete|Failed to delete the product in Stripe");
            }

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
            else if (model.AllowPurchase && model.Price == 0)
            {
                _logger.LogWarning("AdminMarketplaceController|Add|Price is missing");
                return BadRequest("AdminMarketplaceController|Add|Price is missing");
            }
            else
            {
                //TBD - add call to Stripe service to add item to catalog
                //set paymentProductId on our model.PaymentProductId
                //if Stripe fails, catch exception, log exception, and still save to our db
                try
                {
                    var newlyCreatedProduct = await _stripeService.AddProduct(model, UserID);
                    model.PaymentProductId = newlyCreatedProduct.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AdminMarketplaceController|Update|Failed to update the product in Stripe");
                }

                var result = await _dal.Add(model, UserID);
                if (String.IsNullOrEmpty(result))
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
