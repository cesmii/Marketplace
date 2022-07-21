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
            ILogger<IJob> logger,
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
            var authData = await GetAuthToken(configData);

            //make a second call to the API to initiate the instance, pass token as auth header
            base.CreateJobLogMessage($"Creating customer with Borg Connect API...", TaskStatusEnum.InProgress);
            var result = await CreateCustomer(configData, e.User, authData.authToken);

            //put response into a human readable format that can be displayed as a message in the front end. 
            //TBD - encrypt result data in the response data field in the JobLog table. This will be decrypted 
            //by DAL and displayed on the front end.
            string response = $"Url: <a href='{result.URL}' target='_blank' >{result.URL}</a>, User name: {result.Username}, Password: {result.Password}";
            base.CreateJobLogMessage($"Customer created. Connection information:: {"<br />"}{response}", TaskStatusEnum.Completed, false);

            //TBD - append the connection information to the user account. Add a collection of myItems with data associated with said 
            //items. 

            return JsonConvert.SerializeObject(result);
        }

        private async Task<BorgAuthorizeResponseMessage> GetAuthToken(JobBorgConnectActivateConfig configData)
        {
            //call the 5G API to get the initial access token
            var config = new HttpApiConfig()
            {
                Url = configData.AuthorizeConfig.Url,
                Body = JsonConvert.SerializeObject(configData.AuthorizeConfig.Body)
            };

            string responseRaw = await _httpFactory.Run(config);
            var result = JsonConvert.DeserializeObject<BorgAuthorizeResponse>(responseRaw);

            #region Extra parsing
            /*
            //because returned format is heavily escaped, we jump through some hoops to parse response.
            var responseUnescaped = responseRaw.Replace(@"\", "");
            //var responseUnescaped = System.Text.RegularExpressions.Regex.Unescape(responseRaw);
            //remove quote around loginResult value, message value
            responseUnescaped = responseUnescaped.Replace("\"{\"M", "{\"M"); //leading quote
            responseUnescaped = responseUnescaped.Substring(0, responseUnescaped.Length-4) + "\"}}"; //trailing quote
            //now work on the message value to unescape it so it can be deserialized.
            //this is also a non-conventional structure. A message object that comes back as an array of messages.
            responseUnescaped = responseUnescaped.Replace("\"[{", "[{"); //leading quote
            responseUnescaped = responseUnescaped.Replace("}]\"", "}]"); //trailing quote
            var result = JsonConvert.DeserializeObject<BorgAuthorizeResponse>(responseUnescaped);
            */
            #endregion

            if (result.LoginResult == null || string.IsNullOrEmpty(result.LoginResult.Result) ||
                !result.LoginResult.Result.ToLower().Equals("success"))
            {
                var msg = $"Unable to authorize user against Borg Connect API.";
                base.CreateJobLogMessage(msg, TaskStatusEnum.Failed);
                _logger.LogError($"JobBorgConnectActivate|GetBearerToken|{msg}");
                throw new UnauthorizedAccessException(msg);
            }

            //on success, the message value is a different structure, convert it to the expected structure.
            List<BorgAuthorizeResponseMessage> msgResult = new List<BorgAuthorizeResponseMessage>();
            foreach (var item in result.LoginResult.Message) //this is a JArray and cannot convert directly to list.
            {
                msgResult.Add(JsonConvert.DeserializeObject<BorgAuthorizeResponseMessage>(JsonConvert.SerializeObject(item)));
            }

            System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(msgResult[0]));
            return msgResult[0];
        }

        private async Task<BorgCreateResponseMessage> CreateCustomer(JobBorgConnectActivateConfig configData, 
            UserModel user, string token)
        {
            //extract data from user profile and place in the formData needed by the Borg API
            MapUserToFormDataModel(ref configData, user);

            /* Uncomment for testing and not calling the actual create
            return new BorgCreateResponseMessage()
            {
                Password = "eC4",
                URL = $"http://demo.ec4energy.com/{GetCustomerName(user)}",
                Username = user.Email
            };
            */

            //call the 5G API to get the initial access token
            var config = new HttpApiConfig()
            {
                Url = configData.CreateCustomerConfig.Url,
                Body = JsonConvert.SerializeObject(configData.CreateCustomerConfig.Body),
                //this requires an Authorization header with only the auth token as value
                AuthToken = new KeyValuePair<string, string>("", token)
            };

            string responseRaw = await _httpFactory.Run(config);
            var result = JsonConvert.DeserializeObject<BorgCreateResponse>(responseRaw);

            #region Extra parsing
            /*
            //because returned format is heavily escaped, we jump through some hoops to parse response.
            var responseUnescaped = responseRaw.Replace(@"\", "");
            //remove quote around loginResult value, message value
            responseUnescaped = responseUnescaped.Replace("\"{\"M", "{\"M"); //leading quote
            responseUnescaped = responseUnescaped.Substring(0, responseUnescaped.Length - 4) + "\"}}"; //trailing quote
            //now work on the message value to unescape it so it can be deserialized.
            responseUnescaped = responseUnescaped.Replace("\"Message\":\"{", "\"Message\":{"); //leading quote
            responseUnescaped = responseUnescaped.Replace("}\",\"Result\"", "},\"Result\""); //trailing quote
            var result = JsonConvert.DeserializeObject<BorgCreateResponse>(responseUnescaped);
            */
            #endregion

            if (result.CreateCustomerResult == null || string.IsNullOrEmpty(result.CreateCustomerResult.Result) ||
                !result.CreateCustomerResult.Result.ToLower().Equals("success"))
            {
                var msg = $"Unable to create customer against Borg Connect API. {(string)result.CreateCustomerResult.Message}.";
                base.CreateJobLogMessage(msg, TaskStatusEnum.Failed);
                _logger.LogError($"JobBorgConnectActivate|CreateCustomer|{msg}");
                throw new UnauthorizedAccessException(msg);
            }

            //on success, the message value is a different structure, convert it to the expected structure.
            var msgResult = JsonConvert.DeserializeObject<BorgCreateResponseMessage>(JsonConvert.SerializeObject(result.CreateCustomerResult.Message));

            //TBD - figure out how to pass back this sensitive info and not create a security hole. 
            return msgResult;
        }

        private static void MapUserToFormDataModel(ref JobBorgConnectActivateConfig configData, UserModel user)
        {
            //transfer values from user record to formData collection
            configData.CreateCustomerConfig.Body.formData.FirstOrDefault(x => x.Name.Equals("CustomerName")).Value = GetCustomerName(user);
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

        private static string GetCustomerName(UserModel user)
        {
            return (user.Organization == null ?
                new System.Text.RegularExpressions.Regex("[ ()*'\",_&#^@]").Replace(user.FullName, string.Empty) :
                new System.Text.RegularExpressions.Regex("[ ()*'\",_&#^@]").Replace(user.Organization.Name, string.Empty)) +
                DateTime.Now.ToString("yyMMddHHmm");  //append datetime stamp for now so we can multiple times for demos and get unique instance.
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
        public dynamic Message { get; set; }

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
        public dynamic Message { get; set; }

    }

    /// <summary>
    /// Structure of the create customer response message details when successful call
    /// </summary>
    internal class BorgCreateResponseMessage
    {
        public string URL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    }

    #endregion

}
