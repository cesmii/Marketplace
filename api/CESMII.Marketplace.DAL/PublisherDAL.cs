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

    public class PublisherDAL : BaseDAL<Publisher, PublisherModel>, IDal<Publisher, PublisherModel>
    {

        protected IMongoRepository<LookupItem> _repoLookup;
        protected List<LookupItem> _lookupItemsAll;
        protected IMongoRepository<MarketplaceItem> _repoMarketplaceItems;
        protected List<MarketplaceItem> _marketplaceItemsAll;

        public PublisherDAL(IMongoRepository<Publisher> repo, IMongoRepository<LookupItem> repoLookup, 
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
        public override PublisherModel GetById(string id)
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

        public Task<string> Add(PublisherModel model, string userId)
        {
            throw new NotSupportedException("For adding publisher items, use AdminPublisherDAL");
        }

        public Task<int> Update(PublisherModel model, string userId)
        {
            throw new NotSupportedException("For saving publisher items, use AdminPublisherDAL");
        }

        public override Task<int> Delete(string id, string userId)
        {
            throw new NotSupportedException("For deleting publisher items, use AdminPublisherDAL");
        }
        /// <summary>
        /// Get all lookup items (no paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<PublisherModel> GetAll(bool verbose = false)
        {
            DALResult<PublisherModel> result = GetAllPaged(verbose: verbose);
            return result.Data;
        }
        public override DALResult<PublisherModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                x => x.Verified,  
                skip, take,
                l =>  l.Name);  
            var count = returnCount ? _repo.Count(x => x.Verified) : 0;

            //map the data to the final result
            var result = new DALResult<PublisherModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        public override DALResult<PublisherModel> Where(Func<Publisher, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                x => x.Name);
            var count = returnCount ? _repo.Count(predicate) : 0;

            _lookupItemsAll = _repoLookup.GetAll();

            //map the data to the final result
            var result = new DALResult<PublisherModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;

        }

        protected override PublisherModel MapToModel(Publisher entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new PublisherModel
                {
                    ID = entity.ID,
                    //ensure this value is always without spaces and is lowercase. 
                    Name = entity.Name.ToLower().Trim().Replace(" ", "-").Replace("_", "-"),
                    DisplayName = entity.DisplayName,
                    Verified = entity.Verified,
                    Description = entity.Description,
                    CompanyUrl = entity.CompanyUrl,
                    SocialMediaLinks = entity.SocialMediaLinks?.Select(x => new SocialMediaLinkModel() { Css = x.Css, Icon = x.Icon, Url = x.Url }).ToList(),
                    IsActive = entity.IsActive
                };
                if (verbose)
                {
                    result.Categories = MapToModelLookupItems(entity.Categories, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.Process)).ToList());
                    result.IndustryVerticals = MapToModelLookupItems(entity.IndustryVerticals, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.IndustryVertical)).ToList());
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
                IsFeatured = x.IsFeatured,
                IsVerified = x.IsVerified,
                Abstract = x.Abstract,
                Description = x.Description,
                Type = MapToModelLookupItem(x.ItemTypeId, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.SmItemType)).ToList()),
                AuthorId = x.AuthorId,
                Created = x.Created,
                PublishDate = x.PublishDate,
                MetaTags = x.MetaTags,
                Categories = MapToModelLookupItems(x.Categories, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.Process)).ToList()),
                IndustryVerticals = MapToModelLookupItems(x.IndustryVerticals, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.IndustryVertical)).ToList()),
                Status = MapToModelLookupItem(x.StatusId, _lookupItemsAll.Where(x => x.LookupType.EnumValue.Equals(LookupTypeEnum.MarketplaceStatus)).ToList()),
                IsActive = x.IsActive
            }).ToList();
            return result;
        }

    }


}