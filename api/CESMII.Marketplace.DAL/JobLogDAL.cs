namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;

    /// <summary>
    /// </summary>
    public class JobLogDAL : BaseDAL<JobLog, JobLogModel>, IDal<JobLog, JobLogModel>
    {
        public JobLogDAL(IMongoRepository<JobLog> repo) : base(repo)
        {
        }

        public async Task<string> Add(JobLogModel model, string userId)
        {
            var entity = new JobLog
            {
                ID = null
                , Created = DateTime.UtcNow
                , Updated = DateTime.UtcNow
            };
            //set in model because the map to entity will assign val from model.
            model.Status = Common.Enums.TaskStatusEnum.InProgress;

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add
            entity.IsActive = true;
            if (userId != null)
            {
                entity.CreatedById = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(userId));
                entity.UpdatedById = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(userId));
            }

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added item
            return entity.ID;
        }

        public async Task<int> Update(JobLogModel model, string userId)
        {
            var entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
            this.MapToEntity(ref entity, model);
            entity.Updated = DateTime.UtcNow;
            if (userId != null)
            {
                entity.UpdatedById = new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(userId));
            }

            await _repo.UpdateAsync(entity);
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override JobLogModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override List<JobLogModel> GetAll(bool verbose = false)
        {
            var result = GetAllPaged(verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <returns></returns>
        public override DALResult<JobLogModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                x => x.IsActive,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                x => x.Completed, x => x.StatusId);  
            var count = returnCount ? _repo.Count(x => x.IsActive) : 0;

            //map the data to the final result
            var result = new DALResult<JobLogModel>
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
        public override DALResult<JobLogModel> Where(Func<JobLog, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,
                skip, take,
                x => x.Completed, x => x.StatusId);
            var count = returnCount ? _repo.Count(predicate) : 0;

            //map the data to the final result
            var result = new DALResult<JobLogModel>
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

        protected override JobLogModel MapToModel(JobLog entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new JobLogModel
                {
                    ID = entity.ID,
                    Status = (Common.Enums.TaskStatusEnum)entity.StatusId,
                    Completed = entity.Completed,
                    Created = entity.Created,
                    Updated = entity.Updated,
                    IsActive = entity.IsActive,
                    //TBD - decrypt message support
                    Messages = entity.Messages.OrderByDescending(x => x.Created).Select(x => new JobLogMessage()
                    { 
                        Message = x.isEncrypted ? "TBD - decrypt here" : x.Message,
                        Created = x.Created,
                        isEncrypted = x.isEncrypted
                    }).ToList(), 
                    ResponseData = entity.ResponseData
                };

                return result;
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref JobLog entity, JobLogModel model)
        {
            //only update file list, owner, created on add
            entity.StatusId = (int)model.Status;
            if (model.Completed.HasValue)
            {
                entity.Completed = model.Completed;
            }
            entity.Messages = model.Messages?.OrderByDescending(x => x.Created).Select(x => new JobLogMessage()
            {
                Message = x.isEncrypted ? "TBD - encrypt here" : x.Message,
                Created = x.Created,
                isEncrypted = x.isEncrypted
            }).ToList(); 
            entity.ResponseData = model.ResponseData;
        }
    }
}