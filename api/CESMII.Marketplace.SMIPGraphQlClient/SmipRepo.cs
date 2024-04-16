using System.Text;

using Microsoft.Extensions.Logging;

using GraphQL;
using GraphQL.Client;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;

using CESMII.Marketplace.SmipGraphQlClient.Models;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace CESMII.Marketplace.SmipGraphQlClient
{

    //smmarketplace
    //8XxPpWKYgELk
    //$2a$06$lJVBo1jICwn3JDIc3vpgHOnc8whLYAIbr9bKDCmbYY5mZ2QQRZ.iq|$2a$06$lJVBo1jICwn3JDIc3vpgHO

    public class SmipRepo<TModel> :ISmipRepo<TModel> where TModel : SmipAbstractModel 
    {
        protected readonly ILogger _logger;
        protected readonly GraphQLHttpClient _client;
        protected readonly SmipAuthenticatorSettings _settings;
        private string _jwtToken;

        /// <summary>
        /// This will be set by Authenticate method
        /// </summary>
        public string JwtToken { get { return _jwtToken; } 
            set { 
                _jwtToken = value;
                //update auth header
                _client.HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", value);
            }
        }

        public SmipRepo(SmipAuthenticatorSettings settings, ILogger logger)
        {
            _settings = settings;
            //initialize GraphQL client
            _client = new GraphQLHttpClient(new Uri(settings.GraphQlUrl), new NewtonsoftJsonSerializer());
            _client.HttpClient.BaseAddress = new Uri(settings.GraphQlUrl);
            _logger = logger;
        }

        public async Task Authenticate()
        {
            //2 -step process - request authentication challenge.
            //{authenticator: "smmarketplace", role: "sandbox_ro_group", userName: "scoxen"}
            //convert inbound model to what SMIP expects
            /* step 1 request
                mutation get_challenge {
                  authenticationRequest(
                    input: {authenticator: "smmarketplace", role: "sandbox_ro_group", userName: "scoxen"}
                  ) {
                    jwtRequest {
                      challenge
                      message
                    }
                  }
                }
            */
            var reqChallenge = new GraphQLRequest()
            {
                Query = @"
                    mutation get_challenge($authenticator: String, $role: String, $userName: String)
                    {
                        authenticationRequest(
                        input: {authenticator: $authenticator, role: $role, userName: $userName}
                        ) {
                        jwtRequest {
                            challenge
                            message
                            }
                        }
                    }",
                OperationName = "get_challenge",
                Variables = new
                {
                    authenticator = _settings.Authenticator,
                    role = _settings.AuthenticatorRole,
                    userName = _settings.UserName
                }
            };
            /* step 1 response
                {
                  "data": {
                    "authenticationRequest": {
                      "jwtRequest": {
                        "challenge": "de98735adb9782956bc06007689b78a7",
                        "message": "Return \"$challenge|$password\" to validate"
                      }
                    }
                  }
                }
            */
            var respChallenge = await _client.SendMutationAsync<SmipAuthenticationChallengeResponseModel>(reqChallenge);
            if (respChallenge.Errors != null) CheckErrors(respChallenge.Errors);

            /* step 2 request
                mutation get_token {
                  authenticationValidation(
                    input: {
                      authenticator: "smmarketplace"
                      signedChallenge: "ea89fab65c9d07b30853ac7ac449c183|8XxPpWKYgELk"
                    }
                  ) {
                    jwtClaim
                  }
                }
            */
            var signedChallenge = $"{respChallenge.Data.authenticationRequest.jwtRequest.Challenge}|{_settings.Password}";
            var reqGetToken = new GraphQLRequest()
            {
                Query = @"
                    mutation get_token($authenticator: String, $signedChallenge: String)
                    {
                        authenticationValidation(
                            input: {
                                authenticator: $authenticator
                                signedChallenge: $signedChallenge
                            }
                        ) {
                            jwtClaim
                        }
                    }",
                OperationName = "get_token",
                Variables = new
                {
                    authenticator = _settings.Authenticator,
                    signedChallenge = signedChallenge
                }
            };
            /* step 2 response
            {
                "data": {
                    "authenticationValidation": {
                        "jwtClaim": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoic2FuZGJveF9yb19ncm91cCIsImV4cCI6MTcxMzIwNTkyOSwidXNlcl9uYW1lIjoic2NveGVuIiwiYXV0aGVudGljYXRvciI6InNtbWFya2V0cGxhY2UiLCJhdXRoZW50aWNhdGlvbl9pZCI6IjE0MDciLCJpYXQiOjE3MTMyMDQyNzUsImF1ZCI6InBvc3RncmFwaGlsZSIsImlzcyI6InBvc3RncmFwaGlsZSJ9.u61-A-4bnLhyv7rtJ41oRh6QupGaxlXR75pMM_kb2ws"
                    }
                }
            }
            */
            var respToken = await _client.SendMutationAsync<SmipAuthenticationTokenResponseModel>(reqGetToken);
            if (respToken.Errors != null) CheckErrors(respToken.Errors);
            
            //store token in class
            this.JwtToken = respToken.Data.authenticationValidation.jwtClaim;
        }

        public virtual async Task<TModel> GetById(GraphQLRequest req)
        {
            /* sample structure. result.Data?.First? == collection of orgs in this case
                {
                  "data": {
                    "organizations": [
                      {
                        "id": "73316",
                        "displayName": "PPKOrg",
                      }
                    ]
                  }
                }
            */

            GraphQLResponse<JObject> result = await _client.SendQueryAsync<JObject>(req).ConfigureAwait(false);
            if (result.Errors != null) CheckErrors(result.Errors);

            //convert to model
            if (result.Data?.First == null || result.Data?.First?.First == null) return null;
            string json = result.Data?.First?.First?.ToString();
            return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<TModel>(json);
        }

        public virtual async Task<List<TModel>> SearchAsync(GraphQLRequest req)
        {
            /* sample structure. result.Data?.First? == collection of orgs in this case
                {
                  "data": {
                    "organizations": [
                      {
                        "id": "73316",
                        "displayName": "PPKOrg",
                      }
                    ]
                  }
                }
            */

            GraphQLResponse<JObject> result = await _client.SendQueryAsync<JObject>(req).ConfigureAwait(false);
            if (result.Errors != null) CheckErrors(result.Errors);

            //convert to model
            if (result.Data?.First == null || result.Data?.First?.First == null) return null;
            string json = result.Data?.First?.First?.ToString();
            return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<List<TModel>>(json);
        }

        public virtual async Task<TModel> AddAsync(TModel item)
        {
            throw new NotImplementedException();
        }
            /*
                var query = new GraphQLRequest
                {
                    Query = @"
                            query ownerQuery($ownerID: ID!) {
                              owner(ownerId: $ownerID) {
                                id
                                name
                                address
                                accounts {
                                  id
                                  type
                                  description
                                }
                              }
                            }",
                    Variables = new { ownerID = id }
                };         
             */

        private void CheckErrors(GraphQLError[] errors)
        { 
            if (errors != null)
            {
                if (IsTokenExpired(errors)) {
                    _logger.LogWarning($"SmipRepo.SearchAsync||{_client.HttpClient.BaseAddress}||JWT Token expired");
                    throw new SmipJwtTokenException();
                }

                var sbErrors = new System.Text.StringBuilder();
                foreach (var err in errors)
                { 
                    sbErrors.AppendLine(err.Message);
                }
                var msg = $"SmipRepo.SearchAsync||{_client.HttpClient.BaseAddress}||{sbErrors.ToString()}";
                _logger.LogError(msg);
                throw new SmipGrapQlException($"SmipRepo.CheckErrors||{sbErrors.ToString()}");
            }
        }

        private bool IsTokenExpired(GraphQLError[] errors)
        {
            /* sample format of meesage
                {
                  "errors": [
                    {
                      "message": "jwt expired"
                    }
                  ]
                }             
             */
            if (errors == null || !errors.Any()) return false;
            foreach (var err in errors)
            {
                if (err.Message.ToLower().Contains("jwt expired")) return false;
            }
            return false;
        }

    }

}
