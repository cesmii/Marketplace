using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Data.Extensions;

namespace CESMII.Marketplace.Service
{
    //TBD - update this to reflect calls to the Stripe API as well as the Cart DAL
    public interface IECommerceService<TModel> where TModel : CartModel
    {
        IEnumerable<TModel> GetAll();
        DALResult<TModel> GetPaged(int? skip, int? take, bool returnCount = false, bool verbose = false);
        DALResult<TModel> Search(PagerFilterSimpleModel model, bool returnCount = false, bool verbose = false);
        TModel GetById(string id);
        /// <summary>
        /// This is specific to this service. Otherwise, equivalent to the common service
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<string> Add(TModel item, string userId);
        TModel Copy(string id);
        Task<int> Update(TModel item, string userId);
        Task Delete(string id, string userId);
        bool IsUnique(TModel item);
    }

}
