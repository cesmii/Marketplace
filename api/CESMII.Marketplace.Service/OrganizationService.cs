using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.Service
{

    public interface IOrganizationService<TModel>: ICommonService<TModel> where TModel : AbstractModel
    {
        TModel GetByName(string name);
    }

    public class OrganizationService : IOrganizationService<OrganizationModel>
    {
        private readonly IDal<Organization, OrganizationModel> _dal;

        public OrganizationService(IDal<Organization, OrganizationModel> dal)
        {
            _dal = dal;
        }

        public IEnumerable<OrganizationModel> GetAll()
        {
            return _dal.GetAll()
                .OrderBy(x => x.Name).ToList();
        }

        public DALResult<OrganizationModel> GetPaged(int? skip, int? take, bool returnCount = false, bool verbose = false) {
            var result = _dal.Where(new List<Func<Organization, bool>>(), skip, take, returnCount, verbose);
            result.Data = result.Data.OrderBy(x => x.Name).ToList();
            return result;
        }

        public DALResult<OrganizationModel> Search(PagerFilterSimpleModel model, bool returnCount = false, bool verbose = false) {
            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            //get all including inactive. 
            Func<Organization, bool>? predicate = null;

            //now trim further by name if needed. 
            if (!string.IsNullOrEmpty(model.Query))
            {
                predicate = x => x.Name.ToLower().Contains(model.Query);
            }
            //TBD - order by clauses causing Mongo DB query exception
            var result = predicate == null ?
                _dal.GetAllPaged(model.Skip, model.Take, true, true
                , new OrderByExpression<Organization>() { Expression = x => x.IsActive, IsDescending = true }
                , new OrderByExpression<Organization>() { Expression = x => x.Name }
                )
                :
                _dal.Where(predicate, model.Skip, model.Take, true, true
                , new OrderByExpression<Organization>() { Expression = x => x.IsActive, IsDescending = true }
                , new OrderByExpression<Organization>() { Expression = x => x.Name }
                )
                ;
            return result;
        }

        public OrganizationModel Copy(string id) {
            var result = _dal.GetById(id);
            if (result == null) return result;

            //clear out key values, then return as a new item
            result.ID = "";
            result.Name = $"{result.Name}-copy";
            return result;
        }

        public OrganizationModel GetById(string id) {
            return _dal.GetById(id);
        }

        public OrganizationModel GetByName(string name)
        {
            // Search for organization
            var filter = new PagerFilterSimpleModel() { Query = name, Skip = 0, Take = 9999 };
            var listMatchOrganizations = this.Search(filter).Data;

            if (listMatchOrganizations != null && listMatchOrganizations.Count == 1)
            {
                return listMatchOrganizations[0];
            }

            return null;
        }

        public async Task<string> Add(OrganizationModel item, string userId) {
            return await _dal.Add(item, userId);
        }

        public async Task<int> Update(OrganizationModel item, string userId) {
            return await _dal.Update(item, userId);
        }

        public async Task Delete(string id, string userId)
        {
            var result = await _dal.Delete(id, userId);
            if (result < 0)
            {
                throw new ArgumentException($"Could not delete item. Invalid id '{id}'.");
            }
        }

        public bool IsUnique(OrganizationModel item)
        {
            //name is supposed to be unique. Note name is different than display name.
            //if we get a match for something other than this id, return false
            var numItems = _dal.Count(x => x.IsActive && !x.ID.Equals(item.ID) &&
                x.Name.ToLower().Equals(item.Name.ToLower()));
            return numItems == 0;
        }

        public bool Validate(OrganizationModel item, out string message)
        {
            message = "";
            return true;
        }
    }
}
