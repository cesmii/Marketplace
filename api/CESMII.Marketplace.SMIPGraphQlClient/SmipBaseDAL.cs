using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using GraphQL;
using GraphQL.Client;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

using CESMII.Marketplace.SmipGraphQlClient.Models;
using System.Text;

namespace CESMII.Marketplace.SmipGraphQlClient
{

    //smmarketplace
    //8XxPpWKYgELk
    //$2a$06$lJVBo1jICwn3JDIc3vpgHOnc8whLYAIbr9bKDCmbYY5mZ2QQRZ.iq|$2a$06$lJVBo1jICwn3JDIc3vpgHO

    public class SmipBaseDAL<TModel> where TModel : SmipAbstractModel 
    {
        protected readonly ILogger _logger;
        protected readonly ISmipRepo<TModel> _repo;

        public SmipBaseDAL(SmipAuthenticatorSettings settings, ILogger logger)
        {
            _repo = new SmipRepo<TModel>(settings, logger);
            _logger = logger;
        }

        public virtual async Task Authenticate()
        {
            //{ "Authorization":"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoic2FuZGJveF9yb19ncm91cCIsImV4cCI6MTcxMzIxNTQ3NiwidXNlcl9uYW1lIjoic2NveGVuIiwiYXV0aGVudGljYXRvciI6InNhbmRib3giLCJhdXRoZW50aWNhdGlvbl9pZCI6IjE0MTIiLCJpYXQiOjE3MTMyMTM2NzYsImF1ZCI6InBvc3RncmFwaGlsZSIsImlzcyI6InBvc3RncmFwaGlsZSJ9.aKNExoe3BiHjGNk5lvVCoEDMCEXH6HPdOlllDex53Wg"}
            //_repo.JwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoic2FuZGJveF9yb19ncm91cCIsImV4cCI6MTcxMzIxNTQ3NiwidXNlcl9uYW1lIjoic2NveGVuIiwiYXV0aGVudGljYXRvciI6InNhbmRib3giLCJhdXRoZW50aWNhdGlvbl9pZCI6IjE0MTIiLCJpYXQiOjE3MTMyMTM2NzYsImF1ZCI6InBvc3RncmFwaGlsZSIsImlzcyI6InBvc3RncmFwaGlsZSJ9.aKNExoe3BiHjGNk5lvVCoEDMCEXH6HPdOlllDex53Wg";
            await _repo.Authenticate();
        }

        public virtual async Task<List<TModel>> SearchAsync(string query)
        {
            throw new NotImplementedException();
            //var req = new GraphQLRequest
            //{
            //    Query = $"query {{organizations(condition: {{displayName: '{query}'}}) " +
            //            "{id, displayName, description, relativeName, parentOrganization {id, displayName}" +
            //            "}}}"
            //};

            //var result = await _client.SendQueryAsync<List<TModel>>(req);
            //if (result.Errors != null)
            //{
            //    var sbErrors = new System.Text.StringBuilder();
            //    foreach (var err in result.Errors)
            //    { 
            //        sbErrors.AppendLine(err.Message);
            //    }
            //    var msg = $"SmipWrapper.SearchOrganizationsAsync||{_client.}||{sbErrors.ToString()}";
            //    _logger.LogError(msg);
            //    throw new SmipGrapQlException($"SmipWrapper.SearchOrganizationsAsync||{sbErrors.ToString()}")
            //}
            //return result.Data;
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
    }

}
