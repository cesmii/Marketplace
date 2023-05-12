namespace CESMII.Marketplace.DAL
{
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class OrganizationDAL : BaseDAL<Organization, OrganizationModel>, IDal<Organization, OrganizationModel>
    {
        protected IMongoRepository<Organization> _OrgRepo;  // Do I need this? I'm not sure.
        public OrganizationDAL(IMongoRepository<Organization> repo) : base(repo)
        {
            _OrgRepo = repo;
        }

        /// <summary>
        /// Get item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override OrganizationModel GetById(string id)
        {
            var entity = _OrgRepo.FindByCondition(u => u.ID == id).FirstOrDefault();
            return MapToModel(entity);
        }

        /// <summary>
        /// This should be used when getting all items with some filter determined by the calling code.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public new List<OrganizationModel> Where(Func<Organization, bool> predicate, int? skip = null, int? take = null,
            bool returnCount = false, bool verbose = false)
        {
            var data = _OrgRepo.FindByCondition(predicate);

            var retValues = new System.Collections.Generic.List<OrganizationModel>();

            foreach (var item in data)
            {
                retValues.Add(MapToModel(item));
            }

            return retValues;
        }

        public async Task<string> Add(OrganizationModel model, string strInput)
        {
            Organization entity = MapToEntity(model);

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

        public OrganizationModel MapToModel(Organization entity)
        {
            return new OrganizationModel
            {
                ID = entity.ID,
                Name = entity.Name
            };
        }
        public Organization MapToEntity(OrganizationModel model)
        {
            return new Organization
            {
                ID = model.ID,
                Name = model.Name
            };
        }
    }
}
