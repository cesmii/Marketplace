namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Common.Enums;

    public class MarketplaceDAL : BaseDAL<MarketplaceItem, MarketplaceItemModel>, IDal<MarketplaceItem, MarketplaceItemModel>
    {
        protected IMongoRepository<LookupItem> _repoLookup;
        protected List<LookupItem> _lookupItemsAll;
        protected IMongoRepository<Publisher> _repoPublisher;
        protected List<Publisher> _publishersAll;
        protected IMongoRepository<MarketplaceItemAnalytics> _repoAnalytics;
        protected List<MarketplaceItemAnalytics> _marketplaceItemAnalyticsAll;
        protected IMongoRepository<ImageItemSimple> _repoImages;  //get image info except the actual source data. 
        protected List<ImageItemSimple> _imagesAll;
        protected IMongoRepository<JobDefinition> _repoJobDefinition; 
        protected List<JobDefinition> _jobDefinitionAll;
        //default type - use if none assigned yet.
        private readonly MongoDB.Bson.BsonObjectId _smItemTypeIdDefault;

        public MarketplaceDAL(IMongoRepository<MarketplaceItem> repo, IMongoRepository<LookupItem> repoLookup, 
            IMongoRepository<Publisher> repoPublisher, 
            IMongoRepository<MarketplaceItemAnalytics> repoAnalytics,
            IMongoRepository<ImageItemSimple> repoImages,
            IMongoRepository<JobDefinition> repoJobDefinition,
            ConfigUtil configUtil
            ) : base(repo)
        {
            _repoLookup = repoLookup;
            _repoPublisher = repoPublisher;
            _repoAnalytics = repoAnalytics;
            _repoImages = repoImages;
            _repoJobDefinition = repoJobDefinition;

            //init some stuff we will use during the mapping methods
            _smItemTypeIdDefault = new MongoDB.Bson.BsonObjectId(
                MongoDB.Bson.ObjectId.Parse(configUtil.MarketplaceSettings.SmApp.TypeId));

        }

        public async Task<string> Add(MarketplaceItemModel model, string userId)
        {
            throw new NotSupportedException("For adding marketplace items, use AdminMarketplaceDAL");
        }

        public async Task<int> Update(MarketplaceItemModel model, string userId)
        {
            throw new NotSupportedException("For saving marketplace items, use AdminMarketplaceDAL");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override MarketplaceItemModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();

            //get related data - pass list of item ids and publisher ids. 
            GetMarketplaceRelatedData(
                new List<MongoDB.Bson.BsonObjectId>() { new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(id)) },
                new List<MongoDB.Bson.BsonObjectId>() { entity.PublisherId });

            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get all lookup items (no paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<MarketplaceItemModel> GetAll(bool verbose = false)
        {
            DALResult<MarketplaceItemModel> result = GetAllPaged();
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<MarketplaceItemModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var query = _repo.FindByCondition(
                x => x.IsActive,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName });
            var count = returnCount ? _repo.Count(x => x.IsActive) : 0;

            //trigger the query to execute then we can limit what related data we query against
            var data = query.ToList();

            //get related data - pass list of item ids and publisher ids. 
            GetMarketplaceRelatedData(
                data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList());

            //map the data to the final result
            var result = new DALResult<MarketplaceItemModel>
            {
                Count = count,
                Data = MapToModels(data, verbose),
                SummaryData = null
            };
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicates"></param>
        /// <returns></returns>
        public override DALResult<MarketplaceItemModel> Where(List<Func<MarketplaceItem, bool>> predicates, int? skip, int? take, bool returnCount = false, bool verbose = false,
            params OrderByExpression<MarketplaceItem>[] orderByExpressions)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var query = _repo.FindByCondition(
                predicates,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                orderByExpressions);
            var count = returnCount ? _repo.Count(predicates) : 0;

            //trigger the query to execute then we can limit what related data we query against
            var data = query.ToList();

            //get related data - pass list of item ids and publisher ids. 
            GetMarketplaceRelatedData(
                data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList());

            //map the data to the final result
            var result = new DALResult<MarketplaceItemModel>
            {
                Count = count,
                Data = MapToModels(data, verbose),
                SummaryData = null
            };
            return result;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicates"></param>
        /// <returns></returns>
        public override DALResult<MarketplaceItemModel> Where(Func<MarketplaceItem, bool> predicate, int? skip, int? take, bool returnCount = false, bool verbose = false,
            params OrderByExpression<MarketplaceItem>[] orderByExpressions)
        {
            var predicates = new List<Func<MarketplaceItem, bool>>
            {
                predicate
            };
            return this.Where(predicates, skip, take, returnCount, verbose, orderByExpressions);
        }

        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<MarketplaceItemModel> Where(Func<MarketplaceItem, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var query = _repo.FindByCondition(
                predicate,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true } ,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.DisplayName });
            var count = returnCount ? _repo.Count(predicate) : 0;

            //trigger the query to execute then we can limit what related data we query against
            var data = query.ToList();

            //get related data - pass list of item ids and publisher ids. 
            GetMarketplaceRelatedData(
                data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList());

            //map the data to the final result
            var result = new DALResult<MarketplaceItemModel>
            {
                Count = count,
                Data = MapToModels(data, verbose),
                SummaryData = null
            };
            return result;

        }

        public override async Task<int> Delete(string id, string userId)
        {
            throw new NotSupportedException("For deleting marketplace items, use AdminMarketplaceDAL");
        }


        protected override MarketplaceItemModel MapToModel(MarketplaceItem entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new MarketplaceItemModel
                {
                    ID = entity.ID,
                    //ensure this value is always without spaces and is lowercase. 
                    Name = entity.Name.ToLower().Trim().Replace(" ", "-").Replace("_", "-"),
                    DisplayName = entity.DisplayName,
                    Abstract = entity.Abstract,
                    Description = entity.Description,
                    Type = MapToModelLookupItem(entity.ItemTypeId ?? _smItemTypeIdDefault, 
                        _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.SmItemType)).ToList()),
                    AuthorId = entity.AuthorId,
                    Created = entity.Created,
                    Updated = entity.Updated,
                    PublishDate = entity.PublishDate,
                    Version = entity.Version,
                    //Type = new LookupItemModel() { ID = entity.TypeId, Name = entity.Type.Name }
                    MetaTags = entity.MetaTags,
                    // Categories = MapToModelLookupData(entity.Categories, _lookupItemsAll.Where(x => x.TypeId.Equals((int)LookupTypeEnum.Categories)).ToList()),
                    // IndustryVerticals = MapToModelLookupData(entity.IndustryVerticals, _lookupItemsAll.Where(x => x.TypeId.Equals((int)LookupTypeEnum.IndustryVerticals)).ToList()),
                    // MarketplaceStatus = MapToModelLookupData(entity.MarketplaceStatus, _lookupItemsAll.Where(x => x.TypeId.Equals((int)LookupTypeEnum.MarketplaceStatus)).ToList())
                    Categories = MapToModelLookupItems(entity.Categories, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.Process)).ToList()),
                    IndustryVerticals = MapToModelLookupItems(entity.IndustryVerticals, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.IndustryVertical)).ToList()),
                    Status = MapToModelLookupItem(entity.StatusId, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.MarketplaceStatus)).ToList()),
                    Analytics = MapToModelMarketplaceItemAnalyticsData(entity.ID, _marketplaceItemAnalyticsAll),
                    Publisher = MapToModelPublisher(entity.PublisherId, _publishersAll),
                    IsActive = entity.IsActive,
                    ImagePortrait = entity.ImagePortraitId == null ? null : MapToModelImageSimple(x => x.ID.Equals(entity.ImagePortraitId.ToString()), _imagesAll),
                    //ImageSquare = entity.ImageSquareId == null ? null : MapToModelImageSimple(x => x.ID.Equals(entity.ImageSquareId.ToString()), _imagesAll),
                    ImageLandscape = entity.ImageLandscapeId == null ? null : MapToModelImageSimple(x => x.ID.Equals(entity.ImageLandscapeId.ToString()), _imagesAll)
                };
                //get additional data under certain scenarios
                if (verbose)
                {
                    if (_jobDefinitionAll.Any())
                    {
                        result.JobDefinitions = _jobDefinitionAll
                            .Where(x => x.MarketplaceItemId.ToString().Equals(entity.ID))
                            .Select(x => new JobDefinitionSimpleModel { ID = x.ID, Name = x.Name }).ToList();
                    }
                }
                return result;
            }
            else
            {
                return null;
            }

        }

        // Add Maptomodelanalytics create new model
        protected static MarketplaceItemAnalyticsModel MapToModelMarketplaceItemAnalyticsData(string marketplaceItemId, List<MarketplaceItemAnalytics> allItems)
        {
            var entity = allItems.FirstOrDefault(x => x.MarketplaceItemId.ToString() == marketplaceItemId);

            if (entity == null)
            {
                return null;
            }
            return new MarketplaceItemAnalyticsModel
            {
                ID = entity.ID,
                MarketplaceItemId = entity.MarketplaceItemId.ToString(),
                PageVisitCount = entity.PageVisitCount,
                LikeCount = entity.LikeCount,
                DislikeCount = entity.DislikeCount,
                MoreInfoCount = entity.MoreInfoCount,
                SearchResultCount = entity.ShareCount,
                ShareCount = entity.ShareCount,
                // DownloadCount = entity.DownloadHistory == null ? 0 : entity.DownloadHistory.Count
            };

        }
        protected override void MapToEntity(ref MarketplaceItem entity, MarketplaceItemModel model)
        {
            //ensure this value is always without spaces and is lowercase. 
            entity.Name = model.Name.ToLower().Trim().Replace(" ", "-").Replace("_", "-");
            entity.DisplayName = model.DisplayName;
            entity.IsFeatured = model.IsFeatured;
            entity.IsVerified = model.IsVerified;
            entity.Abstract = model.Abstract;
            entity.Description = model.Description;
            entity.ItemTypeId = model.Type != null ?
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.Type.ID)) :
                _smItemTypeIdDefault;
            entity.StatusId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.Status.ID));
            entity.MetaTags = model.MetaTags;
            entity.Categories = model.Categories.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList();
            entity.IndustryVerticals = model.IndustryVerticals.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList();
            entity.PublisherId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.Publisher.ID));
        }

        /// <summary>
        ///When mapping the results, we also get related data. For efficiency, get the look up data now and then
        ///mapToModel will apply cats and industry verts to each item properly.
        ///get list of all categories, industry verticals
        /// </summary>
        /// <param name="marketplaceIds"></param>
        /// <param name="publisherIds"></param>
        protected void GetMarketplaceRelatedData(List<MongoDB.Bson.BsonObjectId> marketplaceIds, List<MongoDB.Bson.BsonObjectId> publisherIds)
        {
            _lookupItemsAll = _repoLookup.GetAll();

            //TBD - revisit and use BSONObject id for both parts. Requires to change ID type.
            var filterPubs = MongoDB.Driver.Builders<Publisher>.Filter.In(x => x.ID, publisherIds.Select(y => y.ToString()).ToArray());
            _publishersAll = _repoPublisher.AggregateMatch(filterPubs);

            var filterImages = MongoDB.Driver.Builders<ImageItemSimple>.Filter.In(x => x.MarketplaceItemId, marketplaceIds);
            var fieldList = new List<string>() 
                { nameof(ImageItemSimple.MarketplaceItemId), nameof(ImageItemSimple.FileName), nameof(ImageItemSimple.Type)};
            _imagesAll = _repoImages.AggregateMatch(filterImages, fieldList);

            var filterAnalytics = MongoDB.Driver.Builders<MarketplaceItemAnalytics>.Filter.In(x => x.MarketplaceItemId, marketplaceIds);
            _marketplaceItemAnalyticsAll = _repoAnalytics.AggregateMatch(filterAnalytics);

            var filterJobDef = MongoDB.Driver.Builders<JobDefinition>.Filter.In(x => x.MarketplaceItemId, marketplaceIds);
            _jobDefinitionAll = _repoJobDefinition.AggregateMatch(filterJobDef);
        }

    }
}