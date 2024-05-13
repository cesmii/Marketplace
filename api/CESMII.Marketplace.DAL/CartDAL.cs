using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Data.Repositories;

namespace CESMII.Marketplace.DAL
{
    /// <summary>
    /// </summary>
    public class CartDAL : BaseDAL<Cart, CartModel>, IDal<Cart, CartModel>
    {
        protected IMongoRepository<MarketplaceItem> _repoMarketplaceItem;
        protected List<MarketplaceItem> _marketplaceItemAll;
        protected new ILogger<CartDAL> _logger;

        // Get this key from the config
        protected string _apiKey;

        public CartDAL(IMongoRepository<Cart> repo,
            IMongoRepository<MarketplaceItem> repoMarketplaceItem,
            ConfigUtil configUtil, ILogger<CartDAL> logger) : base(repo)
        {
            _repoMarketplaceItem = repoMarketplaceItem;
            _apiKey = configUtil.StripeSettings.SecretKey;
            _logger = logger;
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError($"Missing configuration for Stripe API key: cannot read configuration information.");
                throw new Exception("Configuration value missing.");
            }
        }

        public async Task<string> Add(CartModel model, string userId)
        {
            var entity = new Cart
            {
                ID = null
                , Created = DateTime.UtcNow
                , Updated = DateTime.UtcNow
                , CreatedById = MongoDB.Bson.ObjectId.Parse(userId)
        };

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add
            entity.IsActive = true;

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added item
            return entity.ID;
        }

        public async Task<int> Update(CartModel model, string userId)
        {
            var entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
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
        public override CartModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id && x.IsActive)
                .FirstOrDefault();

            //get related data - pass list of item ids. 
            GetDependentData(entity.Items.Select(x => x.MarketplaceItemId).ToList()).Wait();

            return MapToModel(entity, true);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override List<CartModel> GetAll(bool verbose = false)
        {
            var result = GetAllPaged(verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <returns></returns>
        public override DALResult<CartModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                x => x.IsActive,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                x => x.Name);  
            var count = returnCount ? _repo.Count(x => x.IsActive) : 0;

            //map the data to the final result
            var result = new DALResult<CartModel>
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
        public override DALResult<CartModel> Where(Func<Cart, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,
                skip, take,
                x => x.Name);
            var count = returnCount ? _repo.Count(predicate) : 0;

            //map the data to the final result
            var result = new DALResult<CartModel>
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

        /// <summary>
        /// Map the entities to the models
        /// </summary>
        /// <remarks>Overriding the base class so we can get the related marketplace items as we iterate the collection.</remarks>
        /// <param name="entities"></param>
        /// <param name="verbose"></param>
        /// <returns></returns>
        protected override List<CartModel> MapToModels(List<Cart> entities, bool verbose = false)
        {
            var result = new List<CartModel>();

            foreach (var item in entities)
            {
                //get dependent marketplace items for use in map to model
                GetDependentData(item.Items.Select(x => x.MarketplaceItemId).ToList()).Wait();
                result.Add(MapToModel(item, verbose));
            }
            return result;
        }


        protected override CartModel MapToModel(Cart entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new CartModel
                {
                    ID = entity.ID,
                    Completed = entity.Completed,
                    Name = entity.Name,
                    Created = entity.Created,
                    Updated = entity.Updated,
                    CreatedById = entity.CreatedById.ToString(),
                    UpdatedById = entity.UpdatedById?.ToString(),
                    Status = (Common.Enums.CartStatusEnum)entity.Status,
                    Items = MapToModelCartItems(entity.Items),
                    IsActive = entity.IsActive,
                    SessionId= entity.SessionId,
                    OraganizationId = entity.OraganizationId,
                };

                return result;
            }
            else
            {
                return null;
            }

        }

        protected List<CartItemModel> MapToModelCartItems(List<CartItem> items)
        {
            if (items == null) return null;

            var result = new List<CartItemModel>();
            foreach (var entity in items)
            {
                var mktplItem = _marketplaceItemAll.FirstOrDefault(x => x.ID == entity.MarketplaceItemId.ToString());
                result.Add(new CartItemModel
                {
                    MarketplaceItem = new MarketplaceItemCheckoutModel()
                    {
                        ID = entity.MarketplaceItemId.ToString(),
                        DisplayName = mktplItem?.DisplayName,
                        Name = mktplItem?.Name, 
                        Abstract= mktplItem?.Abstract,
                        PaymentProductId = entity.StripeId
                    }, 
                    Credits = entity.Credits,
                    Quantity = entity.Quantity,
                    SelectedPrice = entity.SelectedPrice
                });
            }
            return result
                .OrderBy(x => x.MarketplaceItem.DisplayName)
                .ThenBy(x => x.MarketplaceItem.Name)
                //.ThenByDescending(x => x.Quantity * (x.Price + x.Credits))
                .ToList();
        }

        protected override void MapToEntity(ref Cart entity, CartModel model)
        {
            entity.Name = model.Name;
            entity.Completed = model.Completed;
            entity.Status = model.Status;
            entity.SessionId= model.SessionId;
            entity.OraganizationId= model.OraganizationId;
            entity.Items = MapToEntityCartItem(model.Items);
        }

        protected List<CartItem> MapToEntityCartItem(List<CartItemModel> items)
        {
            if (items == null) return null;

            var result = new List<CartItem>();
            foreach (var model in items)
            {
                result.Add(new CartItem() { 
                    MarketplaceItemId = MongoDB.Bson.ObjectId.Parse(model.MarketplaceItem.ID),
                    Credits = model.Credits.HasValue ? model.Credits.Value : 0,
                    Quantity = model.Quantity,
                    SelectedPrice = model.SelectedPrice,
                    StripeId = model.MarketplaceItem.PaymentProductId
                } );
            }
            return result;
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