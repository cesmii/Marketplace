/*
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.Service
{
    public class SampleService : ICommonService<SectionModel>
    {
        private readonly IDal<Section, SectionModel> _dal;

        public SampleService(IDal<Section, SectionModel> dal)
        {
            _dal = dal;
        }

        public IEnumerable<SectionModel> GetAll()
        {
            return _dal.GetAll()
                .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Caption).ToList();
        }

        public DALResult<SectionModel> GetPaged(int? skip, int? take, bool returnCount = false, bool verbose = false) {
            var result = _dal.Where(new List<Func<Section, bool>>(), skip, take, returnCount, verbose);
            result.Data = result.Data.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Caption).ToList();
            return result;
        }

        public DALResult<SectionModel> Search(PagerFilterSimpleModel model, bool returnCount = false, bool verbose = false) {
            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            //get all including inactive. 
            Func<Section, bool>? predicate = null;

            //now trim further by name if needed. 
            if (!string.IsNullOrEmpty(model.Query))
            {
                predicate = x => x.Caption.ToLower().Contains(model.Query);
            }
            //TBD - order by clauses causing Mongo DB query exception
            var result = predicate == null ?
                _dal.GetAllPaged(model.Skip, model.Take, true, true
                //,new OrderByExpression<Section>() { Expression = x => x.IsActive, IsDescending = true }
                //,new OrderByExpression<Section>() { Expression = x => x.DisplayOrder }
                //,new OrderByExpression<Section>() { Expression = x => x.Caption }
                )
                :
                _dal.Where(predicate, model.Skip, model.Take, true, true
                //, new OrderByExpression<Section>() { Expression = x => x.IsActive, IsDescending = true }
                //, new OrderByExpression<Section>() { Expression = x => x.DisplayOrder }
                //, new OrderByExpression<Section>() { Expression = x => x.Caption }
                )
                ;
            return result;
        }

        public SectionModel Copy(string id) {
            var result = _dal.GetById(id);
            if (result == null) return result;

            //clear out key values, then return as a new item
            result.ID = "";
            result.Caption = $"{result.Caption}-copy";
            return result;
        }

        public SectionModel GetById(string id) {
            return _dal.GetById(id);
        }

        public async Task<string> Add(SectionModel item, string userId) {
            return await _dal.Add(item, userId);
        }

        public async Task<int> Update(SectionModel item, string userId) {
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

        public bool IsUnique(SectionModel item)
        {
            //name is supposed to be unique. Note name is different than display name.
            //if we get a match for something other than this id, return false
            var numItems = _dal.Count(x => x.IsActive && !x.ID.Equals(item.ID) &&
                x.Caption.ToLower().Equals(item.Caption.ToLower()));
            return numItems == 0;
        }
    }
}
    */
