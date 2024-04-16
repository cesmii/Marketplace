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

    public class SmipOrganizationDAL : SmipBaseDAL<SmipOrganizationModel>, ISmipDAL<SmipOrganizationModel>
    {
        public SmipOrganizationDAL(SmipAuthenticatorSettings settings, ILogger logger):
            base(settings, logger)
        {
        }

        public override async Task<List<SmipOrganizationModel>> SearchAsync(string query)
        {
            var req = new GraphQLRequest()
            {
                Query = @"
                    query get_orgs($query: String) {
                      organizations(condition: {displayName: $query}) {
                        id
                        displayName
                        description
                        relativeName
                      }
                    }",
                OperationName = "get_orgs",
                Variables = new
                {
                    query = query
                }
            };
            /*
                {
                  "data": {
                    "organizations": [
                      {
                        "id": "73316",
                        "displayName": "PPKOrg",
                        "description": "An Organization where Prakashan can test Organization queries",
                        "relativeName": "ppkorg"
                      }
                    ]
                  }
                }             
             */
            return await _repo.SearchAsync(req);
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
