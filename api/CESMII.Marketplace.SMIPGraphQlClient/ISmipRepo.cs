using GraphQL;
using CESMII.Marketplace.SmipGraphQlClient.Models;

namespace CESMII.Marketplace.SmipGraphQlClient
{
    public interface ISmipRepo<TModel> where TModel : SmipAbstractModel
    {
        string JwtToken { get; set; }
        Task Authenticate();
        Task<TModel> GetById(GraphQLRequest req);
        Task<List<TModel>> SearchAsync(GraphQLRequest req);
        Task<TModel> AddAsync(TModel item);
    }
}
