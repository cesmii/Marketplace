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
    public class JobDefinitionDAL : BaseDAL<JobDefinition, JobDefinitionModel>, IDal<JobDefinition, JobDefinitionModel>
    {
        protected IMongoRepository<MarketplaceItem> _repoMarketplaceItem;
        protected List<MarketplaceItem> _marketplaceItemAll;
        protected new ILogger<JobDefinitionDAL> _logger;

        // Put this key in this in the following config setting location: PasswordSettings.EncryptionSettings.EncryptDecryptKey
        protected string _encryptDecryptKey;

        public JobDefinitionDAL(IMongoRepository<JobDefinition> repo,
            IMongoRepository<MarketplaceItem> repoMarketplaceItem,
            ConfigUtil configUtil, ILogger<JobDefinitionDAL> logger) : base(repo)
        {
            _repoMarketplaceItem = repoMarketplaceItem;
            _encryptDecryptKey = configUtil.PasswordConfigSettings.EncryptionSettings.EncryptDecryptKey;
            _logger = logger;
            if (string.IsNullOrEmpty(_encryptDecryptKey))
            {
                _logger.LogError($"Missing configuration for encryption key: cannot read or write job configuration information.");
                throw new Exception("Configuration value missing.");
            }
        }

        public async Task<string> Add(JobDefinitionModel model, string userId)
        {
            var entity = new JobDefinition
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

        public async Task<int> Update(JobDefinitionModel model, string userId)
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
        public override JobDefinitionModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id && x.IsActive)
                .FirstOrDefault();

            //get related data - pass list of item ids and publisher ids. 
            GetDependentData(new List<MongoDB.Bson.BsonObjectId>() { entity.MarketplaceItemId }).Wait();

            return MapToModel(entity, true);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override List<JobDefinitionModel> GetAll(bool verbose = false)
        {
            var result = GetAllPaged(verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <returns></returns>
        public override DALResult<JobDefinitionModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                x => x.IsActive,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                x => x.Name);  
            var count = returnCount ? _repo.Count(x => x.IsActive) : 0;

            GetDependentData(data.Select(x => x.MarketplaceItemId).Distinct().ToList()).Wait();

            //map the data to the final result
            var result = new DALResult<JobDefinitionModel>
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
        public override DALResult<JobDefinitionModel> Where(Func<JobDefinition, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,
                skip, take,
                x => x.Name);
            var count = returnCount ? _repo.Count(predicate) : 0;

            GetDependentData(data.Select(x => x.MarketplaceItemId).Distinct().ToList()).Wait();

            //map the data to the final result
            var result = new DALResult<JobDefinitionModel>
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

        protected override JobDefinitionModel MapToModel(JobDefinition entity, bool verbose = false)
        {
            if (entity != null)
            {
                var mktplItem = _marketplaceItemAll.FirstOrDefault(x => x.ID == entity.MarketplaceItemId.ToString());
                var result = new JobDefinitionModel
                {
                    ID = entity.ID,
                    MarketplaceItem = new MarketplaceItemSimpleModel()
                    {
                        ID = entity.MarketplaceItemId.ToString(),
                        DisplayName = mktplItem?.DisplayName,
                        Name = mktplItem?.Name
                    },
                    Name = entity.Name,
                    IconName = entity.IconName,
                    TypeName = entity.TypeName,
                    Data = !string.IsNullOrEmpty(entity.Data) ? 
                            PasswordUtils.DecryptString(entity.Data, _encryptDecryptKey) : null, 
                    IsActive = entity.IsActive
                };

                return result;
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref JobDefinition entity, JobDefinitionModel model)
        {
            entity.MarketplaceItemId = MongoDB.Bson.ObjectId.Parse(model.MarketplaceItem.ID);
            entity.Name = model.Name;
            entity.IconName = model.IconName;
            entity.TypeName = model.TypeName;
            entity.Data = PasswordUtils.EncryptString(model.Data, _encryptDecryptKey);
        }

        /// <summary>
        ///When mapping the results, we also get related data. For efficiency, get the look up data now and then
        ///mapToModel will apply to each item properly.
        ///get list of marketplace items
        /// </summary>
        /// <param name="marketplaceIds"></param>
        protected async Task GetDependentData(List<MongoDB.Bson.BsonObjectId> marketplaceIds)
        {
            var filterMarketplaceItems = MongoDB.Driver.Builders<MarketplaceItem>.Filter.In(x => x.ID, marketplaceIds.Select(y => y.ToString()));
            var fieldList = new List<string>()
                { nameof(MarketplaceItemSimple.ID), nameof(MarketplaceItemSimple.DisplayName), nameof(MarketplaceItemSimple.Name)};
            _marketplaceItemAll = await _repoMarketplaceItem.AggregateMatchAsync(filterMarketplaceItems, fieldList);
        }


    }
}