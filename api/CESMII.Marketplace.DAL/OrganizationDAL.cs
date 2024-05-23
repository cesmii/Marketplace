namespace CESMII.Marketplace.DAL
{
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class OrganizationDAL : BaseDAL<Organization, OrganizationModel>, IDal<Organization, OrganizationModel>
    {
        public OrganizationDAL(IMongoRepository<Organization> repo) : base(repo)
        {
        }

        /// <summary>
        /// Get item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override OrganizationModel GetById(string id)
        {
            var entity = _repo.FindByCondition(u => u.ID == id).FirstOrDefault();
            return MapToModel(entity);
        }

        /// <summary>
        /// This should be used when getting all items with some filter determined by the calling code.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public List<OrganizationModel> Where(Expression<Func<Organization, bool>> predicate, int? skip = null, int? take = null,
            bool returnCount = false, bool verbose = false)
        {
            var data = _repo.FindByCondition(predicate);

            var retValues = new System.Collections.Generic.List<OrganizationModel>();

            foreach (var item in data)
            {
                retValues.Add(MapToModel(item));
            }

            return retValues;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicates"></param>
        /// <returns></returns>
        public override DALResult<OrganizationModel> Where(List<Func<Organization, bool>> predicates, int? skip, int? take, bool returnCount = false, bool verbose = false,
            params OrderByExpression<Organization>[] orderByExpressions)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
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

            //map the data to the final result
            var result = new DALResult<OrganizationModel>
            {
                Count = count,
                Data = MapToModels(data, verbose),
                SummaryData = null
            };
            _logger.Log(NLog.LogLevel.Warn, $"OrganizationDAL|Where|Duration: {timer.ElapsedMilliseconds}ms.");
            return result;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicates"></param>
        /// <returns></returns>
        public override DALResult<OrganizationModel> Where(Func<Organization, bool> predicate, int? skip, int? take, bool returnCount = false, bool verbose = false,
            params OrderByExpression<Organization>[] orderByExpressions)
        {
            var predicates = new List<Func<Organization, bool>>
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
        public override DALResult<OrganizationModel> Where(Func<Organization, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var query = _repo.FindByCondition(
                predicate,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                new OrderByExpression<Organization>() { Expression = x => x.Name });
            var count = returnCount ? _repo.Count(predicate) : 0;

            //trigger the query to execute then we can limit what related data we query against
            var data = query.ToList();

            //get related data - pass list of item ids and publisher ids. 
            var ids = data.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x.ID))).ToList();
            ////if verbose, then pull in images from relatedItems child collection.
            //if (verbose)
            //{
            //    var relatedItemIds = data.SelectMany(x => x.RelatedItems == null || !x.RelatedItems.Any() ?
            //        new List<MongoDB.Bson.BsonObjectId>() :
            //        x.RelatedItems.Select(y => y.MarketplaceItemId)).ToList();
            //    ids = ids.Union(relatedItemIds).ToList();
            //}

            //map the data to the final result
            var result = new DALResult<OrganizationModel>
            {
                Count = count,
                Data = MapToModels(data, verbose),
                SummaryData = null
            };
            return result;

        }

        public async Task<string> Add(OrganizationModel model, string strInput)
        {
            var entity = new Organization();
            MapToEntity(ref entity, model);

            // This will add and call saveChanges
            await _repo.AddAsync(entity);

            // TODO: Have repo return Id of newly created entity
            return entity.ID;
        }

        public async Task<int> Update(OrganizationModel item, string userId)
        {
            //TBD - if userId is not same as item.id, then check permissions of userId before updating
            var entity = _repo.FindByCondition(x => x.ID == item.ID)
                .FirstOrDefault();
            this.MapToEntity(ref entity, item);

            await _repo.UpdateAsync(entity);

            return 1;
        }

        protected override OrganizationModel MapToModel(Organization entity, bool verbose = false)
        {
            if (entity == null) return null;

            return new OrganizationModel
            {
                ID = entity.ID,
                Name = entity.Name,
                Credits= entity.Credits,
            };
        }
        protected override void MapToEntity(ref Organization entity, OrganizationModel model)
        {
            entity.ID = model.ID;
            entity.Name = model.Name;
            entity.Credits = model.Credits;
        }
    }
}
