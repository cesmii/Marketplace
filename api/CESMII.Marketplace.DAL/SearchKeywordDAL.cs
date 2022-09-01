namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;

    /// <summary>
    /// </summary>
    public class SearchKeywordDAL : BaseDAL<SearchKeyword, SearchKeywordModel>, IDal<SearchKeyword, SearchKeywordModel>
    {
        public SearchKeywordDAL(IMongoRepository<SearchKeyword> repo) : base(repo)
        {
        }

        public async Task<string> Add(SearchKeywordModel model, string userId)
        {
            var entity = new SearchKeyword
            {
                ID = null
            };

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added item
            return entity.ID;
        }

        public async Task<int> Update(SearchKeywordModel model, string userId)
        {
            var entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
            this.MapToEntity(ref entity, model);

            await _repo.UpdateAsync(entity);
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override SearchKeywordModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override List<SearchKeywordModel> GetAll(bool verbose = false)
        {
            var result = GetAllPaged(verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <returns></returns>
        public override DALResult<SearchKeywordModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.GetAll(
                skip, take,
                x => x.Term, x => x.Code);  
            var count = returnCount ? _repo.Count() : 0;

            //map the data to the final result
            var result = new DALResult<SearchKeywordModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<SearchKeywordModel> Where(Func<SearchKeyword, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,
                skip, take,
                x => x.Term, x => x.Code);
            var count = returnCount ? _repo.Count(predicate) : 0;

            //map the data to the final result
            var result = new DALResult<SearchKeywordModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        public override async Task<int> Delete(string id, string userId)
        {
            var entity = _repo.FindByCondition(x => x.ID == id).FirstOrDefault();
            if (entity != null) await _repo.Delete(entity);

            return 1;
        }

        protected override SearchKeywordModel MapToModel(SearchKeyword entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new SearchKeywordModel
                {
                    ID = entity.ID,
                    Term = entity.Term,
                    Code = entity.Code
                };

                return result;
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref SearchKeyword entity, SearchKeywordModel model)
        {
            entity.Term = model.Term;
            entity.Code = model.Code;
        }
    }
}