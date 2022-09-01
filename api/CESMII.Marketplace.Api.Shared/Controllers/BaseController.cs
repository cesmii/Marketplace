namespace CESMII.Marketplace.Api.Shared.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Net.Mail;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using CESMII.Marketplace.Api.Shared.Extensions;
    using CESMII.Marketplace.Common.Models;
    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Api.Shared.Models;
    using CESMII.Marketplace.Common.Utils;
    using CESMII.Marketplace.DAL;
    using CESMII.Marketplace.DAL.Models;

    public class BaseController<TController> : Controller where TController : Controller
    {
        protected bool _disposed = false;
        protected const string REQUESTINFO_SUBJECT = "CESMII | SM Marketplace | {{RequestType}}";

        /// <summary>
        /// Logger available to all controllers, simplifies referencing but will show origin as BaseController.
        /// Useful for being lazy honestly, but better to log something than not at all.
        /// Simply use the new keyword to override.
        /// </summary>
        protected readonly ILogger<TController> _logger;
        protected readonly UserDAL _dalUser;
        protected readonly ConfigUtil _configUtil;
        protected readonly MailConfig _mailConfig;

        protected string UserID => LocalUser.ID;

        private UserModel _user; 
        protected UserModel LocalUser { 
            get {
                if (_user == null)
                {
                    _user = InitLocalUser();
                }
                return _user; 
            }
        }

        public BaseController(ConfigUtil configUtil, ILogger<TController> logger, UserDAL dalUser)
        {
            _configUtil = configUtil;
            _mailConfig = configUtil.MailSettings;
            _logger = logger;
            _dalUser = dalUser;
        }

        protected System.Text.StringBuilder ExtractModelStateErrors(bool logErrors = false, string delimiter = ", ")
        {
            var errs = new List<ErrorMessageModel>();
            foreach (var key in ModelState.Keys)
            {
                if (ModelState[key].Errors.Any())
                {
                    var errors = ModelState[key].Errors.Select(e => e.ErrorMessage).ToArray();
                    foreach (var e in errors)
                    {
                        errs.Add(new ErrorMessageModel()
                        {
                            FieldName = string.IsNullOrEmpty(key) ? "[Custom]" : key,
                            Message = e
                        });
                    }
                }
            }
            errs = errs.OrderBy(e => e.FieldName).ThenBy(e => e.Message).ToList();

            //optional logging
            var sb = new System.Text.StringBuilder();
            foreach (var e in errs)
            {
                if (sb.Length > 0) { sb.Append(delimiter); }
                sb.AppendLine($"{e.FieldName}::{e.Message}");
            }
            if (logErrors)
            {
                _logger.LogWarning(sb.ToString());
            }
            return sb;
        }

        protected async Task<bool> EmailRequestInfo(string subject, string body, MailRelayService _mailRelayService)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_mailConfig.MailFromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            return await _mailRelayService.SendEmail(message);
        }

        protected UserModel InitLocalUser() {

            UserModel result = null;

            //extract user name from identity passed in via token
            //check if that user record is in DB. If not, add it.
            var userAAD = User.GetUserAAD();
            var count = _dalUser.Count(x => x.ObjectIdAAD.ToLower().Equals(userAAD.ObjectIdAAD));
            switch (count)
            {
                case 1:
                    result = _dalUser.GetByIdAAD(userAAD.ObjectIdAAD);
                    if (result == null)
                    {
                        _logger.LogWarning($"OnAADLogin||Could not update existing user: {userAAD.ObjectIdAAD}.");
                        throw new ArgumentNullException($"On AAD Login, could not update existing user: {userAAD.ObjectIdAAD}.");
                    }
                    result.LastLogin = DateTime.UtcNow;
                    result.DisplayName  = userAAD.DisplayName;
                    _dalUser.Update(result, userAAD.ObjectIdAAD).Wait();
                    break;
                case 0:
                    result = new UserModel()
                    {
                        ObjectIdAAD = userAAD.ObjectIdAAD,
                        DisplayName = userAAD.DisplayName,
                        LastLogin = DateTime.UtcNow
                    };
                    result.ID = _dalUser.Add(result, userAAD.ObjectIdAAD).Result;
                    break;
                default:
                    _logger.LogWarning($"OnAADLogin||More than one Marketplace user found with user name {userAAD.ObjectIdAAD}.");
                    throw new ArgumentNullException($"On AAD Login, more than one Marketplace user found with user name {userAAD.ObjectIdAAD}.");
            }

            //apply add'l claims not stored in db
            //result.DisplayName = userAAD.DisplayName;
            result.UserName = userAAD.UserName;
            result.FirstName = userAAD.FirstName;
            result.LastName = userAAD.LastName;
            result.Email = userAAD.Email;
            result.Roles = userAAD.Roles;
            result.Scope = userAAD.Scope;
            result.TenantId = userAAD.TenantId;

            //return success message object
            return result;
        }

        /// <summary>
        /// Override this in the descendant classes to handle disposal of unmanaged resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            //only dispose once
            if (_disposed) return;

            //do clean up of resources
            _disposed = true;

            base.Dispose(disposing);
        }
    }
}