namespace CESMII.Marketplace.DAL
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Common.Enums;

    public class AdminPublisherDAL : BaseDAL<Publisher, AdminPublisherModel>, IDal<Publisher, AdminPublisherModel>
    {

        protected IMongoRepository<LookupItem> _repoLookup;
        protected List<LookupItem> _lookupItemsAll;
        protected IMongoRepository<MarketplaceItem> _repoMarketplaceItems;
        protected List<MarketplaceItem> _marketplaceItemsAll;

        public AdminPublisherDAL(IMongoRepository<Publisher> repo, IMongoRepository<LookupItem> repoLookup, 
            IMongoRepository<MarketplaceItem> repoMarketplaceItems) : base(repo)
        {
            _repoLookup = repoLookup;
            _repoMarketplaceItems = repoMarketplaceItems;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override AdminPublisherModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();

            //get list of categories
            //get list of industry verticals
            //get list of marketplace items for this publisher
            _lookupItemsAll = _repoLookup.GetAll();
            MongoDB.Bson.ObjectId bsonId;
            if (MongoDB.Bson.ObjectId.TryParse(entity.ID, out bsonId))
            {
                _marketplaceItemsAll = _repoMarketplaceItems.FindByCondition(x => x.PublisherId.Equals(bsonId)).ToList();
            }

            return MapToModel(entity, true);
        }

        public async Task<string> Add(AdminPublisherModel model, string userId)
        {
            Publisher entity = new Publisher
            {
                ID = ""
                //,Created = DateTime.UtcNow
                //,CreatedBy = userId
            };

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add
            entity.Verified = true;
            entity.IsActive = true;
            entity.Created = DateTime.UtcNow.Date;
            entity.CreatedById = MongoDB.Bson.ObjectId.Parse(userId);

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added user
            return entity.ID;
        }

        public async Task<int> Update(AdminPublisherModel model, string userId)
        {
            Publisher entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
            this.MapToEntity(ref entity, model);
            entity.Updated = DateTime.UtcNow;
            entity.UpdatedById = MongoDB.Bson.ObjectId.Parse(userId);

            await _repo.UpdateAsync(entity);
            return 1;
        }

        public override async Task<int> Delete(string id, string userId)
        {
            Publisher entity = _repo.FindByCondition(x => x.ID == id).FirstOrDefault();
            entity.Updated = DateTime.UtcNow;
            entity.UpdatedById = MongoDB.Bson.ObjectId.Parse(userId);
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            return 1;
        }
        /// <summary>
        /// Get all lookup items (no paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<AdminPublisherModel> GetAll(bool verbose = false)
        {
            DALResult<AdminPublisherModel> result = GetAllPaged(verbose: verbose);
            return result.Data;
        }
        public override DALResult<AdminPublisherModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                x => x.Verified,  
                skip, take,
                l =>  l.Name);  
            var count = returnCount ? _repo.Count(x => x.Verified) : 0;

            //map the data to the final result
            DALResult<AdminPublisherModel> result = new DALResult<AdminPublisherModel>();
            result.Count = count;
            result.Data = MapToModels(data.ToList(), verbose);
            result.SummaryData = null;
            return result;
        }

        public override DALResult<AdminPublisherModel> Where(Func<Publisher, bool> predicate, int? skip, int? take,
            bool returnCount = true, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                x => x.Name);
            var count = returnCount ? _repo.Count(predicate) : 0;

            //map the data to the final result
            DALResult<AdminPublisherModel> result = new DALResult<AdminPublisherModel>();
            result.Count = count;
            result.Data = MapToModels(data.ToList(), verbose);
            result.SummaryData = null;
            return result;

        }

        protected override AdminPublisherModel MapToModel(Publisher entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new AdminPublisherModel
                {
                    ID = entity.ID,
                    //ensure this value is always without spaces and is lowercase. 
                    Name = entity.Name.ToLower().Trim().Replace(" ", "-").Replace("_", "-"),
                    DisplayName = entity.DisplayName,
                    Verified = entity.Verified,
                    Description = entity.Description,
                    CompanyUrl = entity.CompanyUrl,
                    SocialMediaLinks = entity.SocialMediaLinks == null ? null :
                        entity.SocialMediaLinks.Select(x => new SocialMediaLinkModel() { Css = x.Css, Icon = x.Icon, Url = x.Url }).ToList(),
                    IsActive = entity.IsActive
                };
                if (verbose)
                {
                    result.Categories = MapToModelLookupItemsSelectable(entity.Categories, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.Process)).ToList());
                    result.IndustryVerticals = MapToModelLookupItemsSelectable(entity.IndustryVerticals, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.IndustryVertical)).ToList());
                    result.MarketplaceItems = MapToModelMarketplaceItems();
                }
                return result;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Get a simplified list of markeplace items published by this publisher. 
        /// </summary>
        /// <returns></returns>
        protected List<MarketplaceItemModel> MapToModelMarketplaceItems()
        {
            if (_marketplaceItemsAll == null) return null;

            var result = _marketplaceItemsAll.Select(x => new MarketplaceItemModel
            {
                ID = x.ID,
                //ensure this value is always without spaces and is lowercase. 
                Name = x.Name.ToLower().Trim().Replace(" ", "-").Replace("_", "-"),
                DisplayName = x.DisplayName,
                Abstract = x.Abstract,
                Description = x.Description,
                TypeId = x.TypeId,
                AuthorId = x.AuthorId,
                Created = x.Created,
                PublishDate = x.PublishDate,
                Version = x.Version,
                MetaTags = x.MetaTags,
                Categories = MapToModelLookupItems(x.Categories, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.Process)).ToList()),
                IndustryVerticals = MapToModelLookupItems(x.IndustryVerticals, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.IndustryVertical)).ToList()),
                Status = MapToModelLookupItem(x.StatusId, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.MarketplaceStatus)).ToList()),
                IsActive = x.IsActive
            }).ToList();
            return result;
        }

        protected override void MapToEntity(ref Publisher entity, AdminPublisherModel model)
        {
            //ensure this value is always without spaces and is lowercase. 
            entity.Name = model.Name.ToLower().Trim().Replace(" ", "-").Replace("_", "-");
            entity.DisplayName = model.DisplayName;
            entity.CompanyUrl = model.CompanyUrl;
            entity.Description = model.Description;
            entity.Verified = model.Verified;
            //entity.StatusId = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.Status.ID)); ;
            //entity.MetaTags = model.MetaTags;
            entity.Categories = model.Categories
                .Where(x => x.Selected) //only include selected rows
                .Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList();
            entity.IndustryVerticals = model.IndustryVerticals
                .Where(x => x.Selected) //only include selected rows
                .Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList();
        }
    }


}