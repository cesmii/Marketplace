namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.DAL.ExternalSources;

    /// <summary>
    /// Most lookup data is contained in this single entity and differntiated by a lookup type. 
    /// </summary>
    public class RequestInfoDAL : BaseDAL<RequestInfo, RequestInfoModel>, IDal<RequestInfo, RequestInfoModel>
    {
       
        
        protected IMongoRepository<LookupItem> _repoLookup;
        protected List<LookupItem> _lookupItemsAll;
        protected IMongoRepository<MarketplaceItem> _repoMarketplace;
        protected IMongoRepository<Publisher> _repoPublisher;
        protected IMongoRepository<MarketplaceItemAnalytics> _repoAnalytics;
        protected readonly IExternalSourceFactory<MarketplaceItemModel> _sourceFactory;
        protected readonly IDal<ExternalSource, ExternalSourceModel> _dalExternalSource;

        public RequestInfoDAL(IMongoRepository<RequestInfo> repo
            , IMongoRepository<LookupItem> repoLookup
            , IMongoRepository<MarketplaceItem> repoMarketplace
            , IMongoRepository<MarketplaceItemAnalytics> repoAnalytics
            , IMongoRepository<Publisher> repoPublisher
            , IExternalSourceFactory<MarketplaceItemModel> sourceFactory
            , IDal<ExternalSource, ExternalSourceModel> dalExternalSource
            ) : base(repo)
        {
            _repoLookup = repoLookup;
            _repoMarketplace = repoMarketplace;
            _repoAnalytics = repoAnalytics;
            _repoPublisher = repoPublisher;
            _dalExternalSource = dalExternalSource;
            _sourceFactory = sourceFactory;
        }

        public async Task<string> Add(RequestInfoModel model, string userId)
        {
            var entity = new RequestInfo
            {
                ID = ""
                //,Created = DateTime.UtcNow
                //,CreatedBy = userId
            };

            //init to not started
            var matches = _repoLookup.FindByCondition(x => x.LookupType.EnumValue == LookupTypeEnum.TaskStatus).ToList();
            var matchesStatus = matches.Where(x => x.Code.ToString().ToLower().Equals("not-started")).ToList();
            if (matchesStatus.Count == 0)
            {
                model.Status = new LookupItemModel() { ID = Common.Constants.BSON_OBJECTID_EMPTY };
            }
            else
            {
                model.Status = new LookupItemModel() { ID = matches.ToList()[0].ID };
            }

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add
            entity.Created = DateTime.UtcNow;
            entity.IsActive = true;

            if (!string.IsNullOrEmpty(model.MarketplaceItemId))
            {
                IncrementMarketplaceAnalytics(new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.MarketplaceItemId)));
            }

            else if (model.ExternalSource != null)
            {
                await IncrementExternalSourceAnalytics(model.ExternalSource);
            }

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added user
            return entity.ID;
        }

        public async Task<int> Update(RequestInfoModel model, string userId)
        {
            RequestInfo entity = _repo.FindByCondition(x => x.ID == model.ID ).FirstOrDefault();
            this.MapToEntity(ref entity, model);
            entity.Updated = DateTime.UtcNow;
            //this can be called from anonymous user so user id can be null
            if (!string.IsNullOrEmpty(userId)) {
                entity.UpdatedById = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(userId));
            }

            await _repo.UpdateAsync(entity);
            return 1;
        }

        public override async Task<int> Delete(string id, string userId)
        {
            RequestInfo entity = _repo.FindByCondition(x => x.ID == id).FirstOrDefault();
            entity.Updated = DateTime.UtcNow;
            entity.UpdatedById = MongoDB.Bson.ObjectId.Parse(userId);
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            return 1;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override RequestInfoModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            _lookupItemsAll = _repoLookup.FindByCondition(x => x.LookupType.EnumValue == LookupTypeEnum.RequestInfo ||
                x.LookupType.EnumValue == LookupTypeEnum.TaskStatus || x.LookupType.EnumValue == LookupTypeEnum.MembershipStatus).ToList();

            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get all lookup items (no paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<RequestInfoModel> GetAll(bool verbose = false)
        {
            var result = GetAllPaged(verbose: verbose);
            return result.Data;
        }

        public override DALResult<RequestInfoModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                    x => x.IsActive,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                    skip, take,
                    new OrderByExpression<RequestInfo>() { Expression = x => x.Created, IsDescending = true },
                    new OrderByExpression<RequestInfo>() { Expression = x => x.LastName },
                    new OrderByExpression<RequestInfo>() { Expression = x => x.FirstName }
                    );  
            var count = returnCount ? _repo.Count() : 0;

            _lookupItemsAll = _repoLookup.FindByCondition(x => x.LookupType.EnumValue == LookupTypeEnum.RequestInfo ||
                x.LookupType.EnumValue == LookupTypeEnum.TaskStatus || x.LookupType.EnumValue == LookupTypeEnum.MembershipStatus).ToList();

            //map the data to the final result
            var result = new DALResult<RequestInfoModel>
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
        public override DALResult<RequestInfoModel> Where(Func<RequestInfo, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,
                skip, take,
                new OrderByExpression<RequestInfo>(){ Expression= x => x.Created, IsDescending = true},
                new OrderByExpression<RequestInfo>() { Expression = x => x.LastName },
                new OrderByExpression<RequestInfo>(){ Expression= x => x.FirstName}
                );  
            var count = returnCount ? _repo.Count(predicate) : 0;

            _lookupItemsAll = _repoLookup.FindByCondition(x => x.LookupType.EnumValue == LookupTypeEnum.RequestInfo ||
                x.LookupType.EnumValue == LookupTypeEnum.TaskStatus || x.LookupType.EnumValue == LookupTypeEnum.MembershipStatus).ToList();

            //map the data to the final result
            var result = new DALResult<RequestInfoModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;

        }

        protected override RequestInfoModel MapToModel(RequestInfo entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new RequestInfoModel
                {
                    ID = entity.ID,
                    MarketplaceItemId = entity.MarketplaceItemId.ToString(),
                    MarketplaceItem = entity.MarketplaceItemId.ToString().Equals(Common.Constants.BSON_OBJECTID_EMPTY) ? null :
                        MapToModelMarketplaceItemSimple(entity.MarketplaceItemId),
                    PublisherId = entity.PublisherId.ToString(),
                    Publisher = entity.PublisherId.ToString().Equals(Common.Constants.BSON_OBJECTID_EMPTY) ? null :
                        MapToModelPublisher(entity.PublisherId),
                    ExternalSource = entity.ExternalSource, 
                    RequestType = MapToModelLookupItem(entity.RequestTypeId, _lookupItemsAll),
                    MembershipStatus = MapToModelLookupItem(entity.MembershipStatusId, _lookupItemsAll),
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    CompanyName = entity.CompanyName,
                    CompanyUrl = entity.CompanyUrl,
                    Description = entity.Description,
                    Notes = entity.Notes,
                    Email = entity.Email,
                    Phone = entity.Phone,
                    Industries = entity.Industries,
                    Created = entity.Created,
                    Updated = entity.Updated,
                    Status = MapToModelLookupItem(entity.StatusId, _lookupItemsAll),
                    IsActive = entity.IsActive,
                    ccEmail1 = entity.ccEmail1,
                    ccEmail2 = entity.ccEmail2,
                    ccName1 = entity.ccName1,
                    ccName2 = entity.ccName2,
                };
                
                if (result.RequestType != null)
                {
                    result.RequestTypeCode = result.RequestType.Code;
                }
                if (verbose)
                {
                    result.ExternalItem = entity.ExternalSource == null ? null :
                        MapToModelExternalSourceItem(entity.ExternalSource).Result;
                }
                return result;
            }
            else
            {
                return null;
            }

        }

        protected PublisherModel MapToModelPublisher(MongoDB.Bson.BsonObjectId id)
        {

            var matches = _repoPublisher.FindByCondition(x => x.ID == id.ToString());
            if (!matches.Any()) return null;
            return base.MapToModelPublisher(id, matches.ToList());
        }

        protected MarketplaceItemModel MapToModelMarketplaceItemSimple(MongoDB.Bson.BsonObjectId id)
        {

            var entity = _repoMarketplace.FindByCondition(x => x.ID == id.ToString()).FirstOrDefault();
            if (entity == null) return null;

            //return simplified version - all we need is id, name, abstract.
            return new MarketplaceItemModel
            {
                ID = entity.ID,
                //ensure this value is always without spaces and is lowercase. 
                Name = entity.Name.ToLower().Trim().Replace(" ", "-").Replace("_", "-"),
                DisplayName = entity.DisplayName,
                Abstract = entity.Abstract,
                PublishDate = entity.PublishDate,
                ccName1 = entity._ccName1,
                ccName2 = entity._ccName2,
                ccEmail1 = entity._ccEmail1,
                ccEmail2 = entity._ccEmail2
            };
        }


        protected override void MapToEntity(ref RequestInfo entity, RequestInfoModel model)
        {
            //get request type id based on request type code
            var matches = _repoLookup.FindByCondition(x => x.LookupType.EnumValue == LookupTypeEnum.RequestInfo).ToList();
            var matchesRequestType = matches.Where(x => x.LookupType.EnumValue == LookupTypeEnum.RequestInfo
                    && !string.IsNullOrEmpty(x.Code) && x.Code.ToString().ToLower().Equals(model.RequestTypeCode.ToLower())).ToList();
            if (matchesRequestType.Count == 0)
            {
                entity.RequestTypeId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY));
            }
            else
            {
                entity.RequestTypeId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(matchesRequestType[0].ID));
            }

            //entity.MarketplaceItemId = (model.MarketplaceItemId == null) ? entity.MarketplaceItemId : new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.MarketplaceItemId));
            //entity.PublisherId = (model.PublisherId == null) ? entity.PublisherId : new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.PublisherId));
            entity.MarketplaceItemId = (model.MarketplaceItemId == null) ?
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY)) : 
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.MarketplaceItemId));
            entity.PublisherId = (model.PublisherId == null) ?
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY)) : 
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.PublisherId));
            entity.ExternalSource = model.ExternalSource;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.CompanyName = model.CompanyName;
            entity.CompanyUrl = model.CompanyUrl;
            entity.Description = model.Description;
            entity.Notes = model.Notes;
            entity.Email = model.Email;
            entity.Phone = model.Phone;
            entity.Industries = model.Industries;
            entity.MembershipStatusId = model.MembershipStatus == null ?
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY)) : 
                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.MembershipStatus.ID));
            entity.StatusId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.Status.ID));
            entity.ccEmail1 = model.ccEmail1;
            entity.ccEmail2 = model.ccEmail2;
            entity.ccName1 = model.ccName1;
            entity.ccName2 = model.ccName2;
        }

        private void IncrementMarketplaceAnalytics(MongoDB.Bson.BsonObjectId marketplaceItemId)
        {
            //Increment Page Count
            //Check if MpItem is there if not add a new one then increment count and save
            var analytic = _repoAnalytics.FindByCondition(x => x.MarketplaceItemId == marketplaceItemId).FirstOrDefault();

            if (analytic == null)
            {
                analytic = new MarketplaceItemAnalytics() { MarketplaceItemId = marketplaceItemId, MoreInfoCount = 1 };
                _repoAnalytics.Add(analytic);
            }
            else
            {
                analytic.MoreInfoCount += 1;
                _repoAnalytics.Update(analytic);
            }
        }

        public async Task IncrementExternalSourceAnalytics(ExternalSourceSimple item)
        {
            //Increment page Count
            //Check if item is there if not add a new one then increment count and save
            var analytic = _repoAnalytics.FindByCondition(x =>
                x.ExternalSource != null && !string.IsNullOrEmpty(x.ExternalSource.SourceId) && !string.IsNullOrEmpty(x.ExternalSource.ID) &&
                x.ExternalSource.ID == item.ID && x.ExternalSource.SourceId == item.SourceId).FirstOrDefault();
            if (analytic == null)
            {
                analytic = new MarketplaceItemAnalytics() { ExternalSource = item, MoreInfoCount = 1 };
                await _repoAnalytics.AddAsync(analytic);
            }
            else
            {
                analytic.MoreInfoCount += 1;
                await _repoAnalytics.UpdateAsync(analytic);
            }
        }

        private async Task<MarketplaceItemModel> MapToModelExternalSourceItem(ExternalSourceSimple item)
        {
            //get the source
            var src = _dalExternalSource.GetById(item.SourceId);
            if (src == null)
            {
                throw new ArgumentException($"External source not found: SourceId: {item.SourceId}, Code:{item.Code}");
            }

            //now get the source data 
            var dalSource = await _sourceFactory.InitializeSource(src);
            var result = await dalSource.GetById(item.ID);
            if (result == null)
            {
                throw new ArgumentException($"External source item not found: Id: {item.ID}, SourceId: {item.SourceId}, Code:{item.Code}");
            }
            return result;
        }
    }
}