namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.DAL.ExternalSources;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.Common.Enums;

    /// <summary>
    /// Sepcial version of the marketplace DAL. This is intended to be used by the admin.
    /// The main departure from the marketplace DAL is the way many to many lookup items are 
    /// handled. When we get all items, we return a full list of lookup items and then mark selected
    /// items that are chosen. This makes downstream processing easier on the front end
    /// </summary>
    /// <remarks>
    /// Some methods may not be supported to limit the use of this dal.
    /// </remarks>
    public class AdminMarketplaceDAL : BaseDAL<MarketplaceItem, AdminMarketplaceItemModel>, IDal<MarketplaceItem, AdminMarketplaceItemModel>
    {
        protected IMongoRepository<LookupItem> _repoLookup;
        protected List<LookupItem> _lookupItemsAll;
        protected IMongoRepository<Publisher> _repoPublisher;
        protected List<Publisher> _publishersAll;
        protected IMongoRepository<ImageItemSimple> _repoImages;
        protected List<ImageItemSimple> _imagesAll;
        protected readonly IExternalDAL<MarketplaceItemModel> _cloudLibDAL;

        //default type - use if none assigned yet.
        private readonly MongoDB.Bson.BsonObjectId _smItemTypeIdDefault;

        public AdminMarketplaceDAL(IMongoRepository<MarketplaceItem> repo, 
            IMongoRepository<LookupItem> repoLookup, 
            IMongoRepository<Publisher> repoPublisher, 
            IMongoRepository<ImageItemSimple> repoImages,
            IExternalDAL<MarketplaceItemModel> cloudLibDAL,
            ConfigUtil configUtil
            ) : base(repo)
        {
            _repoLookup = repoLookup;
            _repoPublisher = repoPublisher;
            _repoImages = repoImages;
            _cloudLibDAL = cloudLibDAL;

            //init some stuff we will use during the mapping methods
            _smItemTypeIdDefault = new MongoDB.Bson.BsonObjectId(
                MongoDB.Bson.ObjectId.Parse(configUtil.MarketplaceSettings.DefaultItemTypeId));
        }

        public async Task<string> Add(AdminMarketplaceItemModel model, string userId)
        {
            var entity = new MarketplaceItem
            {
                ID = ""
            };

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add
            entity.IsActive = true;
            entity.Created = DateTime.UtcNow.Date;
            entity.CreatedById = MongoDB.Bson.ObjectId.Parse(userId);

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added user
            return entity.ID;
        }

        public async Task<int> Update(AdminMarketplaceItemModel model, string userId)
        {
            MarketplaceItem entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
            this.MapToEntity(ref entity, model);
            entity.Updated = DateTime.UtcNow;
            entity.UpdatedById = MongoDB.Bson.ObjectId.Parse(userId);

            await _repo.UpdateAsync(entity);
            return 1;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override AdminMarketplaceItemModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();

            //get related data - pass list of item ids and publisher ids. 
            var ids = new List<MongoDB.Bson.BsonObjectId>() { new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(id)) };
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
        public override List<AdminMarketplaceItemModel> GetAll(bool verbose = false)
        {
            DALResult<AdminMarketplaceItemModel> result = GetAllPaged();
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<AdminMarketplaceItemModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var query = _repo.FindByCondition(
                x => x.IsActive,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.Name });
            var count = returnCount ? _repo.Count( x => x.IsActive )  : 0;

            //trigger the query to execute then we can limit what related data we query against
            var data = query.ToList();

            //get related data - pass list of item ids and publisher ids. 
            GetDependentData(
                data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList()).Wait();

            //map the data to the final result
            var result = new DALResult<AdminMarketplaceItemModel>
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
        public override DALResult<AdminMarketplaceItemModel> Where(List<Func<MarketplaceItem, bool>> predicates, int? skip, int? take, bool returnCount = false, bool verbose = false,
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
            GetDependentData(
                data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList()).Wait();

            //map the data to the final result
            var result = new DALResult<AdminMarketplaceItemModel>
            {
                Count = count,
                Data = MapToModels(data, verbose),
                SummaryData = null
            };
            return result;

        }


        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<AdminMarketplaceItemModel> Where(Func<MarketplaceItem, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var query = _repo.FindByCondition(
                predicate,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.IsFeatured, IsDescending = true },
                new OrderByExpression<MarketplaceItem>() { Expression = x => x.Name });
            var count = returnCount ? _repo.Count(predicate) : 0;

            //trigger the query to execute then we can limit what related data we query against
            var data = query.ToList();

            //get related data - pass list of item ids and publisher ids. 
            GetDependentData(
                data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList()).Wait();

            //map the data to the final result
            var result = new DALResult<AdminMarketplaceItemModel>
            {
                Count = count,
                Data = MapToModels(data, verbose),
                SummaryData = null
            };
            return result;

        }

        public override async Task<int> Delete(string id, string userId)
        {
            MarketplaceItem entity = _repo.FindByCondition(x => x.ID == id).FirstOrDefault();
            entity.Updated = DateTime.UtcNow;
            entity.UpdatedById = MongoDB.Bson.ObjectId.Parse(userId); 
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            return 1;
        }


        protected override AdminMarketplaceItemModel MapToModel(MarketplaceItem entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new AdminMarketplaceItemModel()
                {
                    ID = entity.ID,
                    //ensure this value is always without spaces and is lowercase. 
                    Name = entity.Name.ToLower().Trim().Replace(" ","-").Replace("_", "-"),  
                    DisplayName = entity.DisplayName,
                    IsFeatured = entity.IsFeatured,
                    IsVerified = entity.IsVerified,
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
                    Categories = MapToModelLookupItemsSelectable(entity.Categories, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.Process)).ToList()),
                    IndustryVerticals = MapToModelLookupItemsSelectable(entity.IndustryVerticals, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.IndustryVertical)).ToList()),
                    Status = MapToModelLookupItem(entity.StatusId, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.MarketplaceStatus)).ToList()),
                    Publisher = MapToModelPublisher(entity.PublisherId, _publishersAll),
                    IsActive = entity.IsActive,
                    ImagePortrait = entity.ImagePortraitId == null ? null : MapToModelImageSimple(x => x.ID.Equals(entity.ImagePortraitId.ToString()), _imagesAll),
                    ImageBanner = entity.ImageBannerId == null ? null : MapToModelImageSimple(z => z.ID.Equals(entity.ImageBannerId.ToString()), _imagesAll),
                    ImageLandscape = entity.ImageLandscapeId == null ? null : MapToModelImageSimple(x => x.ID.Equals(entity.ImageLandscapeId.ToString()), _imagesAll),
                    ccName1 = entity._ccName1,
                    ccName2 = entity._ccName2,
                    ccEmail1 = entity._ccEmail1,
                    ccEmail2 = entity._ccEmail2
                };
                if (verbose)
                {
                    result.RelatedItems = MapToModelRelatedItems(entity.RelatedItems).Result;
                    result.RelatedItemsExternal = MapToModelRelatedItemsExternal(entity.RelatedItemsExternal);
                    //map action links to model
                    result.ActionLinks = entity.ActionLinks;
                }
                return result;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Take a list of all marketplace items and then a list of related items for one item.
        /// With that info, mark the items in the full list as selected if they are a related item
        /// for this entity.
        /// The idea is we return the entire lookup list and mark selected those items appearing selected. 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="allItems"></param>
        /// <returns></returns>
        private async Task<List<MarketplaceItemRelatedModel>> MapToModelRelatedItems(List<RelatedItem> items)
        {
            if (items == null) return new List<MarketplaceItemRelatedModel>();

            //get the supplemental information that is associated with the related items
            var filterRelated = MongoDB.Driver.Builders<MarketplaceItem>.Filter.In(x => x.ID, items.Select(y => y.MarketplaceItemId.ToString()));
            var matches = await _repo.AggregateMatchAsync(filterRelated);

            return !matches.Any() ? new List<MarketplaceItemRelatedModel>() :
                matches.Select(x => new MarketplaceItemRelatedModel
                {
                    //ID = MapToModelRelatedItemsKey(items, x.ID),
                    RelatedId = x.ID,
                    Abstract = x.Abstract,
                    DisplayName = x.DisplayName,
                    //Description = x.Description,
                    Name = x.Name,
                    //Type = MapToModelLookupItem(x.ItemTypeId ?? _smItemTypeIdDefault,
                    //        _lookupItemsAll.Where(z => z.LookupType.EnumValue.Equals(LookupTypeEnum.SmItemType)).ToList()),
                    Version = x.Version,
                    //ImagePortrait = x.ImagePortraitId == null ? null : MapToModelImageSimple(z => z.ID.Equals(x.ImagePortraitId.ToString()), _imagesAll),
                    //ImageLandscape = x.ImageLandscapeId == null ? null : MapToModelImageSimple(z => z.ID.Equals(x.ImageLandscapeId.ToString()), _imagesAll),
                    //assumes only one related item per type
                    RelatedType = MapToModelRelatedItemsRelatedType(items, x.ID),
                    //Selected = items.Find(y => y.MarketplaceItemId.ToString().Equals(x.ID.ToString())) != null
                })
                .OrderBy(x => x.RelatedType.DisplayOrder)
                .ThenBy(x => x.RelatedType.Name)
                .ThenBy(x => x.DisplayName)
                .ToList();
        }

        //TBD - convert this over to use external data sources in full.
        /// <summary>
        /// Take a list of all profiles from CloudLib and then a list of profiles for one item.
        /// With that info, mark the items in the full list as selected if they are a related item
        /// for this entity.
        /// The idea is we return the entire lookup list and mark selected those items appearing selected. 
        /// </summary>
        protected List<ExternalSourceItemModel> MapToModelRelatedItemsExternal(List<RelatedExternalItem> items)
        {
            if (items == null) return new List<ExternalSourceItemModel>();

            //get the supplemental information that is associated with the related items
            var matches = _cloudLibDAL.GetManyById(items.Select(x => x.ExternalSource.ID).ToList()).Result.Data;

            return !matches.Any() ? new List<ExternalSourceItemModel>() :
                matches.Select(x => new ExternalSourceItemModel
                {
                    //ID = MapToModelRelatedProfilesKey(items, x.ID),
                    RelatedId = x.ID,
                    Description = x.Description,
                    DisplayName = x.DisplayName,
                    Name = x.Name,
                    Namespace = x.Namespace,
                    Version = x.Version,
                    //assumes only one related item per type
                    RelatedType = MapToModelRelatedItemsExternalRelatedType(items, x.ID),
                    ExternalSource = x.ExternalSource
                })
                .OrderBy(x => x.RelatedType.DisplayOrder)
                .ThenBy(x => x.RelatedType.Name)
                .ThenBy(x => x.DisplayName)
                .ToList();
        }

        private LookupItemModel MapToModelRelatedItemsRelatedType(
            List<RelatedItem> relatedItems, string id)
        {
            if (relatedItems == null) return null;
            if (relatedItems.Find(x => x.MarketplaceItemId.ToString().Equals(id)) == null) return null;

            return MapToModelLookupItem(
                relatedItems.Find(x => x.MarketplaceItemId.ToString().Equals(id)).RelatedTypeId,
                _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)).ToList());
        }

        /// <summary>
        /// Get the related type look up item associated with the related type id for this related external item
        /// </summary>
        /// <param name="relatedItems"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private LookupItemModel MapToModelRelatedItemsExternalRelatedType(
            List<RelatedExternalItem> relatedItems, string id)
        {
            if (relatedItems == null) return null;
            if (relatedItems.Find(x => x.ExternalSource.ID.Equals(id)) == null) return null;

            return MapToModelLookupItem(
                relatedItems.Find(x => x.ExternalSource.ID.Equals(id)).RelatedTypeId,
                _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.RelatedType)).ToList());
        }


        protected override void MapToEntity(ref MarketplaceItem entity, AdminMarketplaceItemModel model)
        {
            //ensure this value is always without spaces and is lowercase. 
            entity.Name = model.Name.ToLower().Trim().Replace(" ", "-").Replace("_", "-");
            entity.DisplayName = model.DisplayName;
            entity.IsFeatured = model.IsFeatured;
            entity.IsVerified = model.IsVerified;
            entity.Abstract = model.Abstract;
            entity.Description = model.Description;
            //backward compatible - assign sm app if no type is assigned
            entity.ItemTypeId = model.Type != null ?
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.Type.ID)) :
                _smItemTypeIdDefault;
            entity.StatusId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.Status.ID)); ;
            entity.Version = model.Version;
            entity.MetaTags = model.MetaTags;
            entity.Categories = model.Categories
                .Where(x => x.Selected) //only include selected rows
                .Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList();
            entity.IndustryVerticals = model.IndustryVerticals
                .Where(x => x.Selected) //only include selected rows
                .Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList();
            entity.PublisherId =  new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.Publisher.ID));
            entity.PublishDate = model.PublishDate;
            entity.ImagePortraitId = model.ImagePortrait == null ?
                MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY) :
                MongoDB.Bson.ObjectId.Parse(model.ImagePortrait.ID);
            entity.ImageBannerId = model.ImageBanner == null ?
                MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY) :
                MongoDB.Bson.ObjectId.Parse(model.ImageBanner.ID);
            entity.ImageLandscapeId = model.ImageLandscape == null ?
                MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY) :
                MongoDB.Bson.ObjectId.Parse(model.ImageLandscape.ID);

            //replace child collection of items - ids are preserved
            entity.RelatedItems = model.RelatedItems == null ? null : model.RelatedItems
                .Where(x => x.RelatedType != null) //only include selected rows
                .Select(x => new RelatedItem() {
                    MarketplaceItemId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.RelatedId)),
                    RelatedTypeId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.RelatedType.ID)),
                }).ToList();
            //replace child collection of items - ids are preserved
            entity.RelatedItemsExternal = model.RelatedItemsExternal == null ? null : model.RelatedItemsExternal
                .Where(x => x.RelatedType != null) //only include selected rows
                .Select(x => new RelatedExternalItem()
                {
                    ExternalSource = x.ExternalSource,
                    RelatedTypeId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.RelatedType.ID)),
                }).ToList();

            entity.ActionLinks = model.ActionLinks;

            entity._ccName1 = model.ccName1;
            entity._ccEmail1 = model.ccEmail1;
            entity._ccName2 = model.ccName2;
            entity._ccEmail2 = model.ccEmail2;
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
            //pubs
            var filterPubs = MongoDB.Driver.Builders<Publisher>.Filter.In(x => x.ID, publisherIds.Select(y => y.ToString()));
            _publishersAll = await _repoPublisher.AggregateMatchAsync(filterPubs);

            //images
            //add an empty id to list of marketplace id list
            var emptyId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Constants.BSON_OBJECTID_EMPTY));
            if (!marketplaceIds.Contains(emptyId)) marketplaceIds.Add(emptyId);
            var filterImages = MongoDB.Driver.Builders<ImageItemSimple>.Filter.In(x => x.MarketplaceItemId, marketplaceIds);
            _imagesAll = await _repoImages.AggregateMatchAsync(filterImages);
        }

    }
}