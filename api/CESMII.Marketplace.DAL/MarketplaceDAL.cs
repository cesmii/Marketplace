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
        protected readonly ICloudLibDAL<MarketplaceItemModelWithCursor> _cloudLibDAL;

        //default type - use if none assigned yet.
        private readonly MongoDB.Bson.BsonObjectId _smItemTypeIdDefault;

        public MarketplaceDAL(IMongoRepository<MarketplaceItem> repo, IMongoRepository<LookupItem> repoLookup, 
            IMongoRepository<Publisher> repoPublisher, 
            IMongoRepository<MarketplaceItemAnalytics> repoAnalytics,
            IMongoRepository<ImageItemSimple> repoImages,
            IMongoRepository<JobDefinition> repoJobDefinition,
            ICloudLibDAL<MarketplaceItemModelWithCursor> cloudLibDAL,
            ConfigUtil configUtil
            ) : base(repo)
        {
            _repoLookup = repoLookup;
            _repoPublisher = repoPublisher;
            _repoAnalytics = repoAnalytics;
            _repoImages = repoImages;
            _repoJobDefinition = repoJobDefinition;
            _cloudLibDAL = cloudLibDAL;

            //init some stuff we will use during the mapping methods
            _smItemTypeIdDefault = new MongoDB.Bson.BsonObjectId(
                MongoDB.Bson.ObjectId.Parse(configUtil.MarketplaceSettings.SmApp.TypeId));

        }

        public Task<string> Add(MarketplaceItemModel model, string userId)
        {
            throw new NotSupportedException("For adding marketplace items, use AdminMarketplaceDAL");
        }

        public Task<int> Update(MarketplaceItemModel model, string userId)
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
            //pass in this id as well as ids from relatedItems
            var ids = new List<MongoDB.Bson.BsonObjectId>() { new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(id)) };
            ids = !entity.RelatedItems.Any() ? ids : ids.Union(entity.RelatedItems.Select(x => x.MarketplaceItemId)).ToList();
            GetDependentData(
                ids,
                new List<MongoDB.Bson.BsonObjectId>() { entity.PublisherId }).Wait();

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
            GetDependentData(
                data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList()).Wait();

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
                null, null,
                orderByExpressions);
            var count = returnCount ? query.Count() : 0;

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }
            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            //trigger the query to execute then we can limit what related data we query against
            var data = query.ToList();

            //get related data - pass list of item ids and publisher ids. 
            GetDependentData(
                data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList()).Wait();

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
            var ids = data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList();
            //if verbose, then pull in images from relatedItems child collection.
            if (verbose)
            {
                var relatedItemIds = data.SelectMany(x => x.RelatedItems == null || !x.RelatedItems.Any() ?
                    new List<MongoDB.Bson.BsonObjectId>() :
                    x.RelatedItems.Select(y => y.MarketplaceItemId)).ToList();
                ids = ids.Union(relatedItemIds).ToList();
            }
            GetDependentData(ids,
                data.Select(x => x.PublisherId).Distinct().ToList()).Wait();

            //map the data to the final result
            var result = new DALResult<MarketplaceItemModel>
            {
                Count = count,
                Data = MapToModels(data, verbose),
                SummaryData = null
            };
            return result;

        }

        public override Task<int> Delete(string id, string userId)
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

                    //get list of marketplace items associated with this list of ids, map to return object
                    var relatedItems = MapToModelRelatedItems(entity.RelatedItems);

                    //get related profiles from CloudLib
                    var relatedProfiles = MapToModelRelatedProfiles(entity.RelatedProfiles);
                    
                    //map related items into specific buckets - required, recommended
                    result.RelatedItemsGrouped = GroupAndMergeRelatedItems(relatedItems, relatedProfiles);
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

        /// <summary>
        /// Get related items from DB, filter out each group based on required/recommended/related flag
        /// assume all related items in same collection and a type id distinguishes between the types. 
        /// </summary>
        protected List<MarketplaceItemRelatedModel> MapToModelRelatedItems(List<RelatedItem> items)
        {
            if (items == null)
            {
                return new List<MarketplaceItemRelatedModel>();
            }

            //get list of marketplace items associated with this list of ids, map to return object
            var matches = _repo.FindByCondition(x => 
                items.Any(y => y.MarketplaceItemId.Equals(
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))))).ToList();
            return !matches.Any() ? new List<MarketplaceItemRelatedModel>() :
                matches.Select(x => new MarketplaceItemRelatedModel()
                {
                    //ID = x.ID,
                    RelatedId = x.ID,
                    Abstract = x.Abstract,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    Name = x.Name,
                    Type = MapToModelLookupItem(x.ItemTypeId ?? _smItemTypeIdDefault,
                            _lookupItemsAll.Where(z => z.LookupType.EnumValue.Equals(LookupTypeEnum.SmItemType)).ToList()),
                    Version = x.Version,
                    ImagePortrait = x.ImagePortraitId == null ? null : MapToModelImageSimple(z => z.ID.Equals(x.ImagePortraitId.ToString()), _imagesAll),
                    ImageLandscape = x.ImageLandscapeId == null ? null : MapToModelImageSimple(z => z.ID.Equals(x.ImageLandscapeId.ToString()), _imagesAll),
                    //assumes only one related item per type
                    RelatedType = MapToModelLookupItem(items.Find(z => z.MarketplaceItemId.ToString().Equals(x.ID)).RelatedTypeId,
                            _lookupItemsAll.Where(z => z.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)).ToList()),
                }).ToList();
        }

        /// <summary>
        /// Get related items from DB, filter out each group based on required/recommended/related flag
        /// assume all related items in same collection and a type id distinguishes between the types. 
        /// </summary>
        protected List<MarketplaceItemRelatedModel> MapToModelRelatedProfiles(List<RelatedProfileItem> items)
        {
            if (items == null)
            {
                return new List<MarketplaceItemRelatedModel>();
            }

            //get list of profile items associated with this list of ids, call CloudLib to get the supporting info for these
            var matches = _cloudLibDAL.GetManyById(items.Select(x => x.ProfileId).ToList()).Result;
            return !matches.Any() ? new List<MarketplaceItemRelatedModel>() : 
                matches.Select(x => new MarketplaceItemRelatedModel()
                {
                    //ID = x.ID,
                    RelatedId = x.ID,
                    Abstract = x.Abstract,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    Name = x.Name,
                    Namespace = x.Namespace,
                    Type = x.Type,
                    Version = x.Version,
                    ImagePortrait = x.ImagePortrait,
                    ImageLandscape = x.ImageLandscape,
                    //assumes only one related item per type
                    RelatedType = MapToModelLookupItem(items.Find(z => z.ProfileId.ToString().Equals(x.ID)).RelatedTypeId,
                            _lookupItemsAll.Where(z => z.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)).ToList()),
                }).ToList();
        }

        /// <summary>
        /// Take two related sets, group them by type and union them, filter them by related type and order them
        /// </summary>
        /// <param name="items"></param>
        /// <param name="itemsProfile"></param>
        /// <param name="relatedType"></param>
        /// <returns></returns>
        protected List<RelatedItemsGroupBy> GroupAndMergeRelatedItems(
            List<MarketplaceItemRelatedModel> items,
            List<MarketplaceItemRelatedModel> itemsProfile)
        {
            if (items == null && itemsProfile == null)
            {
                return new List<RelatedItemsGroupBy>();
            }
            //group by both sets and then merge
            var result = new List<RelatedItemsGroupBy>();

            //convert group to return type
            if (items?.Count > 0)
            {
                var grpItems = items.GroupBy(x => new { ID = x.RelatedType.ID });
                foreach (var item in grpItems)
                {
                    result.Add(new RelatedItemsGroupBy()
                    {
                        RelatedType = items.Where(x => x.RelatedType.ID.Equals(item.Key.ID)).FirstOrDefault()?.RelatedType,
                        Items = items.Where(x => x.RelatedType.ID.Equals(item.Key.ID)).ToList() // item.ToList()
                    });
                }
            }

            //append profiles group to existing group (if present)
            if (itemsProfile?.Count > 0)
            {
                var grpProfile = itemsProfile.GroupBy(x => new { ID = x.RelatedType.ID });
                foreach (var item in grpProfile)
                {
                    var matches = itemsProfile.Where(x => x.RelatedType.ID.Equals(item.Key.ID)).ToList();

                    var existingGroup = result.Find(x => x.RelatedType.ID.Equals(item.Key.ID));
                    if (existingGroup == null)
                    {
                        result.Add(new RelatedItemsGroupBy()
                        {
                            RelatedType = matches.FirstOrDefault()?.RelatedType,
                            Items = matches
                        });
                    }
                    else
                    {
                        existingGroup.Items = existingGroup.Items.Union(matches).ToList();
                    }
                }
            }

            //do some ordering
            result = result
                .OrderBy(x => x.RelatedType.DisplayOrder)
                .ThenBy(x => x.RelatedType.Name).ToList();
            foreach (var g in result)
            {
                g.Items = g.Items
                    .OrderBy(x => x.DisplayName)
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.Namespace)
                    .ThenBy(x => x.Version)
                    .ToList();
            }

            return result;
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
        protected async Task GetDependentData(List<MongoDB.Bson.BsonObjectId> marketplaceIds, List<MongoDB.Bson.BsonObjectId> publisherIds)
        {
            _lookupItemsAll = _repoLookup.GetAll();

            //TBD - revisit and use BSONObject id for both parts. Requires to change ID type.
            var filterPubs = MongoDB.Driver.Builders<Publisher>.Filter.In(x => x.ID, publisherIds.Select(y => y.ToString()));
            _publishersAll = await _repoPublisher.AggregateMatchAsync(filterPubs);

            var filterImages = MongoDB.Driver.Builders<ImageItemSimple>.Filter.In(x => x.MarketplaceItemId, marketplaceIds);
            var fieldList = new List<string>() 
                { nameof(ImageItemSimple.MarketplaceItemId), nameof(ImageItemSimple.FileName), nameof(ImageItemSimple.Type)};
            _imagesAll = await _repoImages.AggregateMatchAsync(filterImages, fieldList);

            var filterAnalytics = MongoDB.Driver.Builders<MarketplaceItemAnalytics>.Filter.In(x => x.MarketplaceItemId, marketplaceIds);
            _marketplaceItemAnalyticsAll = await _repoAnalytics.AggregateMatchAsync(filterAnalytics);

            var filterJobDef = MongoDB.Driver.Builders<JobDefinition>.Filter.In(x => x.MarketplaceItemId, marketplaceIds);
            _jobDefinitionAll = await _repoJobDefinition.AggregateMatchAsync(filterJobDef);

            if (_publishersAll.Count() == 0 && publisherIds.Any())
            {
                _logger.Warn($"GetDependentData || _publishersAll - item count 0: {string.Join(", ", publisherIds.Any())}.");
            }
        }

    }
}