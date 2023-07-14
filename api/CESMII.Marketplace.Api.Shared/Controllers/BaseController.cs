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
    using CESMII.Common.SelfServiceSignUp.Services;

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
        protected readonly ConfigUtil _configUtil;
        protected readonly MailConfig _mailConfig;
        protected readonly UserDAL _dalUser;

        protected string UserID => LocalUser.ID;

        private UserModel _user;
        protected UserModel LocalUser
        {
            get
            {
                if (_user == null)
                {
                    _user = User.GetUserAAD(); // InitLocalUser();
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

        protected async Task<bool> EmailRequestInfo(string subject, string body, MailRelayService _mailRelayService, string[] astrEmailAddress, string[] astrName, bool[] abSendTo)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_mailConfig.MailFromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            int count = astrEmailAddress.Length;

            for (int i= 0; i < count; i++)
            {
                string strEmail = astrEmailAddress[i];
                string strName = "";
                if (astrName.Length > i)
                    strName = astrName[i];

                if (abSendTo[i])
                {
                    if (string.IsNullOrEmpty(strName))
                    {
                        message.To.Add(new MailAddress(strEmail));
                    }
                    else
                    {
                        message.To.Add(new MailAddress(strEmail, strName));
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(strName) && !string.IsNullOrEmpty(strEmail))
                    {
                        message.CC.Add(new MailAddress(strEmail));
                    }
                    else if (!string.IsNullOrEmpty(strName) && !string.IsNullOrEmpty(strEmail))
                    {
                        message.CC.Add(new MailAddress(strEmail, strName));
                    }
                }
            }

            return await _mailRelayService.SendEmail(message);
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