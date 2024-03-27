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
            var jobConfigData = JsonConvert.DeserializeObject<JobOnTimeEdgeConfig>(e.Config.Data);
            var payload = JsonConvert.DeserializeObject<JobOnTimeEdgePayload>(e.Payload);
            var smipSettings = payload.SmipSettings != null ? payload.SmipSettings :
                                    e.User?.SmipSettings != null ? e.User.SmipSettings : 
                                    null;
            //validate all settings before we proceed
            var isValid = this.ValidateData(jobConfigData, payload, smipSettings, out string errorMessage);
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
            var req = MapToBody(jobConfigData.OnTimeEdgeSettings.SecretKey, payload.TrialFormData);

            //save record of submission to the request info DB.
            base.CreateJobLogMessage($"TBD - Saving request info user information to Marketplace DB...", TaskStatusEnum.InProgress);
            //TBD
            
            //try to connect to the OnTime | Edge API to submit request to start trial. 
            //they use Zapier catch hook to receive the post
            base.CreateJobLogMessage($"Submitting user information to OnTime | Edge API trial workflow...", TaskStatusEnum.InProgress);
            //call the Zapier catch hook API to intialize start trial flow
            var config = new HttpApiConfig()
            {
                Url = jobConfigData.OnTimeEdgeSettings.Url,
                Body = JsonConvert.SerializeObject(req),
                IsPost = true,
                ContentType = "application/json",
            };

            string responseRaw = await _httpFactory.Run(config);
            var result = JsonConvert.DeserializeObject<OnTimeEdgeResponseModel>(responseRaw);

            if (!string.IsNullOrEmpty(result.status) && result.status.ToLower().Equals("success"))
            {
                base.CreateJobLogMessage($"Form submitted to the OnTime | Edge API workflow successfully...", TaskStatusEnum.Completed);
            }
            else
            {
                base.CreateJobLogMessage($"An error occurred submitting the trial form to OnTime | Edge...", TaskStatusEnum.Failed);
            }

            //TBD - notify CESMII recipients of the trial. 
            await EmailSubmissionData(jobConfigData, payload);

            //TBD - below here
            //create an organization in the SMIP if OnTime | Edge API indicates successful result. 

            //return success / fail to user and show thank you page on success. 

/* TBD
            //put response into a human readable format that can be displayed as a message in the front end. 
            //TBD - encrypt result data in the response data field in the JobLog table. This will be decrypted 
            //by DAL and displayed on the front end.
            string response = $"Url: <a href='{result.URL}' target='_blank' >{result.URL}</a>, User name: {result.Username}. An email has been sent with your temporary password.";
            base.CreateJobLogMessage($"Customer created. Connection information:: {"<br />"}{response}", TaskStatusEnum.Completed, false);
*/
            return JsonConvert.SerializeObject(result);

        }

        /// <summary>
        /// Send two emails with account info. a password to the owner of this job. 
        /// </summary>
        /// <remarks>do not fail the process if email sending fails. </remarks>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task EmailSubmissionData(OnTimeEdgeConfig jobConfig, JobOnTimeEdgePayload payload)
        {
            try
            {
                string body = $"<p>Thank you for your interest in OnTime | Edge and the Smart Manufacturing Innovation Platform. Your OnTime | Edge activation has completed and your instance information is included below. " +
                    "For security reasons, your password will be delivered in a separate email. </p>" +
                    $"<p><b>Connection Details:</b><br />" +
                    $"<b>Url</b>: <a href='{result.URL}' target='_blank' >{result.URL}</a><br />" +
                    $"<b>Username</b>: {result.Username}<br />" +
                    "</p>";
                await base.SendEmail("CESMII | SM Marketplace | OnTime | Edge | Trial Form Submitted", body);

                string bodyPw = $"<p>Thank you for your interest in OnTime | Edge and the Smart Manufacturing Innovation Platform. Your OnTime | Edge activation has completed and your password information is included below. " +
                    "For security reasons, your instance details will be delivered in a separate email. </p>" +
                    $"<p></p>" +
                    $"<b>Password</b>: {result.Password}</p>";
                await base.SendEmail("CESMII | SM Marketplace | OnTime | Edge | Activation - Connection Information", bodyPw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"JobOnTimeEdgeTrial|Email Error");
            }
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
            return sbResult.Length == 0;
        }

        private bool ValidateJobConfig(JobOnTimeEdgeConfig jobConfig, out string message)
        {
            var sbResult = new System.Text.StringBuilder();
            if (jobConfig == null)
            {
                sbResult.AppendLine("The configuration file is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|jobConfig is missing.");
            }
            if (jobConfig.OnTimeEdgeSettings == null)
            {
                sbResult.AppendLine("The OnTime Edge configuration is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.OnTimeEdgeSettings is missing.");
            }
            if (string.IsNullOrEmpty(jobConfig.OnTimeEdgeSettings?.Url)) {
                sbResult.AppendLine("The OnTime Edge url is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.OnTimeEdgeSettings.Url is missing.");
            }
            if (string.IsNullOrEmpty(jobConfig.OnTimeEdgeSettings?.SecretKey))
            {
                sbResult.AppendLine("The OnTime Edge configuration is missing configuration data. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidateJobConfig|The jobConfig.OnTimeEdgeSettings.SecretKey is missing.");
            }

            message = sbResult.ToString();
            return sbResult.Length == 0;
        }

        private bool ValidatePayload(JobOnTimeEdgePayload payload, out string message)
        {
            var sbResult = new System.Text.StringBuilder();
            if (payload == null || payload.TrialFormData == null)
            {
                sbResult.AppendLine("The trial form data is missing. Please contact the system administrator.");
                _logger.LogError($"JobOnTimeEdgeTrial|ValidatePayload|payload is missing.");
            }
            
            //TBD - determine if SMIP settings is being included here. 
            
            if (string.IsNullOrEmpty(payload.TrialFormData?.FirstName))
            {
                sbResult.AppendLine("First Name is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|FirstName is required.");
            }
            if (string.IsNullOrEmpty(payload.TrialFormData?.LastName))
            {
                sbResult.AppendLine("Last Name is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|LastName is required.");
            }
            if (string.IsNullOrEmpty(payload.TrialFormData?.Organization))
            {
                sbResult.AppendLine("Organization is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|Organization is required.");
            }
            if (string.IsNullOrEmpty(payload.TrialFormData?.Email))
            {
                sbResult.AppendLine("Email is required.");
                _logger.LogInformation($"JobOnTimeEdgeTrial|ValidatePayload|Email is required.");
            }
            if (string.IsNullOrEmpty(payload.TrialFormData?.Phone))
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
                organization = model.Organization,
                email = model.Email,
                phone = model.Phone,
                secretKey = secretKey
            };
        }

    }


    #region Models associated with this particular job
    internal class JobOnTimeEdgePayload
    {
        public OnTimeEdgeUserModel TrialFormData { get; set; }
        public SmipSettings SmipSettings { get; set; }
        /// <summary>
        /// If true, update user profile with SMIP settings and User Profile Settings.
        /// </summary>
        public bool UpdateSmipSettings { get; set; }
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
        public string Organization { get; set; }
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
