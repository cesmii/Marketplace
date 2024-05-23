using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using CESMII.Marketplace.Data.Repositories;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common.Models;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Enums;
using CESMII.Common.SelfServiceSignUp.Services;
using CESMII.Marketplace.SmipGraphQlClient;
using CESMII.Marketplace.SmipGraphQlClient.Models;
using CESMII.Marketplace.JobManager.Models;

namespace CESMII.Marketplace.JobManager.Jobs
{

    /// <summary>
    /// Simple job to test out the job manager framework
    /// </summary>
    public class JobOnTimeEdgeTrial : JobBase
    {
        public JobOnTimeEdgeTrial(
            IServiceScopeFactory serviceScopeFactory, 
            ILogger<IJob> logger,
            IHttpApiFactory httpFactory, 
            IDal<JobLog, JobLogModel> dalJobLog,
            UserDAL dalUser,
            IConfiguration configuration,
            MailRelayService mailRelayService) : 
            base(serviceScopeFactory, logger, httpFactory, dalJobLog, dalUser, configuration, mailRelayService)
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

            //validate all settings before we proceed
            var isValid = this.ValidateData(jobConfig, payload, out string errorMessage);
            if (!isValid)
            {
                base.CreateJobLogMessage($"Validating data...failed. {errorMessage}", TaskStatusEnum.Failed);
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateData|Failed|{errorMessage}");
                return null;
            }

            //Get/Add organization to SMIP, send org info to onTimeEdge
            /*de-scoped
            var org = new SmipOrganizationModel() { displayName = payload.FormData.CompanyName };
            if (jobConfig.SmipSettings.Enabled)
            {
                base.CreateJobLogMessage($"Generating SMIP organization information in SMIP instance...", TaskStatusEnum.InProgress);
                org = await GenerateSmipData(jobConfig.SmipSettings, org);
            }
            */

            //get user information from request info type of form.
            base.CreateJobLogMessage($"Preparing user information to submit to OnTime | Edge...", TaskStatusEnum.InProgress);
            var req = MapToBody(jobConfig.ApogeanApi.SecretKey, payload.FormData, payload.FormData.Organization.Name);

            //save record of submission to the request info DB.
            //base.CreateJobLogMessage($"TBD - Saving request info user information to Marketplace DB...", TaskStatusEnum.InProgress);
            //TBD

            //try to connect to the OnTime | Edge API to submit request to start trial. 
            bool isSuccess = true;
            if (jobConfig.ApogeanApi.Enabled)
            {
                //they use Zapier catch hook to receive the post
                base.CreateJobLogMessage($"Submitting user information to OnTime | Edge API trial workflow...", TaskStatusEnum.InProgress);
                //call the Zapier catch hook API to intialize start trial flow
                var config = new HttpApiConfig()
                {
                    BaseAddress = "",
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
                base.CreateJobLogMessage($"Warning. Apogean API configuration is disabled. Skipping submit user information to OnTime | Edge API trial workflow...", TaskStatusEnum.InProgress);
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

            //notify CESMII recipients of the trial submission. 
            await GenerateNotifyEmailHTML(e.Config.MarketplaceItem, jobConfig, payload, isSuccess);

            //return success / fail to user and show thank you page on success. 
            return JsonConvert.SerializeObject(isSuccess);
        }

        [System.Obsolete("GenerateSmipData de-scoped")]
        private async Task<SmipOrganizationModel> GenerateSmipData(SmipAuthenticatorSettings settings, SmipOrganizationModel org)
        {
            var dalSmipOrg = new SmipOrganizationDAL(settings, _logger);
            await dalSmipOrg.Authenticate();

            var matches = await dalSmipOrg.SearchAsync(org.displayName);
            if (matches == null || matches.Count == 0) {
                base.CreateJobLogMessage($"Adding a new organization into the SMIP instance. ", TaskStatusEnum.InProgress);
                _logger.LogWarning($"JobOnTimeEdgeTrial|GenerateSmipData|{settings.GraphQlUrl}|Adding a new organization into the SMIP instance. Name {org.displayName}");
                //return await dalSmipOrg.AddAsync(new SmipOrganizationModel() { relativeName });
                return null;
            }
            else if (matches.Count > 1)
            {
                base.CreateJobLogMessage($"An error occurred generating organization data in the SMIP instance. There are multiple organizations with name {org.displayName}. Please contact the system administrator.", TaskStatusEnum.Failed);
                _logger.LogError($"JobOnTimeEdgeTrial|GenerateSmipData|Failed|{settings.GraphQlUrl}|There are multiple organizations with name {org.displayName}");
                return org;
            }
            else if (matches.Count == 1)
            {
                return matches[0];
            }
            else
            {
                return org;
                //return await dalSmipOrg.AddAsync(new SmipOrganizationModel() { relativeName });
            }
        }

        private bool ValidateData(JobOnTimeEdgeConfig jobConfig, JobOnTimeEdgePayload payload, out string message)
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
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|jobConfig is missing.");
            }
            if (jobConfig.ApogeanApi == null)
            {
                sbResult.AppendLine("The ApogeanApi config section is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|ApogeanApi section is missing.");
            }
            if (string.IsNullOrEmpty(jobConfig.ApogeanApi.Url)) {
                sbResult.AppendLine("The OnTime Edge url is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.Url is missing.");
            }
            if (string.IsNullOrEmpty(jobConfig.ApogeanApi.SecretKey))
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
            if (string.IsNullOrEmpty(payload.FormData?.Organization?.Name))
            {
                sbResult.AppendLine("Organization is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|Organization is required.");
            }
            if (string.IsNullOrEmpty(payload.FormData?.Email))
            {
                sbResult.AppendLine("Email is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|Email is required.");
            }
            else
            {
                var msgEmailDomain = "";
                if (!ValidateEmailDomain(payload.FormData?.Email, out msgEmailDomain))
                {
                    sbResult.AppendLine(msgEmailDomain);
                }
            }
            if (string.IsNullOrEmpty(payload.FormData?.Phone))
            {
                sbResult.AppendLine("Phone is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|Phone is required.");
            }

            message = sbResult.ToString();
            return sbResult.Length == 0;
        }

        private bool ValidateEmailDomain(string email, out string message)
        {
            var sbResult = new System.Text.StringBuilder();
            if (string.IsNullOrEmpty(email))
            {
                sbResult.AppendLine("Email is required.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateEmailDomain|Email is required.");
            }
            else
            {
                //wrap in scope so that we don't lose the scope of the dependency injected objects once the 
                //web api request completes and disposes of the import service object (and its module vars)
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    //initialize scoped services for DI, initialize job class
                    var repoBlockedDomains = scope.ServiceProvider.GetService<IMongoRepository<BlockedEmailDomain>>();
                    var numMatches = repoBlockedDomains.Count(x => email.Trim().ToLower().EndsWith(x.domain.ToLower()));

                    if (numMatches > 0)
                    {
                        //var matches = repoBlockedDomains.FindByCondition(x => email.Trim().ToLower().EndsWith(x.domain.ToLower())).Select(x => x.domain).ToList();
                        sbResult.AppendLine("Signing up for this trial prohibits use of free email domains. Please use a different email.");
                        _logger.LogWarning($"JobOnTimeEdgeTrial|ValidateEmailDomain|Blocked|Email '{email}' has a blocked email domain and is not permitted.");
                    }
                }
            }

            message = sbResult.ToString();
            return sbResult.Length == 0;
        }


        [System.Obsolete("ValidateSmipSettings de-scoped")]
        private bool ValidateSmipSettings(SmipSettingsOnTimeEdge settings, out string message)
        {
            var sbResult = new System.Text.StringBuilder();
            if (settings == null)
            {
                sbResult.AppendLine("The SMIP Settings are missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|jobConfig is missing the SMIP Settings.");
            }
            if (string.IsNullOrEmpty(settings.GraphQlUrl))
            {
                sbResult.AppendLine("The SMIP Settings GraphQL Url is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.SmipSettings.GraphQlUrl is missing.");
            }
            if (string.IsNullOrEmpty(settings.UserName))
            {
                sbResult.AppendLine("The SMIP Settings user name is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.SmipSettings.UserName is missing.");
            }
            if (string.IsNullOrEmpty(settings.Authenticator))
            {
                sbResult.AppendLine("The SMIP Settings authenticator is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.SmipSettings.Authenticator is missing.");
            }
            if (string.IsNullOrEmpty(settings.AuthenticatorRole))
            {
                sbResult.AppendLine("The SMIP Settings AuthenticatorRole is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.SmipSettings.AuthenticatorRole is missing.");
            }

            message = sbResult.ToString();
            return sbResult.Length == 0;
        }

        private OnTimeEdgeRequestModel MapToBody(string secretKey, UserCheckoutModel model, string organizationName)
        {
            return new OnTimeEdgeRequestModel()
            {
                firstName = model.FirstName,
                lastName = model.LastName,
                //TBD - work w/ Apogean to get this part changed. 
                //organization = org,
                organization = organizationName,
                email = model.Email,
                phone = model.Phone,
                secretKey = secretKey
            };
        }

        #region Confirmation message html
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
            sbBody.AppendLine("<ul class='p-0 m-0 pl-3'>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>First Name: {payload.FormData.FirstName}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Last Name: {payload.FormData.LastName}</li>");
            sbBody.AppendLine($"<li class='m-0 p-0 my-1'>Company Name: {payload.FormData.Organization?.Name}</li>");
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
        public UserCheckoutModel FormData { get; set; }
    }

    internal class JobOnTimeEdgeConfig
    {
        public OnTimeEdgeApiConfig ApogeanApi { get; set; }
        public SmipSettingsOnTimeEdge SmipSettings { get; set; }
        public List<string> EmailRecipients { get; set; }
    }

    internal class OnTimeEdgeApiConfig
    {
        public bool Enabled { get; set; }
        public string Url { get; set; }
        public string SecretKey { get; set; }
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
    /// Structure of the response from Apogean
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

    internal class SmipSettingsOnTimeEdge : SmipAuthenticatorSettings
    {
        /// <summary>
        /// Allow job to skip over SMIP step if needed
        /// </summary>
        public bool Enabled { get; set; }

    }


    #endregion

}
