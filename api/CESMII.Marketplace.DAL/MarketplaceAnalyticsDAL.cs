namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Most lookup data is contained in this single entity and differntiated by a lookup type. 
    /// </summary>
    public class MarkeplaceAnalyticsDAL : BaseDAL<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel>, IDal<MarketplaceItemAnalytics, MarketplaceItemAnalyticsModel>
    {
       
        protected List<MarketplaceDownloadHistory> _downloadItemsAll;
        public MarkeplaceAnalyticsDAL(IMongoRepository<MarketplaceItemAnalytics> repo) : base(repo)
        {
        }

        public async Task<string> Add(MarketplaceItemAnalyticsModel model, string userId)
        {
            MarketplaceItemAnalytics entity = new MarketplaceItemAnalytics
            {
                ID = ""
                //,Created = DateTime.UtcNow
                //,CreatedBy = userId
            };

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added user
            return entity.ID;
        }

        public async Task<int> Update(MarketplaceItemAnalyticsModel model, string userId)
        {
            MarketplaceItemAnalytics entity = _repo.FindByCondition(x => x.ID == model.ID ).FirstOrDefault();
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
        public override MarketplaceItemAnalyticsModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get all lookup items (no paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<MarketplaceItemAnalyticsModel> GetAll(bool verbose = false)
        {
            var result = GetAllPaged(null, null, verbose: verbose);
            return result.Data;
        }

        public override DALResult<MarketplaceItemAnalyticsModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.GetAll(
                    skip, take);  //TBD - add display order to lookup data
            var count = returnCount ? _repo.Count() : 0;

            //map the data to the final result
            var result = new DALResult<MarketplaceItemAnalyticsModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        public override DALResult<MarketplaceItemAnalyticsModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false,
            params OrderByExpression<MarketplaceItemAnalytics>[] orderByExpressions)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var predicates = new List<Func<MarketplaceItemAnalytics, bool>>();
            var data = _repo.FindByCondition(
                predicates,
                skip, take,
                orderByExpressions);
            var count = returnCount ? _repo.Count() : 0;

            //map the data to the final result
            var result = new DALResult<MarketplaceItemAnalyticsModel>
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
        public override DALResult<MarketplaceItemAnalyticsModel> Where(Func<MarketplaceItemAnalytics, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,
                skip, take,
                l => l.MarketplaceItemId, l => l.ID);  
            var count = returnCount ? _repo.Count(predicate) : 0;

            //map the data to the final result
            var result = new DALResult<MarketplaceItemAnalyticsModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;

        }

        protected override MarketplaceItemAnalyticsModel MapToModel(MarketplaceItemAnalytics entity, bool verbose = false)
        {
            if (entity != null)
            {
                return new MarketplaceItemAnalyticsModel
                {
                    ID = entity.ID,
                    MarketplaceItemId = entity.MarketplaceItemId.ToString(),
                    CloudLibId = entity.CloudLibId,
                    PageVisitCount = entity.PageVisitCount,
                    LikeCount = entity.LikeCount,
                    DislikeCount = entity.DislikeCount,
                    MoreInfoCount = entity.MoreInfoCount,
                    SearchResultCount = entity.ShareCount,
                    ShareCount = entity.ShareCount
                };
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref MarketplaceItemAnalytics entity, MarketplaceItemAnalyticsModel model)
        {
            entity.MarketplaceItemId = string.IsNullOrEmpty(model.MarketplaceItemId) ?
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY)) :
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.MarketplaceItemId));
            entity.CloudLibId = model.CloudLibId;
            entity.PageVisitCount = model.PageVisitCount;
            entity.LikeCount = model.LikeCount;
            entity.DislikeCount = model.DislikeCount;
            entity.MoreInfoCount = model.MoreInfoCount;
            entity.SearchResultCount = model.SearchResultCount;
            entity.ShareCount = model.ShareCount;

        }
    }
}