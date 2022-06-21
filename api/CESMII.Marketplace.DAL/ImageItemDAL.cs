namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;

    /// <summary>
    /// </summary>
    public class ImageItemDAL : BaseDAL<ImageItem, ImageItemModel>, IDal<ImageItem, ImageItemModel>
    {
       
        protected List<MarketplaceDownloadHistory> _downloadItemsAll;
        public ImageItemDAL(IMongoRepository<ImageItem> repo) : base(repo)
        {
        }

        public async Task<string> Add(ImageItemModel model, string userId)
        {
            var entity = new ImageItem
            {
                ID = ""
            };

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added user
            return entity.ID;
        }

        public async Task<int> Update(ImageItemModel model, string userId)
        {
            ImageItem entity = _repo.FindByCondition(x => x.ID == model.ID ).FirstOrDefault();
            this.MapToEntity(ref entity, model);

            await _repo.UpdateAsync(entity);
            return 1;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override ImageItemModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get all lookup items (no paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<ImageItemModel> GetAll(bool verbose = false)
        {
            DALResult<ImageItemModel> result = GetAllPaged(verbose: verbose);
            return result.Data;
        }

        public override DALResult<ImageItemModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.GetAll(
                    skip, take);  //TBD - add display order to lookup data
            var count = returnCount ? _repo.Count() : 0;

            //map the data to the final result
            var result = new DALResult<ImageItemModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        public override DALResult<ImageItemModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false,
            params OrderByExpression<ImageItem>[] orderByExpressions)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var predicates = new List<Func<ImageItem, bool>>();
            var data = _repo.FindByCondition(
                predicates,
                skip, take,
                orderByExpressions);
            var count = returnCount ? _repo.Count() : 0;

            //map the data to the final result
            var result = new DALResult<ImageItemModel>
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
        public override DALResult<ImageItemModel> Where(Func<ImageItem, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,
                skip, take,
                l => l.FileName);  
            var count = returnCount ? _repo.Count(predicate) : 0;

            //map the data to the final result
            var result = new DALResult<ImageItemModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;

        }

        public override async Task<int> Delete(string id, string userId)
        {
            ImageItem entity = _repo.FindByCondition(x => x.ID == id).FirstOrDefault();

            //TBD - remove assignment of this image that are used on this marketplace item

            await _repo.Delete(entity);
            return 1;
        }


        protected override ImageItemModel MapToModel(ImageItem entity, bool verbose = false)
        {
            if (entity != null)
            {
                return new ImageItemModel
                {
                    ID = entity.ID,
                    MarketplaceItemId = entity.MarketplaceItemId.ToString(),
                    Src = entity.Src,
                    FileName = entity.FileName,
                    Type = entity.Type
                };
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref ImageItem entity, ImageItemModel model)
        {
            entity.MarketplaceItemId = string.IsNullOrEmpty(model.MarketplaceItemId) ?
                                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY)) :
                                new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(model.MarketplaceItemId));
            entity.Src = model.Src;
            entity.FileName = model.FileName;
            entity.Type = model.Type;
        }
    }
}