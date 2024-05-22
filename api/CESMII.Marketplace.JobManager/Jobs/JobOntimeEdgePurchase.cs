using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common.Models;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Enums;
using CESMII.Common.SelfServiceSignUp.Services;

namespace CESMII.Marketplace.JobManager.Jobs
{

    /// <summary>
    /// Simple job to test out the job manager framework
    /// </summary>
    public class JobOntimeEdgePurchase : JobBase
    {
        public JobOntimeEdgePurchase(
            ILogger<IJob> logger,
            IHttpApiFactory httpFactory, 
            IDal<JobLog, JobLogModel> dalJobLog,
            UserDAL dalUser,
            IConfiguration configuration,
            MailRelayService mailRelayService) : 
            base(logger, httpFactory, dalJobLog, dalUser, configuration, mailRelayService)
        {
            //wire up run async event
            base.JobRun += JobInitiateLicensing;
        }

        /// <summary>
        /// Connect to OnTimeEdge API and submit information to trigger the issuance of a license on the OnTime | Edge side. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task<string> JobInitiateLicensing(object sender, JobEventArgs e)
        {
            //3 sources of data - job config - static data, runtime payload data - user entered data and user data - user profile data
            //extract out job config params from payload and convert from JSON to an object we can use within this job
            //if smip settings is null, still allow purchase to go through but inform user
            var jobConfig = JsonConvert.DeserializeObject<JobOnTimeEdgeConfig>(e.Config.Data);
            var payload = JsonConvert.DeserializeObject<ECommerceOnCompleteModel>(e.Payload);

            //validate all settings before we proceed
            var isValid = this.ValidateData(jobConfig, payload, out string errorMessage);
            if (!isValid)
            {
                base.CreateJobLogMessage($"Validating data...failed. {errorMessage}", TaskStatusEnum.Failed);
                _logger.LogError($"JobOntimeEdgePurchase|ValidateData|Failed|{errorMessage}");
                return null;
            }

            //get user information from request info type of form.
            base.CreateJobLogMessage($"Preparing user information to submit to OnTime | Edge...", TaskStatusEnum.InProgress);
            var req = MapToBody(jobConfig.ApogeanApi.SecretKey, payload.CheckoutUser, payload.CheckoutUser.Organization.Name);

            //save record of submission to the request info DB.
            //base.CreateJobLogMessage($"TBD - Saving request info user information to Marketplace DB...", TaskStatusEnum.InProgress);
            //TBD

            //try to connect to the OnTime | Edge API to submit request to start purchase. 
            bool isSuccess = true;
            if (jobConfig.ApogeanApi.Enabled)
            {
                //they use Zapier catch hook to receive the post
                base.CreateJobLogMessage($"Submitting user information to OnTime | Edge API purchase workflow...", TaskStatusEnum.InProgress);
                //call the Zapier catch hook API to intialize start purchase flow
                var config = new HttpApiConfig()
                {
                    Url = jobConfig.ApogeanApi.Url,
                    Body = new StringContent(JsonConvert.SerializeObject(req)),
                    ContentType = "application/json",
                };

                string responseRaw = await _httpFactory.Run(config);
                var result = JsonConvert.DeserializeObject<OnTimeEdgeResponseModel>(responseRaw);
                //var result = JsonConvert.DeserializeObject<OnTimeEdgeResponseModel>("{attempt: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',id: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',request_id: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',status: 'success'}");
                /*
                {attempt: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',id: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',request_id: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',status: 'success'}
                */
                isSuccess = !string.IsNullOrEmpty(result.status) && result.status.ToLower().Equals("success");
            }
            else
            {
                base.CreateJobLogMessage($"Warning. Apogean API configuration is disabled. Skipping submit user information to OnTime | Edge API purchase workflow...", TaskStatusEnum.InProgress);
            }

            //generate output
            if (isSuccess)
            {
                //front end will display a message
                base.CreateJobLogMessage($"Success! Your information has been submitted.", TaskStatusEnum.Completed);
            }
            else
            {
                base.CreateJobLogMessage($"An error occurred submitting your information.", TaskStatusEnum.Failed);
            }

            //notify CESMII recipients of the purchase submission. 
            await GenerateNotifyEmailHTML(e.Config.MarketplaceItem, jobConfig, payload, isSuccess);

            //return success / fail to user and show thank you page on success. 
            return JsonConvert.SerializeObject(isSuccess);
        }


        private bool ValidateData(JobOnTimeEdgeConfig jobConfig, ECommerceOnCompleteModel payload, out string message)
        {
            var sbResult = new System.Text.StringBuilder();

            var errorMessageJobConfig = "";
            var isValidJobConfig = this.ValidateJobConfig(jobConfig, out errorMessageJobConfig);
            if (!string.IsNullOrEmpty(errorMessageJobConfig)) sbResult.AppendLine(errorMessageJobConfig);

            var errorMessagePayload = "";
            var isValidPayload = this.ValidatePayload(payload, out errorMessagePayload);
            if (!string.IsNullOrEmpty(errorMessagePayload)) sbResult.AppendLine(errorMessagePayload);

            /*de-scoped
            var errorMessageSmip = "";
            var isValidSmip = this.ValidateSmipSettings(jobConfig.SmipSettings, out errorMessageSmip);
            if (!string.IsNullOrEmpty(errorMessageSmip)) sbResult.AppendLine(errorMessagePayload);
            */

            message = sbResult.ToString();
            return isValidJobConfig && isValidPayload; //&& isValidSmip;
        }

        private bool ValidateJobConfig(JobOnTimeEdgeConfig jobConfig, out string message)
        {
            var sbResult = new System.Text.StringBuilder();
            if (jobConfig == null)
            {
                sbResult.AppendLine("The configuration file is missing. Please contact the system administrator.");
                _logger.LogError($"JobOntimeEdgePurchase|ValidateJobConfig|jobConfig is missing.");
            }
            if (jobConfig.ApogeanApi == null)
            {
                sbResult.AppendLine("The ApogeanApi config section is missing. Please contact the system administrator.");
                _logger.LogError($"JobOntimeEdgePurchase|ValidateJobConfig|ApogeanApi section is missing.");
            }
            if (string.IsNullOrEmpty(jobConfig.ApogeanApi.Url)) {
                sbResult.AppendLine("The OnTime Edge url is missing. Please contact the system administrator.");
                _logger.LogError($"JobOntimeEdgePurchase|ValidateJobConfig|The jobConfig.Url is missing.");
            }
            if (string.IsNullOrEmpty(jobConfig.ApogeanApi.SecretKey))
            {
                sbResult.AppendLine("The OnTime Edge configuration is missing configuration data. Please contact the system administrator.");
                _logger.LogError($"JobOntimeEdgePurchase|ValidateJobConfig|The jobConfig.SecretKey is missing.");
            }

            message = sbResult.ToString();
            return sbResult.Length == 0;
        }

        private bool ValidatePayload(ECommerceOnCompleteModel payload, out string message)
        {
            var sbResult = new System.Text.StringBuilder();
            if (payload == null || payload.CheckoutUser == null)
            {
                sbResult.AppendLine("The purchaser information is missing. Please contact the system administrator.");
                _logger.LogError($"JobOntimeEdgePurchase|ValidatePayload|payload is missing.");
            }
            
            if (string.IsNullOrEmpty(payload.CheckoutUser?.FirstName))
            {
                sbResult.AppendLine("First Name is required.");
                _logger.LogInformation($"JobOntimeEdgePurchase|ValidatePayload|FirstName is required.");
            }
            if (string.IsNullOrEmpty(payload.CheckoutUser?.LastName))
            {
                sbResult.AppendLine("Last Name is required.");
                _logger.LogInformation($"JobOntimeEdgePurchase|ValidatePayload|LastName is required.");
            }
            if (string.IsNullOrEmpty(payload.CheckoutUser?.Organization?.Name))
            {
                sbResult.AppendLine("Organization is required.");
                _logger.LogInformation($"JobOntimeEdgePurchase|ValidatePayload|Organization is required.");
            }
            if (string.IsNullOrEmpty(payload.CheckoutUser?.Email))
            {
                sbResult.AppendLine("Email is required.");
                _logger.LogInformation($"JobOntimeEdgePurchase|ValidatePayload|Email is required.");
            }
            //if (string.IsNullOrEmpty(payload.GuestUser?.Phone))
            //{
            //    sbResult.AppendLine("Phone is required.");
            //    _logger.LogInformation($"JobOntimeEdgePurchase|ValidatePayload|Phone is required.");
            //}

            message = sbResult.ToString();
            return sbResult.Length == 0;
        }


        private OnTimeEdgeRequestModel MapToBody(string secretKey, UserCheckoutModel model, string organizationName)
        {
            return new OnTimeEdgeRequestModel()
            {
                firstName = model.FirstName,
                lastName = model.LastName,
                organization = organizationName,
                email = model.Email,
                //phone = model.Phone,
                secretKey = secretKey
            };
        }

        #region Confirmation message html
        private async Task GenerateNotifyEmailHTML(MarketplaceItemSimpleModel marketplaceItem, JobOnTimeEdgeConfig jobConfig, ECommerceOnCompleteModel payload, bool isSuccess)
        {
            if (jobConfig.EmailRecipients == null || !jobConfig.EmailRecipients.Any()) return;

            //build to email list
            /*
            var toEmails = new MailAddressCollection();
            foreach (var e in jobConfig.EmailRecipients)
            {
                toEmails.Add(new MailAddress(e));
            }
            */
            var title = isSuccess ? "Purchase Submitted Successfully" : "Purchase Submission Failed";
                        
            //generate email body
            System.Text.StringBuilder sbBody = new System.Text.StringBuilder();
            sbBody.AppendLine("<div class='row mb-2'>");
            sbBody.AppendLine("<div class='col-6 mx-auto'>");
            sbBody.AppendLine($"<h1>{title} | {marketplaceItem.DisplayName}</h1>");
            sbBody.AppendLine($"<hr class='my-2' />");
            sbBody.AppendLine("<ul class='p-0 m-0 pl-3'>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>First Name: {payload.CheckoutUser.FirstName}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Last Name: {payload.CheckoutUser.LastName}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Company Name: {payload.CheckoutUser.Organization.Name}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Email: {payload.CheckoutUser.Email}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Phone: {payload.CheckoutUser.Phone}</li>");
            sbBody.AppendLine("</ul>");
            sbBody.AppendLine("</div>");
            sbBody.AppendLine("</div>");

            var toEmails = string.Join(",", jobConfig.EmailRecipients.ToArray());
            var message = new MailMessage(_configUtil.MailSettings.MailFromAddress, toEmails)
            {
                Subject = $"CESMII | SM Marketplace | OnTime Edge | {title}",
                Body = sbBody.ToString(),
                IsBodyHtml = true
            };

            await _mailRelayService.SendEmail(message);

        }

        #endregion

    }


    #region Models associated with this particular job
    //most of the models in the JobOntimeEdgePurchase.cs
    internal class ECommerceOnCompleteModel
    {
        public CartItemModel CartItem { get; set; }
        /// <summary>
        /// Only populated if user is purchasing anonymously as a one time guest
        /// </summary>
        public UserCheckoutModel CheckoutUser { get; set; }
    }

    #endregion

}
