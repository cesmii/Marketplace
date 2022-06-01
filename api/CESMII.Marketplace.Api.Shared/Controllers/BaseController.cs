﻿namespace CESMII.Marketplace.Api.Shared.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Net.Mail;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using NLog;

    using CESMII.Marketplace.Api.Shared.Extensions;
    using CESMII.Marketplace.Common.Models;
    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Api.Shared.Models;
    using System;

    public class BaseController<TController> : Controller where TController : Controller
    {
        protected bool _disposed = false;

        /// <summary>
        /// Logger available to all controllers, simplifies referencing but will show origin as BaseController.
        /// Useful for being lazy honestly, but better to log something than not at all.
        /// Simply use the new keyword to override.
        /// </summary>
        protected readonly ILogger<TController> _logger;

        protected string UserID => User.GetUserID();

        protected readonly ConfigUtil _configUtil;
        protected readonly MailConfig _mailConfig;

        public BaseController(ConfigUtil configUtil, ILogger<TController> logger)
        {
            _configUtil = configUtil;
            _mailConfig = configUtil.MailSettings;
            _logger = logger;
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
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
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

        protected async Task<bool> EmailRequestInfo(string body, Utils.MailRelayService _mailRelayService)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_mailConfig.MailFromAddress),
                Subject = $"CESMII - SM Marketplace - Request Info Item Submitted",
                Body = body,
                IsBodyHtml = true
            };

            switch (_mailConfig.Provider)
            {
                case "SMTP":
                    if (!_mailRelayService.SendEmail(message))
                    {
                        return false;
                    }

                    break;
                case "SendGrid":
                    if (!_mailRelayService.SendEmailSendGrid(message).Result)
                    {
                        return false;
                    }
                    break;
                default:
                    throw new InvalidOperationException("The configured email provider is invalid.");
            }

            return true;
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