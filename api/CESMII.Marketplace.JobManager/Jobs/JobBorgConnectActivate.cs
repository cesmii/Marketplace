using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common.Models;
using CESMII.Marketplace.Common;
using System.Threading.Tasks;
using CESMII.Marketplace.Common.Enums;

namespace CESMII.Marketplace.JobManager.Jobs
{

    /// <summary>
    /// Simple job to test out the job manager framework
    /// </summary>
    public class JobBorgConnectActivate : JobBase
    {
        public JobBorgConnectActivate(
            ILogger<JobBorgConnectActivate> logger,
            IHttpApiFactory httpFactory, IDal<JobLog, JobLogModel> dalJobLog): 
            base(logger, httpFactory, dalJobLog)
        {
            //wire up run async event
            base.JobRun += JobRunBorg;
        }

        private async Task<string> JobRunBorg(object sender, JobEventArgs e)
        {
            //extract out job config params from payload and convert from JSON to an object we can use within this job
            var configData = JsonConvert.DeserializeObject<JobBorgConnectActivateConfig>(e.Config.Data);

            base.CreateJobLogMessage($"Authorizing user with Borg Connect API...", TaskStatusEnum.InProgress);
            var token = await GetBearerToken(configData);

            //make a second call to the API to initiate the instance, pass token as auth header
            base.CreateJobLogMessage($"Creating customer with Borg Connect API...", TaskStatusEnum.InProgress);
            var result = await CreateCustomer(configData, e.User, token);

            //serialize & encrypt result data in a response data field in the JobLog table. This will be decrypted 
            //and displayed on the front end.
            string response = JsonConvert.SerializeObject(result);
            base.SetJobLogResponse(response, $"Customer created. Retrieving connection information...", TaskStatusEnum.Completed);

            return JsonConvert.SerializeObject(result);
        }

        private async Task<string> GetBearerToken(JobBorgConnectActivateConfig configData)
        {
            return "[auth token]";

            //call the 5G API to get the initial access token
            var config = new HttpApiConfig()
            {
                Url = configData.AuthorizeConfig.Url,
                Body = JsonConvert.SerializeObject(configData.AuthorizeConfig.Body)
            };

            string response = await _httpFactory.Run(config);
            var result = JsonConvert.DeserializeObject<BorgAuthorizeResponse>(response);
            if (result.LoginResult == null || string.IsNullOrEmpty(result.LoginResult.Result) ||
                !result.LoginResult.Result.ToLower().Equals("success"))
            {
                var msg = "Unable to authorize user against Borg Connect API.";
                _logger.LogError($"JobBorgConnectActivate|GetBearerToken|{msg}");
                base.CreateJobLogMessage(msg, TaskStatusEnum.Failed);
                throw new UnauthorizedAccessException(msg);
            }

            return result.LoginResult.Message.authToken;
        }

        private async Task<BorgCreateResponseMessage> CreateCustomer(JobBorgConnectActivateConfig configData, 
            UserModel user, string token)
        {
            return new BorgCreateResponseMessage()
            {
                Password = "pw-TEST",
                URL = "https://www.google.com",
                Username = "un-TEST"
            };

            //extract data from user profile and place in the formData needed by the Borg API
            MapUserToFormDataModel(ref configData, user);

            //call the 5G API to get the initial access token
            var config = new HttpApiConfig()
            {
                Url = configData.CreateCustomerConfig.Url,
                Body = JsonConvert.SerializeObject(configData.CreateCustomerConfig.Body),
                BearerToken = token
            };

            string response = await _httpFactory.Run(config);
            var result = JsonConvert.DeserializeObject<BorgCreateResponse>(response);
            if (result.CreateCustomerResult == null || string.IsNullOrEmpty(result.CreateCustomerResult.Result) ||
                !result.CreateCustomerResult.Result.ToLower().Equals("success"))
            {
                var msg = "Unable to create customer against Borg Connect API.";
                _logger.LogError($"JobBorgConnectActivate|CreateCustomer|{msg}");
                base.CreateJobLogMessage(msg, TaskStatusEnum.Failed);
                throw new UnauthorizedAccessException(msg);
            }

            //TBD - figure out how to pass back this sensitive info and not create a security hole. 
            return result.CreateCustomerResult.Message;
        }

        private static void MapUserToFormDataModel(ref JobBorgConnectActivateConfig configData, UserModel user)
        {
            //transfer values from user record to formData collection
            configData.CreateCustomerConfig.Body.formData.FirstOrDefault(x => x.Name.Equals("CustomerName")).Value =
                user.FullName;
            configData.CreateCustomerConfig.Body.formData.FirstOrDefault(x => x.Name.Equals("ContactEmail")).Value =
                user.Email;
            configData.CreateCustomerConfig.Body.formData.FirstOrDefault(x => x.Name.Equals("CESMII_GraphQL_URL")).Value =
                user.SmipSettings.GraphQlUrl;
            configData.CreateCustomerConfig.Body.formData.FirstOrDefault(x => x.Name.Equals("CESMII_UserName")).Value =
                user.SmipSettings.UserName;
            configData.CreateCustomerConfig.Body.formData.FirstOrDefault(x => x.Name.Equals("CESMII_Password")).Value =
                user.SmipSettings.Password;
            configData.CreateCustomerConfig.Body.formData.FirstOrDefault(x => x.Name.Equals("CESMII_Authenticator")).Value =
                user.SmipSettings.Authenticator;
            configData.CreateCustomerConfig.Body.formData.FirstOrDefault(x => x.Name.Equals("CESMII_Authenticator_role")).Value =
                user.SmipSettings.AuthenticatorRole;
        }
    }


    #region Models associated with this particular job
    internal class JobBorgConnectActivateConfig
    {
        public BorgAuthorizeConfig AuthorizeConfig { get; set; }
        public BorgCreateConfig CreateCustomerConfig { get; set; }
    }

    internal class BorgAuthorizeConfig
    {
        public string Url { get; set; }
        public BorgAuthorizeBody Body { get; set; }
    }

    internal class BorgCreateConfig
    {
        public string Url { get; set; }
        public BorgCreateBody Body { get; set; }
    }

    /// <summary>
    /// Structure of the post argument passed to BorgConnect for authorization
    /// </summary>
    internal class BorgAuthorizeBody
    {
        public string userName { get; set; }
        public string password { get; set; }
        public string admin { get; set; }
    }

    /// <summary>
    /// Structure of the authorize response
    /// </summary>
    internal class BorgAuthorizeResponse
    {
        public BorgAuthorizeResponseDetail LoginResult { get; set; }
    }

    /// <summary>
    /// Structure of the authorize response detail
    /// </summary>
    internal class BorgAuthorizeResponseDetail
    {
        public string Result { get; set; }
        public BorgAuthorizeResponseMessage Message { get; set; }

    }

    /// <summary>
    /// Structure of the authorize response message details
    /// </summary>
    internal class BorgAuthorizeResponseMessage
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string FirstName { get; set; }
        public string UserType { get; set; }
        public string authToken { get; set; }

        //Note - additional data returned in response is not being used right now...

    }

    /// <summary>
    /// Structure of the post argument passed to BorgConnect for creating customer
    /// </summary>
    /// <remarks>
    /// formData includes customer name, contact email, CESMII graph QL url, CESMII SMIP user name, password, authenticator, role
    /// </remarks>
    internal class BorgCreateBody
    {
        public List<CreateCustomerBodyData> formData { get; set; }
    }

    internal class CreateCustomerBodyData
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Structure of the create response
    /// </summary>
    internal class BorgCreateResponse
    {
        public BorgCreateResponseDetail CreateCustomerResult { get; set; }
    }

    /// <summary>
    /// Structure of the create customer response
    /// </summary>
    internal class BorgCreateResponseDetail
    {
        public string Result { get; set; }
        public BorgCreateResponseMessage Message { get; set; }

    }

    /// <summary>
    /// Structure of the create customer response message details
    /// </summary>
    internal class BorgCreateResponseMessage
    {
        public string URL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    }

    #endregion

}
