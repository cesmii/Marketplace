using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
using System.Net.Mail;

namespace CESMII.Marketplace.JobManager.Jobs
{

    /// <summary>
    /// Simple job to test out the job manager framework
    /// </summary>
    public class JobOnTimeEdgeTrial : JobBase
    {
        public JobOnTimeEdgeTrial(
            ILogger<IJob> logger,
            IHttpApiFactory httpFactory, 
            IDal<JobLog, JobLogModel> dalJobLog,
            UserDAL dalUser,
            IConfiguration configuration,
            MailRelayService mailRelayService) : 
            base(logger, httpFactory, dalJobLog, dalUser, configuration, mailRelayService)
        {
            //wire up run async event
            base.JobRun += JobInitiateTrial;
        }

        /// <summary>
        /// Connect to OnTimeEdge API and submit information to trigger the issuance of a license on the OnTime | Edge side. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task<string> JobInitiateTrial(object sender, JobEventArgs e)
        {
            //3 sources of data - job config - static data, runtime payload data - user entered data and user data - user profile data
            //extract out job config params from payload and convert from JSON to an object we can use within this job
            //if smip settings is null, still allow trial to go through but inform user
            var jobConfig = JsonConvert.DeserializeObject<JobOnTimeEdgeConfig>(e.Config.Data);
            var payload = JsonConvert.DeserializeObject<JobOnTimeEdgePayload>(e.Payload);
            var smipSettings = payload.SmipSettings != null ? payload.SmipSettings :
                                    e.User?.SmipSettings != null ? e.User.SmipSettings : 
                                    null;
            //validate all settings before we proceed
            var isValid = this.ValidateData(jobConfig, payload, smipSettings, out string errorMessage);
            if (!isValid)
            {
                base.CreateJobLogMessage($"Validating data...failed. {errorMessage}", TaskStatusEnum.Failed);
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateData|Failed|{errorMessage}");
                return null;
            }

            ////if update profile checked, then update user account info
            //if (payload.UpdateSmipSettings)
            //{
            //    base.CreateJobLogMessage($"Updating user profile SMIP information in Marketplace DB...", TaskStatusEnum.InProgress);
            //    var usr = _dalUser.GetById(e.User.ID);
            //    usr.SmipSettings = payload.SmipSettings;
            //    await _dalUser.Update(usr, e.User.ID);
            //}

            //get user information from request info type of form.
            base.CreateJobLogMessage($"Preparing request info user information to submit to OnTime | Edge...", TaskStatusEnum.InProgress);
            var req = MapToBody(jobConfig.SecretKey, payload.FormData);

            //save record of submission to the request info DB.
            base.CreateJobLogMessage($"TBD - Saving request info user information to Marketplace DB...", TaskStatusEnum.InProgress);
            //TBD
            
            //try to connect to the OnTime | Edge API to submit request to start trial. 
            //they use Zapier catch hook to receive the post
            base.CreateJobLogMessage($"Submitting user information to OnTime | Edge API trial workflow...", TaskStatusEnum.InProgress);
            //call the Zapier catch hook API to intialize start trial flow
            var config = new HttpApiConfig()
            {
                Url = jobConfig.Url,
                Body = JsonConvert.SerializeObject(req),
                IsPost = true,
                ContentType = "application/json",
            };

            //string responseRaw = await _httpFactory.Run(config);
            //var result = JsonConvert.DeserializeObject<OnTimeEdgeResponseModel>(responseRaw);
            var result = JsonConvert.DeserializeObject<OnTimeEdgeResponseModel>("{attempt: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',id: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',request_id: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',status: 'success'}");
            /*
            {attempt: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',id: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',request_id: '018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a',status: 'success'}
            */
            var isSuccess = !string.IsNullOrEmpty(result.status) && result.status.ToLower().Equals("success");

            string msg;
            if (isSuccess)
            {
                msg = GenerateSuccessHTML(e.Config.MarketplaceItem, payload);
                base.CreateJobLogMessage($"{msg}", TaskStatusEnum.Completed);
            }
            else
            {
                msg = GenerateFailHTML(e.Config.MarketplaceItem, payload);
                base.CreateJobLogMessage($"{msg}", TaskStatusEnum.Failed);
            }

            //notify CESMII recipients of the trial submission. 
            await GenerateNotifyEmailHTML(e.Config.MarketplaceItem, jobConfig, payload, isSuccess);

            //return success / fail to user and show thank you page on success. 
            return JsonConvert.SerializeObject(new { isSuccess = isSuccess, Message = msg });
        }

        private bool ValidateData(JobOnTimeEdgeConfig jobConfig, JobOnTimeEdgePayload payload, SmipSettings smipSettings, out string message)
        {
            var sbResult = new System.Text.StringBuilder();

            var errorMessageJobConfig = "";
            var isValidJobConfig = this.ValidateJobConfig(jobConfig, out errorMessageJobConfig);
            if (!string.IsNullOrEmpty(errorMessageJobConfig)) sbResult.AppendLine(errorMessageJobConfig);

            var errorMessagePayload = "";
            var isValidPayload = this.ValidatePayload(payload, out errorMessagePayload);
            if (!string.IsNullOrEmpty(errorMessagePayload)) sbResult.AppendLine(errorMessagePayload);

            message = sbResult.ToString();
            return isValidJobConfig && isValidPayload;
        }

        private bool ValidateJobConfig(JobOnTimeEdgeConfig jobConfig, out string message)
        {
            var sbResult = new System.Text.StringBuilder();
            if (jobConfig == null)
            {
                sbResult.AppendLine("The configuration file is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|jobConfig is missing.");
            }
            if (string.IsNullOrEmpty(jobConfig.Url)) {
                sbResult.AppendLine("The OnTime Edge url is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.Url is missing.");
            }
            if (string.IsNullOrEmpty(jobConfig.SecretKey))
            {
                sbResult.AppendLine("The OnTime Edge configuration is missing configuration data. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.SecretKey is missing.");
            }

            message = sbResult.ToString();
            return sbResult.Length == 0;
        }

        private bool ValidatePayload(JobOnTimeEdgePayload payload, out string message)
        {
            var sbResult = new System.Text.StringBuilder();
            if (payload == null || payload.FormData == null)
            {
                sbResult.AppendLine("The trial form data is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidatePayload|payload is missing.");
            }
            
            //TBD - determine if SMIP settings is being included here. 
            
            if (string.IsNullOrEmpty(payload.FormData?.FirstName))
            {
                sbResult.AppendLine("First Name is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|FirstName is required.");
            }
            if (string.IsNullOrEmpty(payload.FormData?.LastName))
            {
                sbResult.AppendLine("Last Name is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|LastName is required.");
            }
            if (string.IsNullOrEmpty(payload.FormData?.CompanyName))
            {
                sbResult.AppendLine("Organization is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|Organization is required.");
            }
            if (string.IsNullOrEmpty(payload.FormData?.Email))
            {
                sbResult.AppendLine("Email is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|Email is required.");
            }
            if (string.IsNullOrEmpty(payload.FormData?.Phone))
            {
                sbResult.AppendLine("Phone is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|Phone is required.");
            }

            message = sbResult.ToString();
            return sbResult.Length == 0;
        }

        private OnTimeEdgeRequestModel MapToBody(string secretKey, OnTimeEdgeUserModel model)
        {
            return new OnTimeEdgeRequestModel()
            {
                firstName = model.FirstName,
                lastName = model.LastName,
                organization = model.CompanyName,
                email = model.Email,
                phone = model.Phone,
                secretKey = secretKey
            };
        }

        #region Confirmation message html
        private string GenerateSuccessHTML(MarketplaceItemSimpleModel marketplaceItem, JobOnTimeEdgePayload payload)
        {
            System.Text.StringBuilder sbResult = new System.Text.StringBuilder();
            sbResult.AppendLine("<div class='row mb-2'>");
            sbResult.AppendLine("<div class='col-6 mx-auto'>");
            sbResult.AppendLine("<h1>Thank you for submitting your information.</h1>");
            sbResult.AppendLine($"<h2>Nice job {payload.FormData.FirstName}! Starting a free trial of Apogean is the first step to collecting CNC machine data.</h2>");
            sbResult.AppendLine("<p>Within one business day, we'll send you a license key to activate your free trial.  Meanwhile, you can get started by following the instructions below.</p>");
            sbResult.AppendLine("</div>");
            sbResult.AppendLine("</div>");
            sbResult.AppendLine("<div class='row mb-2'>");
            sbResult.AppendLine("<div class='col-6 mx-auto'>");
            sbResult.AppendLine("<h3 class='headline-3'>Start getting these things ready:</h3>");
            sbResult.AppendLine("<ul class='p-0 m-0'>");
            sbResult.AppendLine("<li class='m-0 p-0 my-1'>Purchase a Windows 10 or later edge device<br>(we tested the <a href='https://a.co/d/64V2XJE' rel='noopener' target='_blank'>GMKtec Mini PC Windows 11</a> and the <a href='https://a.co/d/eQDeobH' rel='noopener' target='_blank'>Mini PC GoLite 11</a>)</li>");
            sbResult.AppendLine("<li class='m-0 p-0 my-1'>Decide if you need a serial cable or ethernet cable to connect the CNC machine to the Windows device<br>(we tested this <a href='https://a.co/d/fJJIcVK' rel='noopener' target='_blank'>USB to RS232 DB25 serial adapter cable</a>)</li>");
            sbResult.AppendLine("<li class='m-0 p-0 my-1'>We'll send you the installation file and activation key within one business day</li>");
            sbResult.AppendLine("</ul>");
            sbResult.AppendLine("</div>");
            sbResult.AppendLine("</div>");
            sbResult.AppendLine("<div class='row mb-2'>");
            sbResult.AppendLine("<div class='col-6 mx-auto'>");
            sbResult.AppendLine($"<p>About <a href='{payload.HostUrl}/library/{marketplaceItem.Name}' >{marketplaceItem.DisplayName}</a></p>");
            sbResult.AppendLine("</div>");
            sbResult.AppendLine("</div>");
            return sbResult.ToString();
        }

        private string GenerateFailHTML(MarketplaceItemSimpleModel marketplaceItem, JobOnTimeEdgePayload payload)
        {
            System.Text.StringBuilder sbResult = new System.Text.StringBuilder();
            sbResult.AppendLine("<div class='row mb-2'>");
            sbResult.AppendLine("<div class='col-6 mx-auto'>");
            sbResult.AppendLine("<h1>An error occurred submitting your information.</h1>");
            sbResult.AppendLine("<p>For more information, please contact the system administrator.</p>");
            sbResult.AppendLine("</div>");
            sbResult.AppendLine("</div>");
            sbResult.AppendLine("<div class='row mb-2'>");
            sbResult.AppendLine("<div class='col-6 mx-auto'>");
            sbResult.AppendLine($"<p>About <a href='{payload.HostUrl}/library/{marketplaceItem.Name}' >{marketplaceItem.DisplayName}</a></p>");
            sbResult.AppendLine("</div>");
            sbResult.AppendLine("</div>");
            return sbResult.ToString();
        }

        private async Task GenerateNotifyEmailHTML(MarketplaceItemSimpleModel marketplaceItem, JobOnTimeEdgeConfig jobConfig, JobOnTimeEdgePayload payload, bool isSuccess)
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
            var title = isSuccess ? "Trial Submitted Successfully" : "Trial Submission Failed";
                        
            //generate email body
            System.Text.StringBuilder sbBody = new System.Text.StringBuilder();
            sbBody.AppendLine("<div class='row mb-2'>");
            sbBody.AppendLine("<div class='col-6 mx-auto'>");
            sbBody.AppendLine($"<h1>{title} | {marketplaceItem.DisplayName}</h1>");
            sbBody.AppendLine($"<hr class='my-2' />");
            sbBody.AppendLine("<ul class='p-0 m-0'>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>First Name: {payload.FormData.FirstName}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Last Name: {payload.FormData.LastName}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Company Name: {payload.FormData.CompanyName}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Email: {payload.FormData.Email}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Phone: {payload.FormData.Phone}</li>");
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
    internal class JobOnTimeEdgePayload
    {
        public string HostUrl { get; set; }
        public OnTimeEdgeUserModel FormData { get; set; }
        public SmipSettings SmipSettings { get; set; }
    }

    internal class JobOnTimeEdgeConfig
    {
        public string Url { get; set; }
        public string SecretKey { get; set; }
        public SmipSettings SmipSettings { get; set; }
        public List<string> EmailRecipients { get; set; }
    }

    /// <summary>
    /// Structure of the post argument passed to OnTime | Edge for authorization
    /// </summary>
    internal class OnTimeEdgeUserModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    /// <summary>
    /// Structure of the body we submit to Apogean
    /// </summary>
    internal class OnTimeEdgeRequestModel
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string organization { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string secretKey { get; set; }
    }

    /// <summary>
    /// Structure of the response
    /// </summary>
    internal class OnTimeEdgeResponseModel
    {
        /* sample response
        {
            "attempt": "018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a",
            "id": "018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a",
            "request_id": "018e80b4-cfe9-5ed6-fd7e-99adf0eebc7a",
            "status": "success"
        }         
         */
        public string id { get; set; }
        public string attempt { get; set; }
        public string request_id { get; set; }
        public string status { get; set; }
    }

    #endregion

}
