using Microsoft.Extensions.Logging;
using GraphQL;
using CESMII.Marketplace.SmipGraphQlClient.Models;

namespace CESMII.Marketplace.SmipGraphQlClient
{

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
            return await _repo.SearchAsync(req);
        }
    }

}
