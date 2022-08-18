using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Common.Enums;

namespace CESMII.Marketplace.Api.Controllers
{
    [Authorize, Route("api/[controller]")]
    public class ImageController : BaseController<ImageController>
    {
        private readonly IDal<ImageItem, ImageItemModel> _dal;

        public ImageController(IDal<ImageItem, ImageItemModel> dal,
            UserDAL dalUser,
            ConfigUtil config, ILogger<ImageController> logger)
            : base(config, logger, dalUser)
        {
            _dal = dal;
        }


        [AllowAnonymous, HttpGet, Route("{id}")]
        [ProducesResponseType(200, Type = typeof(FileContentResult))]
        [ProducesResponseType(400)]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)] //1 week cache
        public IActionResult GetById(string id)
        {
            if (id == null)
            {
                _logger.LogWarning($"ImageController|GetById|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(id);
            if (result == null)
            {
                _logger.LogWarning($"AdminImageController|GetById|No records found matching this ID: {id}");
                return BadRequest($"No records found matching this ID: {id}");
            }

            var base64 = result.Src.Replace($"data:{result.Type};base64,", "");

            return File(Convert.FromBase64String(base64), result.Type);
        }

        #region Admin Image Endpoints

        [HttpPost, Route("all")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(List<ImageItemModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetImagesByMarketplaceItemId([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"AdminMarketplaceController|GetImagesByMarketplaceItemId|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }
            //get images assoc with this item AND any unassigned stock images
            var result = _dal.Where(x => x.MarketplaceItemId.ToString().Equals(model.ID) || x.MarketplaceItemId.ToString().Equals(Common.Constants.BSON_OBJECTID_EMPTY)
                                         , null, null, false, false).Data;
            if (result == null)
            {
                _logger.LogWarning($"AdminMarketplaceController|GetImagesByMarketplaceItemId|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            return Ok(result);
        }

        /// <summary>
        /// Update an existing image.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("update")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> UpdateImage([FromBody] ImageItemModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminMarketplaceController|UpdateImage|Invalid model");
                return BadRequest("Marketplace|UpdateImage|Invalid model");
            }
            var record = _dal.GetById(model.ID);
            if (record == null)
            {
                _logger.LogWarning($"AdminMarketplaceController|UpdateImage|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //check each image for uniqueness
            List<ImageItemModel> imgs = new List<ImageItemModel>();
            if (!IsValidImageUnique(imgs, out var message))
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = $"Duplicate. {message}"
                });
            }
            else
            {
                var result = await _dal.Update(model, UserID);
                if (result < 0)
                {
                    _logger.LogWarning($"AdminMarketplaceController|UpdateImage|Could not update image. Invalid id:{model.ID}.");
                    return BadRequest("Could not update image. Invalid id.");
                }
                _logger.LogInformation($"AdminMarketplaceController|UpdateImage|Updated MarketplaceItem. Id:{model.ID}.");

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Image was updated.",
                    Data = model.ID
                });
            }

        }

        /// <summary>
        /// Delete an existing image. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("delete")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> DeleteImage([FromBody] IdStringModel model)
        {
            var result = await _dal.Delete(model.ID.ToString(), UserID);
            if (result < 0)
            {
                _logger.LogWarning($"AdminMarketplaceController|DeleteImage|Could not delete image. Invalid id:{model.ID}.");
                return BadRequest("Could not delete image. Invalid id.");
            }
            _logger.LogInformation($"AdminMarketplaceController|DeleteImage|Deleted image. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Image was deleted." });
        }

        /// <summary>
        /// Add one or many images.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("add")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageMarketplace))]
        [Authorize(Roles = "cesmii.marketplace.marketplaceadmin")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> AddImage([FromBody] List<ImageItemModel> model)
        {
            if (model == null)
            {
                _logger.LogWarning("AdminMarketplaceController|AddImage|Invalid model");
                return BadRequest("Marketplace|AddImage|Invalid model");
            }

            //check each image for uniqueness
            else if (!IsValidImageUnique(model, out var message))
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = $"Duplicate. {message}"
                });
            }
            else
            {
                List<string> result = new List<string>();
                foreach (var img in model)
                {
                    var id = await _dal.Add(img, UserID);
                    if (string.IsNullOrEmpty(id))
                    {
                        _logger.LogWarning($"AdminMarketplaceController|AddImage|Could not add image");
                        return BadRequest("Could not add image. ");
                    }
                    result.Add(id);
                    _logger.LogInformation($"AdminMarketplaceController|AddImage|Add image. Id:{id}.");
                }

                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Image(s) added.",
                    Data = result //id of added value
                });
            }

        }

        private bool IsValidImageUnique(List<ImageItemModel> model, out string message)
        {
            var result = true;
            message = "";
            foreach (var img in model)
            {
                //if file name and image are already there, then don't permit dup
                var numItems = _dal.Count(x => !x.ID.Equals(img.ID) &&
                    x.FileName.ToLower().Equals(img.FileName.ToLower()) &&
                    (string.IsNullOrEmpty(img.MarketplaceItemId) ?
                    x.MarketplaceItemId.ToString().ToLower().Equals(Common.Constants.BSON_OBJECTID_EMPTY) :
                    x.MarketplaceItemId.ToString().ToLower().Equals(img.MarketplaceItemId.ToString().ToLower())) &&
                    x.Src.ToLower().Equals(img.Src.ToLower()));
                if (numItems > 0)
                {
                    _logger.LogWarning($"AdminMarketplaceController|AddImage|Duplicate image {img.FileName}.");
                    message = $"{message}Image with file name '{img.FileName}' and same image source already exists. This image can not be uploaded. ";
                };
            }
            return result;
        }

        #endregion
    }


}
