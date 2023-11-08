namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.Common;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// </summary>
    public class ExternalSourceDAL : BaseDAL<ExternalSource, ExternalSourceModel>, IDal<ExternalSource, ExternalSourceModel>
    {
        protected IMongoRepository<LookupItem> _repoLookupItem;
        protected List<LookupItem> _smItemTypeMatches;
        protected IMongoRepository<Publisher> _repoPublisher;
        protected List<Publisher> _publishersAll;
        protected IMongoRepository<ImageItemSimple> _repoImages;  //get image info except the actual source data. 
        protected List<ImageItemSimple> _imagesAll;
        protected new ILogger<ExternalSourceDAL> _logger;

        // Put this key in this in the following config setting location: PasswordSettings.EncryptionSettings.EncryptDecryptKey
        protected string _encryptDecryptKey;

        public ExternalSourceDAL(IMongoRepository<ExternalSource> repo,
            IMongoRepository<LookupItem> repoLookupItem,
            IMongoRepository<Publisher> repoPublisher,
            IMongoRepository<ImageItemSimple> repoImages,
            ConfigUtil configUtil, ILogger<ExternalSourceDAL> logger) : base(repo)
        {
            _repoLookupItem = repoLookupItem;
            _repoPublisher = repoPublisher;
            _repoImages = repoImages;
            _encryptDecryptKey = configUtil.PasswordConfigSettings.EncryptionSettings.EncryptDecryptKey;
            _logger = logger;
            if (string.IsNullOrEmpty(_encryptDecryptKey))
            {
                _logger.LogError($"Missing configuration for encryption key: cannot read or write external source configuration information.");
                throw new Exception("Configuration value missing.");
            }
        }

        public async Task<string> Add(ExternalSourceModel model, string userId)
        {
            var entity = new ExternalSource
            {
                ID = null
                , Created = DateTime.UtcNow
                , Updated = DateTime.UtcNow
            };

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add
            entity.IsActive = true;

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added item
            return entity.ID;
        }

        public async Task<int> Update(ExternalSourceModel model, string userId)
        {
            var entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
            this.MapToEntity(ref entity, model);
            entity.Updated = DateTime.UtcNow;

            await _repo.UpdateAsync(entity);
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override ExternalSourceModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id && x.IsActive)
                .FirstOrDefault();

            //get related data - pass list of item ids and publisher ids. 
            GetDependentData(
                new List<MongoDB.Bson.BsonObjectId>() { entity.ItemTypeId },
                new List<MongoDB.Bson.BsonObjectId>() { entity.PublisherId },
                new List<MongoDB.Bson.BsonObjectId>() { entity.DefaultImageIdPortrait, entity.DefaultImageIdBanner, entity.DefaultImageIdLandscape }).Wait();

            return MapToModel(entity, true);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override List<ExternalSourceModel> GetAll(bool verbose = false)
        {
            var result = GetAllPaged(verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <returns></returns>
        public override DALResult<ExternalSourceModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                x => x.IsActive,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                x => x.Name);  
            var count = returnCount ? _repo.Count(x => x.IsActive) : 0;

            var listImageIds = data.Select(x => x.DefaultImageIdBanner)
                                .Union(data.Select(x => x.DefaultImageIdLandscape))
                                .Union(data.Select(x => x.DefaultImageIdPortrait))
                                .Distinct().ToList();
            GetDependentData(
                data.Select(x => x.ItemTypeId).Distinct().ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList(), 
                listImageIds).Wait();

            //map the data to the final result
            var result = new DALResult<ExternalSourceModel>
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
        public override DALResult<ExternalSourceModel> Where(Func<ExternalSource, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,
                skip, take,
                x => x.Name);
            var count = returnCount ? _repo.Count(predicate) : 0;

            var listImageIds = data.Select(x => x.DefaultImageIdBanner)
                                .Union(data.Select(x => x.DefaultImageIdLandscape))
                                .Union(data.Select(x => x.DefaultImageIdPortrait))
                                .Distinct().ToList();
            GetDependentData(
                data.Select(x => x.ItemTypeId).Distinct().ToList(),
                data.Select(x => x.PublisherId).Distinct().ToList(),
                listImageIds).Wait();

            //map the data to the final result
            var result = new DALResult<ExternalSourceModel>
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
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            return 1;
        }

        protected override ExternalSourceModel MapToModel(ExternalSource entity, bool verbose = false)
        {
            if (entity != null)
            {
                var itemType = _smItemTypeMatches.FirstOrDefault(x => x.ID == entity.ItemTypeId.ToString());
                var result = new ExternalSourceModel
                {
                    ID = entity.ID,
                    ItemType = new LookupItemModel()
                    {
                        ID = entity.ItemTypeId.ToString(),
                        Code = itemType.Code,
                        Name = itemType.Name, 
                        DisplayOrder = itemType.DisplayOrder
                    },
                    Name = entity.Name,
                    Code = entity.Code,
                    BaseUrl = entity.BaseUrl,
                    Enabled = entity.Enabled,
                    Data = !string.IsNullOrEmpty(entity.Data) ? 
                            PasswordUtils.DecryptString(entity.Data, _encryptDecryptKey) : null,
                    Publisher = MapToModelPublisher(entity.PublisherId, _publishersAll),
                    DefaultImagePortrait = entity.DefaultImageIdPortrait == null ? null : MapToModelImageSimple(x => x.ID.Equals(entity.DefaultImageIdPortrait.ToString()), _imagesAll),
                    DefaultImageBanner = entity.DefaultImageIdBanner == null ? null : MapToModelImageSimple(x => x.ID.Equals(entity.DefaultImageIdBanner.ToString()), _imagesAll),
                    DefaultImageLandscape = entity.DefaultImageIdLandscape == null ? null : MapToModelImageSimple(x => x.ID.Equals(entity.DefaultImageIdLandscape.ToString()), _imagesAll),
                    TypeName = entity.TypeName,
                    AdminTypeName = entity.AdminTypeName,
                    IsActive = entity.IsActive,
                    FailOnException = entity.FailOnException
                };

                return result;
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref ExternalSource entity, ExternalSourceModel model)
        {
            entity.ItemTypeId = MongoDB.Bson.ObjectId.Parse(model.ItemType.ID);
            entity.Name = model.Name;
            entity.Code = model.Code;
            entity.BaseUrl = model.BaseUrl;
            entity.Enabled = model.Enabled;
            entity.AdminTypeName = model.AdminTypeName;
            entity.TypeName = model.TypeName;
            //save/encrypt json data unique to this source
            entity.Data = PasswordUtils.EncryptString(model.Data, _encryptDecryptKey);
            //save default images
            entity.DefaultImageIdPortrait = model.DefaultImagePortrait == null ?
                MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY) :
                MongoDB.Bson.ObjectId.Parse(model.DefaultImagePortrait.ID);
            entity.DefaultImageIdBanner = model.DefaultImageBanner == null ?
                MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY) :
                MongoDB.Bson.ObjectId.Parse(model.DefaultImageBanner.ID);
            entity.DefaultImageIdLandscape = model.DefaultImageLandscape == null ?
                MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY) :
                MongoDB.Bson.ObjectId.Parse(model.DefaultImageLandscape.ID);
            entity.PublisherId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.Publisher.ID));
            entity.FailOnException = model.FailOnException;
        }

        /// <summary>
        ///When mapping the results, we also get related data. For efficiency, get the look up data now and then
        ///mapToModel will apply to each item properly.
        ///get list of marketplace items
        /// </summary>
        /// <param name="itemTypeIds"></param>
        protected async Task GetDependentData(
        List<MongoDB.Bson.BsonObjectId> itemTypeIds, 
        List<MongoDB.Bson.BsonObjectId> publisherIds,
        List<MongoDB.Bson.BsonObjectId> imageIds)
        {
            var filterItemTypes = MongoDB.Driver.Builders<LookupItem>.Filter.In(x => x.ID, itemTypeIds.Select(y => y.ToString()));
            _smItemTypeMatches = await _repoLookupItem.AggregateMatchAsync(filterItemTypes);

            //TBD - revisit and use BSONObject id for both parts. Requires to change ID type.
            var filterPubs = MongoDB.Driver.Builders<Publisher>.Filter.In(x => x.ID, publisherIds.Select(y => y.ToString()));
            _publishersAll = await _repoPublisher.AggregateMatchAsync(filterPubs);

            if (imageIds.Any(x => x != null))
            {
                var filterImages = MongoDB.Driver.Builders<ImageItemSimple>.Filter.In(x => x.ID, imageIds.Where(x => x != null).Select(y => y.ToString()));
                var fieldList = new List<string>()
                { nameof(ImageItemSimple.MarketplaceItemId), nameof(ImageItemSimple.FileName), nameof(ImageItemSimple.Type)};
                _imagesAll = await _repoImages.AggregateMatchAsync(filterImages, fieldList);
            }

        }
    }
}