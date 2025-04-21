using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.DAL.Models;

namespace CESMII.Marketplace.Service
{
    public interface ICommonService<TModel> where TModel : AbstractModel
    {
        IEnumerable<TModel> GetAll();
        DALResult<TModel> GetPaged(int? skip, int? take, bool returnCount = false, bool verbose = false);
        DALResult<TModel> Search(PagerFilterSimpleModel model, bool returnCount = false, bool verbose = false);
        TModel GetById(string id);
        Task<string> Add(TModel item, string userId);
        TModel Copy(string id);
        Task<int> Update(TModel item, string userId);
        Task Delete(string id, string userId);
        bool IsUnique(TModel item);
    }

}