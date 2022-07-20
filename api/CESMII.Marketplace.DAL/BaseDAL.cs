namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    using NLog;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using System.Threading.Tasks;

    public abstract class BaseDAL<TEntity, TModel> where TEntity : AbstractEntity where TModel : AbstractModel
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        protected readonly IMongoRepository<TEntity> _repo;

        public BaseDAL(IMongoRepository<TEntity> repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Get item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual TModel GetById(string id)
        {
            var entity = _repo.FindByCondition(u => u.ID == id).FirstOrDefault();
            return MapToModel(entity);
        }

        public virtual List<TModel> GetAll(bool verbose = false)
        {
            var result = _repo.GetAll().ToList();
            return MapToModels(result, verbose);
        }

        public virtual DALResult<TModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            throw new NotImplementedException("TBD - Under Construction. Make this work for Mongo DB. Perhaps pass in the paging to the repo.");
        }

        public virtual DALResult<TModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false,
            params OrderByExpression<TEntity>[] orderByExpressions)
        {
            throw new NotImplementedException("TBD - Implement in derived class.");
        }

        public virtual DALResult<TModel> Where(Func<TEntity, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            throw new NotImplementedException("TBD - Implement in derived class.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicates"></param>
        /// <returns></returns>
        public virtual DALResult<TModel> Where(List<Func<TEntity, bool>> predicates, int? skip, int? take, bool returnCount = false, bool verbose = false,
            params OrderByExpression<TEntity>[] orderByExpressions)
        {
            throw new NotImplementedException("TBD - Implement in derived class.");
        }

        public virtual DALResult<TModel> Where(Func<TEntity, bool> predicate, int? skip, int? take, bool returnCount = false, bool verbose = false,
            params OrderByExpression<TEntity>[] orderByExpressions)
        {
            throw new NotImplementedException("TBD - Implement in derived class.");
        }

        public virtual long Count(List<Func<TEntity, bool>> predicates)
        {
            return _repo.Count(predicates);
        }

        public virtual long Count(Func<TEntity, bool> predicate)
        {
            return _repo.Count(predicate);
        }

        public virtual long Count()
        {
            return _repo.Count();
        }

        protected virtual TEntity CheckForExisting(TModel model)
        {
            throw new NotImplementedException();
        }
        public virtual Task<int> Delete(string id, string userId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        /// <remarks>Verbose is intended to map more of the related data. Each DAL 
        /// can determine how much is enough</remarks>
        protected virtual TModel MapToModel(TEntity entity, bool verbose = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        /// <remarks>Verbose is intended to map more of the related data. Each DAL 
        /// can determine how much is enough. Other DALs may choose to not use and keep the 
        /// mapping the same between getById and GetAll/Where calls.</remarks>
        protected virtual List<TModel> MapToModels(List<TEntity> entities, bool verbose = false)
        {
            var result = new List<TModel>();

            foreach (var item in entities)
            {
                result.Add(MapToModel(item, verbose));
            }
            return result;
        }

        protected virtual void MapToEntity(ref TEntity entity, TModel model)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Convert an id list of items into a look up item list. This trims out the lookup items not in related items.
        /// </summary>
        /// <param name="relatedItems"></param>
        /// <param name="allItems"></param>
        /// <returns></returns>
        protected List<LookupItemModel> MapToModelLookupItems(List<MongoDB.Bson.BsonObjectId> relatedItems, List<LookupItem> allItems)
        {

            var trimmedList = allItems.Where(x => relatedItems.Select(x => x.ToString()).ToList().Contains(x.ID)).ToList();
            var result = trimmedList.Select(x => new LookupItemModel
            {
                ID = x.ID,
                Code = x.Code,
                DisplayOrder = x.DisplayOrder,
                LookupType = new LookupTypeModel() { EnumValue = x.LookupType.EnumValue, Name = x.Name },
                Name = x.Name
            }).ToList();
            return result;
        }

        /// <summary>
        /// Take a list of all lookup items of a particular type and a list of selected items. 
        /// Map the all lookup items into a selected lookup items model and mark specific items selected that exist in the selected list. 
        /// The idea is we return the entire lookup list and mark selected those items appearing selected. 
        /// </summary>
        /// <param name="selectedItems"></param>
        /// <param name="allItems"></param>
        /// <returns></returns>
        public List<LookupItemFilterModel> MapToModelLookupItemsSelectable(List<MongoDB.Bson.BsonObjectId> selectedItems, List<LookupItem> allItems)
        {
            var result = allItems
                .OrderBy(x =>x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Select(x => new LookupItemFilterModel
            {
                ID = x.ID,
                Code = x.Code,
                DisplayOrder = x.DisplayOrder,
                LookupType = new LookupTypeModel() { EnumValue = x.LookupType.EnumValue, Name = x.Name },
                Name = x.Name,
                Selected = selectedItems.Find(y => y.ToString().Equals(x.ID.ToString())) != null
            }).ToList();
            return result;
        }

        protected LookupItemModel MapToModelLookupItem(MongoDB.Bson.BsonObjectId lookupId, List<LookupItem> allItems)
        {
            if (lookupId == null) return null;
            
            var match = allItems.FirstOrDefault(x => x.ID == lookupId.ToString());
            if (match == null) return null;
            return new LookupItemModel()
            {
                ID = match.ID,
                Code = match.Code,
                DisplayOrder = match.DisplayOrder,
                LookupType = new LookupTypeModel() { EnumValue = match.LookupType.EnumValue, Name = match.Name },
                Name = match.Name
            };
        }

        protected PublisherModel MapToModelPublisher(MongoDB.Bson.BsonObjectId publisherId, List<Publisher> allItems)
        {

            var pubItem = allItems.FirstOrDefault(x => x.ID == publisherId.ToString());
            return new PublisherModel()
            {
                ID = pubItem.ID,
                Verified = pubItem.Verified,
                Description = pubItem.Description,
                CompanyUrl = pubItem.CompanyUrl,
                Name = pubItem.Name,
                DisplayName = pubItem.DisplayName,
                SocialMediaLinks = pubItem.SocialMediaLinks?.Select(x => new SocialMediaLinkModel() { Css = x.Css, Icon = x.Icon, Url = x.Url }).ToList()
            };

        }

        protected List<ImageItemModel> MapToModelImages(List<ImageItem> items)
        {
            if (items == null) return null;
            var result = new List<ImageItemModel>();
            foreach (var img in items)
            {
                result.Add(MapToModelImage(img));
            }
            return result;
        }

        protected ImageItemModel MapToModelImage(Func<ImageItem, bool> predicate, List<ImageItem> allItems)
        {
            var match = allItems.FirstOrDefault(predicate);
            if (match == null) return null;
            return MapToModelImage(match);
        }

        protected ImageItemModel MapToModelImage(ImageItem entity)
        {
            return new ImageItemModel()
            {
                ID = entity.ID,
                FileName = entity.FileName,
                Type = entity.Type,
                Src = entity.Src,
                MarketplaceItemId = entity.MarketplaceItemId == null || entity.MarketplaceItemId.ToString().Equals(Common.Constants.BSON_OBJECTID_EMPTY) ?
                    null : entity.MarketplaceItemId.ToString()
            };
        }

        protected ImageItemSimpleModel MapToModelImageSimple(Func<ImageItemSimple, bool> predicate, List<ImageItemSimple> allItems)
        {
            var match = allItems.FirstOrDefault(predicate);
            if (match == null) return null;
            return MapToModelImageSimple(match);
        }

        protected ImageItemSimpleModel MapToModelImageSimple(ImageItemSimple entity)
        {
            return new ImageItemSimpleModel()
            {
                ID = entity.ID,
                FileName = entity.FileName,
                Type = entity.Type,
                MarketplaceItemId = entity.MarketplaceItemId == null || entity.MarketplaceItemId.ToString().Equals(Common.Constants.BSON_OBJECTID_EMPTY) ?
                    null : entity.MarketplaceItemId.ToString()
            };
        }

        public virtual void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            _repo.Dispose();
            //set flag so we only run dispose once.
            _disposed = true;
        }

    }

}