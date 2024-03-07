using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Data.Extensions;

namespace CESMII.Marketplace.Service
{
    public class StripeService : IECommerceService<CartModel>
    {
        private readonly IDal<Cart, CartModel> _dal;

        public StripeService(IDal<Cart, CartModel> dal)
        {
            _dal = dal;
        }

        public IEnumerable<CartModel> GetAll()
        {
            return _dal.GetAll().OrderBy(x => x.Name).ToList();
        }

        public DALResult<CartModel> GetPaged(int? skip, int? take, bool returnCount = false, bool verbose = false) {
            var result = _dal.Where(new List<Func<Cart, bool>>(), skip, take, returnCount, verbose);
            result.Data = result.Data.OrderBy(x => x.Name).ToList();
            return result;
        }

        public DALResult<CartModel> Search(PagerFilterSimpleModel model, bool returnCount = false, bool verbose = false)
        {
            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            //get all including inactive. 
            Func<Cart, bool>? predicate = null;

            //now trim further by name if needed. 
            if (!string.IsNullOrEmpty(model.Query))
            {
                predicate = x => x.Name.ToLower().Equals(model.Query);
            }
            var orderBys = new OrderByExpression<Cart>() { Expression = x => x.Name };
            var result = predicate == null ?
                _dal.GetAllPaged(model.Skip, model.Take, true, true, orderBys)
                :
                _dal.Where(predicate, model.Skip, model.Take, true, true, orderBys);
            return result;
        }

        public CartModel GetById(string id) {
            return _dal.GetById(id);
        }

        public CartModel Copy(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<string> Add(CartModel item, string userId) { 
            return await _dal.Add(item, userId);
        }
        public async Task<int> Update(CartModel item, string userId) {
            return await _dal.Update(item, userId);
        }
        public async Task Delete(string id, string userId) {
            await _dal.Delete(id, userId);
        }

        public bool IsUnique(CartModel item)
        {
            throw new NotImplementedException();
        }
    }
}
