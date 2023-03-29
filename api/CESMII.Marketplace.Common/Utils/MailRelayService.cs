
namespace CESMII.Marketplace.Common.Utils
{
    using System;
    using System.Net;
    using System.Net.Mail;

    using System.Collections.Generic;
    using System.Threading.Tasks;

    using SendGrid;
    using SendGrid.Helpers.Mail;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Configuration;

    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Common.Models;

    //public class MailRelayService
    //{
    //    protected readonly ILogger<MailRelayService> _logger;
    //    private readonly MailConfig _config;

    //    public MailRelayService(
    //        ILogger<MailRelayService> logger,
    //        IConfiguration configuration)
    //    {
    //        _config = (new ConfigUtil(configuration)).MailSettings;
    //        _logger = logger;
    //    }

    //    public async Task<bool> SendEmail(MailMessage message)
    //    {
    //        switch (_config.Provider)
    //        {
    //            case "SMTP":
    //                if (!(await SendSmtpEmail(message)))
    //                {
    //                    return false;
    //                }

    //                break;
    //            case "SendGrid":
    //                if (!(await SendEmailSendGrid(message)))
    //                {
    //                    return false;
    //                }
    //                break;
    //            default:
    //                throw new InvalidOperationException("The configured email provider is invalid.");
    //        }
    //        return true;
    //    }

    //    private async Task<bool> SendSmtpEmail(MailMessage message)
    //    {
    //        // Do not proceed further if mail relay is disabled.
    //        if (!_config.Enabled)
    //        {
    //            _logger.LogWarning("Mail Relay is disabled.");
    //            return true;
    //        }

    //        // Configure the SMTP client and send the message
    //        var client = new SmtpClient
    //        {
    //            Host = _config.Address,
    //            Port = _config.Port,

    //            // Use whatever SSL mode is set.
    //            EnableSsl = _config.EnableSsl,
    //            DeliveryMethod = SmtpDeliveryMethod.Network
    //        };

    //        if (_config.EnableSsl)
    //        {
    //            ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
    //        }

    //        _logger.LogDebug($"Email configuration | Server: {_config.Address} Port: {_config.Port} SSL: {_config.EnableSsl}");

    //        message.From = new MailAddress(_config.MailFromAddress, "SM Marketplace");

    //        // If Mail Relay is in debug mode set all addresses to the configuration file.
    //        if (_config.Debug)
    //        {
    //            _logger.LogDebug($"Mail relay is in debug mode. Redirecting target email to: {string.Join("|", _config.DebugToAddresses)}");
    //            message.To.Clear();
    //            foreach (var address in _config.DebugToAddresses)
    //            {
    //                message.To.Add(address);
    //            }
    //        }

    //        else
    //        {
    //            message.To.Clear();
    //            foreach (var address in _config.ToAddresses)
    //            {
    //                message.To.Add(address);
    //            }
    //        }

    //        // If the user has setup credentials, use them.
    //        if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
    //        {
    //            client.Credentials = new NetworkCredential(_config.Username, _config.Password);
    //            _logger.LogDebug("Credentials are set in app settings, will leverage for SMTP connection.");
    //        }

    //        try
    //        {
    //            await client.SendMailAsync(message);
    //        }
    //        catch (Exception ex)
    //        {
    //            if (ex is SmtpException)
    //            {
    //                _logger.LogError("An SMTP exception occurred while attempting to relay mail.");
    //            }
    //            else
    //            {
    //                _logger.LogError("A general exception occured while attempting to relay mail.");
    //            }

    //            _logger.LogError(ex.Message);
    //            return false;
    //        }
    //        finally
    //        {
    //            message.Dispose();
    //        }

    //        _logger.LogInformation("Message relay complete.");
    //        return true;
    //    }

    //    private async Task<bool> SendEmailSendGrid(MailMessage message)
    //    {
    //        var client = new SendGridClient(_config.ApiKey);
    //        var from = new EmailAddress(_config.MailFromAddress, "SM Marketplace");
    //        var subject = message.Subject;
    //        var mailTo = new List<EmailAddress>();
    //        // If Mail Relay is in debug mode set all addresses to the configuration file.
    //        if (_config.Debug)
    //        {
    //            _logger.LogDebug($"Mail relay is in debug mode. Redirecting target email to: {string.Join("|", _config.DebugToAddresses)}");
    //            foreach (var address in _config.DebugToAddresses)
    //            {
    //                mailTo.Add(new EmailAddress(address));
    //            }
    //        }
    //        else
    //        {
    //            foreach (var address in _config.ToAddresses)
    //            {
    //                mailTo.Add(new EmailAddress(address));
    //            }
    //        }

    //        var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, mailTo, subject, null, message.Body);

    //        var response = await client.SendEmailAsync(msg);
    //        if (response != null)
    //        {
    //            return true;
    //        }
    //        return false;
    //    }

    //}
}