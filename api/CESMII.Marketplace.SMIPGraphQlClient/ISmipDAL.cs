using CESMII.Marketplace.SmipGraphQlClient.Models;

namespace CESMII.Marketplace.SmipGraphQlClient
{
    public interface ISmipDAL<TModel> where TModel : SmipAbstractModel
    {
        Task Authenticate();
        Task<List<TModel>> SearchAsync(string query);
        Task<TModel> AddAsync(TModel item);
    }
}
