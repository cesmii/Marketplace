using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.Marketplace.Common;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Api.Shared.Extensions;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class AuthController : BaseController<AuthController>
    {
        private readonly UserDAL _dal;
        //private readonly TokenUtils _tokenUtils;

        public AuthController(UserDAL dal, ConfigUtil config, ILogger<AuthController> logger) 
            : base(config, logger, dal)
        {
            _dal = dal;
            //_tokenUtils = tokenUtils;
        }

        [HttpPost, Route("onAADLogin")]
        [Authorize(Roles = "cesmii.marketplace.user")]
        public IActionResult OnAADLogin()
        {
            //extract user name from identity passed in via token
            //check if that user record is in DB. If not, add it.
            //InitLocalUser: this property checks for user, adds to db and returns a fully formed user model if one does not exist. 
            var user = InitLocalUser(); 
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = $"On AAD Login, marketplace user {user.ObjectIdAAD} was initialized." });
        }


        /// <summary>
        /// On successful Azure AD login, front end will call this to initialize the user in our DB (if not already there).
        /// Once this happens, then subsequent calls will expect user record is already and just ask for id. We won't have multiple 
        /// parallel calls trying to create user locally.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected UserModel InitLocalUser()
        {
            UserModel result = null;

            //extract user name from identity passed in via token
            //check if that user record is in DB. If not, add it.
            var userAAD = User.GetUserAAD();
            var matches = _dalUser.Where(x => x.ObjectIdAAD.ToLower().Equals(userAAD.ObjectIdAAD), null, null).Data;
            switch (matches.Count)
            {
                case 1:
                    result = matches[0];
                    result.LastLogin = DateTime.UtcNow;
                    result.DisplayName = userAAD.DisplayName;
                    _dalUser.Update(matches[0], userAAD.ObjectIdAAD).Wait();
                    break;
                case 0:
                    result = new UserModel()
                    {
                        ObjectIdAAD = userAAD.ObjectIdAAD,
                        DisplayName = userAAD.DisplayName,
                        LastLogin = DateTime.UtcNow
                    };
                    result.ID = _dalUser.Add(result, null).Result;
                    break;
                default:
                    _logger.LogWarning($"InitLocalUser||More than one Profile designer user record found with user name {userAAD.ObjectIdAAD}.");
                    throw new ArgumentNullException($"InitLocalUser: More than one Profile designer record user found with user name {userAAD.ObjectIdAAD}.");
            }

            return result;

        }
        
        /*
        [AllowAnonymous, HttpPost, Route("Login")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = new ResultMessageWithDataModel(){IsSuccess = false, Message = "", Data = null };

            if (string.IsNullOrEmpty(model.UserName))
            {
                result.Message = "Please supply the required username.";
                return Ok(result);
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                result.Message = "Please supply the required password.";
                return Ok(result);
            }

            var user = await _dal.Validate(model.UserName, model.Password);
            if (user == null)
            {
                result.Message = "Invalid user name or password. Please try again.";
                return Ok(result);
            }

            var tokenModel = _tokenUtils.BuildToken(user);
            result.IsSuccess = true;
            result.Data = new LoginResultModel() {
                Token = tokenModel.Token,
                IsImpersonating = tokenModel.IsImpersonating,
                User = user
            };

            return Ok(result);
        }

        [HttpPost, Route("changepassword")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            if (string.IsNullOrEmpty(model.OldPassword))
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = "Old Password is required."
                });
            }

            if (string.IsNullOrEmpty(model.NewPassword))
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = "New Password is required."
                });
            }

            var user = _dal.GetById(User.GetUserID());
            if (user == null)
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = "User was not found."
                });
            }

            // If we get here, update the user data with new password
            var result = await _dal.ChangePassword(user.ID, model.OldPassword, model.NewPassword);
            //update token given new password
            var tokenModel = _tokenUtils.BuildToken(user);

            return Ok(new ResultMessageWithDataModel()
            {
                IsSuccess = result,
                Message = result ? "Password updated" : "Old password does not match",
                Data = tokenModel.Token
            });
        }

        [HttpPost, Route("ExtendToken")]
        [ProducesResponseType(200, Type = typeof(TokenModel))]
        public IActionResult ExtendToken()
        {
            if (User.IsImpersonating())
            {
                // So little tricky bit here, because we "believe" the UserID to be the org ID we cannot use the base
                // UserID and must acquire this from the token directly; not the helper method.
                var realUser = _dal.GetById(User.GetRealUserID());

                // Refresh the token with the target user and org id.
                return Ok(_tokenUtils.BuildImpersonationToken(realUser, User.ImpersonationTargetUserID()));
            }
            else
            {
                var user = _dal.GetById(User.GetUserID());
                var newToken = _tokenUtils.BuildToken(user);
                return Ok(newToken);
            }
        }


        [AllowAnonymous]
        [HttpPost, Route("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] UserModel model)
        {
            throw new NotImplementedException();
        }
        */
    }
}
