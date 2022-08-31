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
using CESMII.Marketplace.Api.Shared.Utils;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Extensions;

namespace CESMII.Marketplace.Api.Controllers
{
    
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : BaseController<UserController>
    {
        private readonly UserDAL _dal;

        public UserController(UserDAL dal,
            ConfigUtil config, ILogger<UserController> logger)
            : base(config, logger, dal)
        {
            _dal = dal;
        }


        [HttpGet, Route("All")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [Authorize(Roles = "cesmii.marketplace.useradmin")]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetAll()
        {
            var result = _dal.GetAll();
            if (result == null)
            {
                return BadRequest($"No records found.");
            }
            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [Authorize(Roles = "cesmii.marketplace.useradmin")]
        [ProducesResponseType(200, Type = typeof(UserModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdStringModel model)
        {
            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            //clear out SMIP settings so not sent to front end
            result.SmipSettings = null;
            return Ok(result);
        }

        /// <summary>
        /// This is a user getting their own profile
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("profile/mine")]
        [Authorize()]
        [ProducesResponseType(200, Type = typeof(UserModel))]
        [ProducesResponseType(400)]
        public IActionResult GetMine()
        {
            var result = LocalUser;
            if (result == null)
            {
                _logger.LogWarning($"UserController|GetMine|No records found matching this ID: {User.GetUserIdAAD()}.");
                return BadRequest($"No records found matching this ID: {User.GetUserIdAAD()}");
            }
            //clear out SMIP password so not sent to front end. Change password dialog will force user to enter existing password.
            result.SmipSettings.Password = "";
            return Ok(result);
        }

        /// <summary>
        /// This is a user getting their own profile - MSAL flow
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("profile/mine/msal")]
        [Authorize()]
        [ProducesResponseType(200, Type = typeof(UserModel))]
        [ProducesResponseType(400)]
        public IActionResult GetMineMsal()
        {
            //clear out SMIP password so not sent to front end. Change password dialog will force user to enter existing password.
            var result = LocalUser;
            result.SmipSettings.Password = "";
            return Ok(result);
        }

        [HttpPost, Route("Search")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [Authorize(Roles = "cesmii.marketplace.useradmin")]
        [ProducesResponseType(200, Type = typeof(DALResult<UserModel>))]
        public IActionResult Search([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                return BadRequest("User|Search|Invalid model");
            }

            if (string.IsNullOrEmpty(model.Query))
            {
                return Ok(_dal.GetAllPaged(model.Skip, model.Take, true));
            }

            model.Query = model.Query.ToLower();
            var result = _dal.Where(s =>
                            //string query section
                            //s.IsActive && 
                            (s.ObjectIdAAD.ToLower().Contains(model.Query) 
                            //|| (s.FirstName.ToLower() + s.LastName.ToLower()).Contains(
                            //    model.Query.Replace(" ", "").Replace("-", ""))),  //in case they search for code and name in one string.
                            ),
                            model.Skip, model.Take, true);
            return Ok(result);
        }

        /*
        [HttpPost, Route("Add")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [Authorize(Roles = "cesmii.marketplace.useradmin")]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        public async Task<IActionResult> Add([FromBody] UserModel model)
        {
            if (!ModelState.IsValid)
            {
                //var errors = ExtractModelStateErrors();
                return BadRequest("The user record is invalid. Please correct the following:...TBD - join errors collection into string list.");
            }

            var result = await _dal.Add(model, User.GetUserID());
            model.ID = result;
            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning($"Could not add user: {model.FirstName} {model.LastName}.");
                return BadRequest("Could not add user. ");
            }
            _logger.LogInformation($"Added user item. Id:{result}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was added." });
        }
        */

        /// <summary>
        /// This is an admin user updating someone else's profile.
        /// </summary>
        [HttpPost, Route("Update")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [Authorize(Roles = "cesmii.marketplace.useradmin")]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        public async Task<IActionResult> Update([FromBody] UserModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("The profile record is invalid. Please correct the following:...join errors collection into string list.");
            }

            var result = await _dal.Update(model, LocalUser.ID);
            if (result < 0)
            {
                _logger.LogWarning($"UserController|Update|Could not update user. Invalid id:{model.ID}.");
                return BadRequest("Could not update user. Invalid id.");
            }
            _logger.LogInformation($"UserController|Update|Updated user. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was updated." });
        }

        /// <summary>
        /// This is an individual user updating their own profile.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("profile/update")]
        [Authorize()]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        public async Task<IActionResult> UpdateProfile([FromBody] UserModel model)
        {
            var userId = LocalUser.ID;
            if (!userId.Equals(model.ID))
            {
                _logger.LogWarning($"UserController|UpdateProfile|User attempting to update another user.");
                return BadRequest("User can only update their own profile.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("The profile record is invalid. Please correct the following:...join errors collection into string list.");
            }

            var result = await _dal.Update(model, userId);
            if (result < 0)
            {
                _logger.LogWarning($"UserController|UpdateProfile|Could not update user. Invalid id:{model.ID}.");
                return BadRequest("Could not update user. Invalid id.");
            }
            _logger.LogInformation($"UserController|UpdateProfile|Updated user. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Account profile was updated." });
        }

        [HttpPost, Route("Delete")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [Authorize(Roles = "cesmii.marketplace.useradmin")]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {
            var result = await _dal.Delete(model.ID, LocalUser.ID);
            if (result < 0)
            {
                _logger.LogWarning($"Could not delete user. Invalid id:{model.ID}.");
                return BadRequest("Could not delete user. Invalid id.");
            }
            _logger.LogInformation($"Deleted user. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }

        [HttpPost, Route("smipSettings/changepassword")]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> ChangeSmipPassword([FromBody] ChangePasswordModel model)
        {
            if (string.IsNullOrEmpty(model.OldPassword))
            {
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = "Old Password is required."
                });
            }

            if (string.IsNullOrEmpty(model.NewPassword))
            {
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = "New Password is required."
                });
            }

            var user = _dal.GetByIdAAD(User.GetUserIdAAD());
            if (user == null)
            {
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = "User was not found."
                });
            }

            // If we get here, update the user data with new password
            var result = await _dal.ChangeSmipPassword(user.ID, model.OldPassword, model.NewPassword);

            return Ok(new ResultMessageModel()
            {
                IsSuccess = result,
                Message = result ? "Password updated" : "Old password does not match"
            });

        }

    }

}
