
namespace CESMII.Marketplace.Api.Shared.Utils
{
    using System;
    using System.Net;
    using System.Net.Mail;

    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using SendGrid;
    using SendGrid.Helpers.Mail;

    using CESMII.Marketplace.Api.Shared.Extensions;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Common.Models;

    public class MailRelayService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly MailConfig _config;

        public MailRelayService(ConfigUtil configUtil)
        {
            _config = configUtil.MailSettings;
        }

        public bool SendEmail(MailMessage message)
        {
            // Do not proceed further if mail relay is disabled.
            if (!_config.Enabled)
            {
                Logger.Warn("Mail Relay is disabled.");
                return true;
            }

            // Configure the SMTP client and send the message
            var client = new SmtpClient
            {
                Host = _config.Address,
                Port = _config.Port,

                // Use whatever SSL mode is set.
                EnableSsl = _config.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (_config.EnableSsl)
            {
                ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
            }

            Logger.Debug($"Email configuration | Server: {_config.Address} Port: {_config.Port} SSL: {_config.EnableSsl}");

            message.From = new MailAddress(_config.MailFromAddress, "SM Marketplace");

            // If Mail Relay is in debug mode set all addresses to the configuration file.
            if (_config.Debug)
            {
                Logger.Debug($"Mail relay is in debug mode. Redirecting target email to: {string.Join("|", _config.DebugToAddresses)}");
                message.To.Clear();
                foreach (var address in _config.DebugToAddresses)
                {
                    message.To.Add(address);
                }
            }

            else
            {
                message.To.Clear();
                foreach (var address in _config.ToAddresses)
                {
                    message.To.Add(address);
                }
            }

            // If the user has setup credentials, use them.
            if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
            {
                client.Credentials = new NetworkCredential(_config.Username, _config.Password);
                Logger.Debug("Credentials are set in app settings, will leverage for SMTP connection.");
            }

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                if (ex is SmtpException)
                {
                    Logger.Error("An SMTP exception occurred while attempting to relay mail.");
                }
                else
                {
                    Logger.Error("A general exception occured while attempting to relay mail.");
                }

                Logger.Error(ex.Message);
                return false;
            }
            finally
            {
                message.Dispose();
            }

            Logger.Info("Message relay complete.");
            return true;
        }

        public async Task<bool> SendEmailSendGrid(MailMessage message)
        {
            var client = new SendGridClient(_config.ApiKey);
            var from = new EmailAddress(_config.MailFromAddress, "SM Marketplace");
            var subject = message.Subject;
            List<EmailAddress> mailTo = new List<EmailAddress>();
            // If Mail Relay is in debug mode set all addresses to the configuration file.
            if (_config.Debug)
            {
                Logger.Debug($"Mail relay is in debug mode. Redirecting target email to: {string.Join("|", _config.DebugToAddresses)}");
                foreach (var address in _config.DebugToAddresses)
                {
                    mailTo.Add(new EmailAddress(address));
                }
            }
            else
            {
                foreach (var address in _config.ToAddresses)
                {
                    mailTo.Add(new EmailAddress(address));
                }
            }

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, mailTo, subject, null, message.Body);

            var response = await client.SendEmailAsync(msg);
            if (response != null)
            {
                return true;
            }
            return false;
        }

        //public string GenerateMessageBodyFromTemplate(RequestInfoModel model, string template = "Default")
        //{
        //    // TODO Add support for more than one template.

        //    var filePath = Directory.GetCurrentDirectory() + $"\\Templates\\Email\\{template}.html";
        //    var stream = new StreamReader(filePath);
        //    var fileText = stream.ReadToEnd();
        //    stream.Close();

        //    // Now replace each item.
        //    foreach (var property in model.GetType().GetProperties())
        //    {
        //        fileText = fileText.Replace($"[{property.Name}]", property.GetValue(model)?.ToString());
        //    }
        //    // TODO these are hardcoded for the default email template, in later versions we'll add better nesting iteration for this to be more automatic.
        //    fileText = fileText.Replace("[MembershipStatus.Name]", model.MembershipStatus.Name);
        //    fileText = fileText.Replace("[RequestType.Name]", model.RequestType == null ? "General" : model.RequestType.Name);

        //    if (model.MarketplaceItem != null)
        //    {
        //        fileText = fileText.Replace("[RelatedTo.DisplayName]", model.MarketplaceItem.DisplayName);
        //        fileText = fileText.Replace("[RelatedTo.Abstract]", model.MarketplaceItem.Abstract);
        //    }
        //    else if (model.Publisher != null)
        //    {
        //        fileText = fileText.Replace("[RelatedTo.DisplayName]", model.Publisher.DisplayName);
        //        fileText = fileText.Replace("[RelatedTo.Abstract]", "");
        //    }
        //    else
        //    {
        //        fileText = fileText.Replace("[RelatedTo.DisplayName]", "N/A");
        //        fileText = fileText.Replace("[RelatedTo.Abstract]", "");
        //    }

        //    return fileText;
        //}
    }
}