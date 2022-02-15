namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Most lookup data is contained in this single entity and differntiated by a lookup type. 
    /// </summary>
    public class LookupDAL : BaseDAL<LookupItem, LookupItemModel>, IDal<LookupItem, LookupItemModel>
    {
        public LookupDAL(IMongoRepository<LookupItem> repo) : base(repo)
        {
        }

        public async Task<string> Add(LookupItemModel model, string userId)
        {
            LookupItem entity = new LookupItem
            {
                ID = ""
                //,Created = DateTime.UtcNow
                //,CreatedBy = userId
            };

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add
            entity.IsActive = true;

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added user
            return entity.ID;
        }

        public async Task<int> Update(LookupItemModel model, string userId)
        {
            LookupItem entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
            //model.Updated = DateTime.UtcNow;
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
        public override LookupItemModel GetById(string id)
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
        public override List<LookupItemModel> GetAll(bool verbose = false)
        {
            DALResult<LookupItemModel> result = GetAllPaged(verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<LookupItemModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                x => x.IsActive,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                l => l.LookupType.Name, l => l.DisplayOrder , l => l.Name);  //TBD - add display order to lookup data
            var count = returnCount ? _repo.Count(x => x.IsActive) : 0;

            //map the data to the final result
            DALResult<LookupItemModel> result = new DALResult<LookupItemModel>();
            result.Count = count;
            result.Data = MapToModels(data.ToList(), verbose);
            result.SummaryData = null;
            return result;
        }

        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<LookupItemModel> Where(Func<LookupItem, bool> predicate, int? skip, int? take, 
            bool returnCount = true, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,  
                skip, take,
                l => l.LookupType.Name, l => l.DisplayOrder, l => l.Name);  //TBD - add display order to lookup data
            var count = returnCount ? _repo.Count(predicate) : 0;

            //map the data to the final result
            DALResult<LookupItemModel> result = new DALResult<LookupItemModel>();
            result.Count = count;
            result.Data = MapToModels(data.ToList(), verbose);
            result.SummaryData = null;
            return result;

        }

        public override async Task<int> Delete(string id, string userId)
        {
            LookupItem entity = _repo.FindByCondition(x => x.ID == id).FirstOrDefault();
            //entity.Updated = DateTime.UtcNow;
            //entity.UpdatedBy = userId;
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            return 1;
        }


        protected override LookupItemModel MapToModel(LookupItem entity, bool verbose = false)
        {
            if (entity != null)
            {
                return new LookupItemModel
                {
                    ID = entity.ID,
                    Name = entity.Name,
                    Code = entity.Code,
                    DisplayOrder = entity.DisplayOrder,
                    IsActive = entity.IsActive,
                    LookupType = new LookupTypeModel() { 
                        EnumValue = entity.LookupType.EnumValue, 
                        Name = Common.Utils.EnumUtils.GetEnumDescription(entity.LookupType.EnumValue)
                    }
                };
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref LookupItem entity, LookupItemModel model)
        {
            entity.Name = model.Name;
            entity.Code = model.Code;
            entity.DisplayOrder = model.DisplayOrder;
            entity.LookupType = new LookupType() { 
                EnumValue= model.LookupType.EnumValue, 
                Name = Common.Utils.EnumUtils.GetEnumDescription(model.LookupType.EnumValue) };  
        }
    }
}