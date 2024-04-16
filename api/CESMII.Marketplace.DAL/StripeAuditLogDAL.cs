using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CESMII.Marketplace.DAL
{
    public class StripeAuditLogDAL : BaseDAL<StripeAuditLog, StripeAuditLogModel>, IDal<StripeAuditLog, StripeAuditLogModel>
    {
        public StripeAuditLogDAL(IMongoRepository<StripeAuditLog> repo) : base(repo)
        {
        }

        public async Task<string> Add(StripeAuditLogModel model, string userId)
        {
            var entity = new StripeAuditLog
            {
                ID = ""
            };

            this.MapToEntity(ref entity, model);
            entity.Created = DateTime.UtcNow.Date;
            if (!string.IsNullOrEmpty(userId))
            {
                entity.CreatedById = MongoDB.Bson.ObjectId.Parse(userId);
            }

            // Return id for newly added user
            return await _repo.AddAsync(entity);
        }

        public async Task<int> Update(StripeAuditLogModel model, string userId)
        {
            StripeAuditLog entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
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
        public override StripeAuditLogModel GetById(string id)
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
        public override List<StripeAuditLogModel> GetAll(bool verbose = false)
        {
            DALResult<StripeAuditLogModel> result = GetAllPaged();
            return result.Data;
        }

        public override async Task<int> Delete(string id, string userId)
        {
            //var entity = _repo.FindByCondition(x => x.ID == id).FirstOrDefault();
            //entity.IsActive = false;

            //await _repo.UpdateAsync(entity);
            return 1;
        }

        protected override StripeAuditLogModel MapToModel(StripeAuditLog entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new StripeAuditLogModel
                {
                    ID = entity.ID,
                    Type = entity.Type,
                    AdditionalInfo = entity.AdditionalInfo,
                    Created= entity.Created,
                    //CreatedBy = entity.CreatedById,
                    Message= entity.Message,
                };

                return result;
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref StripeAuditLog entity, StripeAuditLogModel model)
        {
            entity.ID = model.ID;
            entity.Type = model.Type;
            entity.AdditionalInfo = model.AdditionalInfo;
            entity.Created = model.Created;
            entity.Message = model.Message;
        }
    }
}
