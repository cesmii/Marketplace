namespace CESMII.Marketplace.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Data.Repositories;
    using CESMII.Marketplace.Common;

    /// <summary>
    /// Interact with User Data. 
    /// This is a enhanced version of the typical DAL class in that it has 
    /// additional handling for login validation, password handling, 
    /// handling updates for multiple repos at one time, etc. 
    /// </summary>
    public class UserDAL : BaseDAL<User, UserModel>, IDal<User, UserModel>
    {
        protected List<Organization> _organizations;
        //protected List<Permission> _permissions;
        protected readonly ConfigUtil _configUtil;

        public UserDAL(IMongoRepository<User> repo,
            IMongoRepository<Organization> repoOrganization,
            //IMongoRepository<Permission> repoPermission,
            ConfigUtil configUtil) : base(repo)
        {
            _configUtil = configUtil;
            //when mapping the results, we also get related data. For efficiency, get the orgs and permissions now 
            _organizations = repoOrganization.GetAll();
            //_permissions = repoPermission.GetAll();
        }

        /// <summary>
        /// Add user info when a new user appears that was authenticated to Azure AD
        /// Add very limited info: user name
        /// TBD - eventually add organization here.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<string> Add(UserModel model, string userId)
        {
            var entity = new User
            {
                ID = ""
                ,Created = DateTime.UtcNow
            };

            //initialize SMIP settings
            entity.SmipSettings = new SmipSettings();
            if (model.SmipSettings == null)
            {
                model.SmipSettings = new SmipSettings();
            }
            this.MapToEntity(ref entity, model);

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added user
            return entity.ID;
        }

        /// <summary>
        /// Get user
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override UserModel GetById(string id)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get user by user's Azure AAD
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public UserModel GetByIdAAD(string userIdAAD)
        {
            var entity = _repo.FindByCondition(x => x.ObjectIdAAD.ToLower().Equals(userIdAAD))
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        public override List<UserModel> GetAll(bool verbose = false)
        {
            var result = _repo.GetAll()
                .OrderBy(u => u.ObjectIdAAD)
                .ToList();
            return MapToModels(result, verbose);
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<UserModel> GetAllPaged(int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.GetAll(
                skip, take,
                x => x.ObjectIdAAD);
            var count = returnCount ? _repo.Count() : 0;

            //map the data to the final result
            var result = new DALResult<UserModel>
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
        public override DALResult<UserModel> Where(Func<User, bool> predicate, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var data = _repo.FindByCondition(
                predicate,
                skip, take,
                x => x.ObjectIdAAD);
            var count = returnCount ? _repo.Count(predicate) : 0;

            //map the data to the final result
            var result = new DALResult<UserModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;

        }

        public async Task<int> Update(UserModel item, string userId)
        {
            //TBD - if userId is not same as item.id, then check permissions of userId before updating
            var entity = _repo.FindByCondition(x => x.ID == item.ID)
                .FirstOrDefault();
            this.MapToEntity(ref entity, item);

            await _repo.UpdateAsync(entity);

            return 1;
        }

        public override async Task<int> Delete(string id, string userId)
        {
            //remove use from our local DB. Note actual Azure AD account not impacted by this.
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();

            await _repo.Delete(entity);

            return 1;
        }

        #region SMIP Settings Methods
        /// <summary>
        /// Update the user's Smip Settings pasword
        /// </summary>
        /// <remarks>
        /// This assumes the controller does all the proper validation and passes a non-encrypted value
        /// </remarks>
        /// <returns></returns>
        public async Task<bool> ChangeSmipPassword(string id, string oldPassword, string newPassword)
        {
            var existingUser = _repo.GetByID(id);
            if (existingUser == null) throw new ArgumentNullException($"User not found with id {id}");

            //validate existing password
            bool isValidExisting = this.ValidateSmipPassword(existingUser.ID, existingUser.SmipSettings?.UserName, oldPassword);
            if (!isValidExisting)
            {
                _logger.Log(NLog.LogLevel.Warn, $"Change SMIP Password - Old Password does not match for user {id}");
                return false;
            }

            //Encrypt new password 
            existingUser.SmipSettings.Password = PasswordUtils.EncryptString(newPassword, 
                _configUtil.PasswordConfigSettings.EncryptionSettings.EncryptDecryptKey); 

            //save changes
            await _repo.UpdateAsync(existingUser);
            return true;
        }

        private bool ValidateSmipPassword(string id, string userName, string password)
        {
            //check user name and password match for this user
            if (string.IsNullOrEmpty(password))
            {
                // Null value is used on initial creation, therefore null may not be passed into this method.
                var ex = new ArgumentNullException(password, "Password required.");
                _logger.Error(ex); // Log this within all targets as an error.
                throw ex; // Throw an explicit exception, those using this should be aware this cannot be allowed.
            }

            //1. Validate against our encryption. Because we use the existing user's settings, we get the 
            // existing pw, parse it into parts and encrypt the new pw with the same settings to see if it matches
            var match = _repo.FindByCondition(u =>
                u.ID.Equals(id) &&
                u.SmipSettings?.UserName.ToLower() == userName.ToLower())
                .FirstOrDefault();
            if (match == null) return false;

            //SMIP password needs encrypt/decrypt ability so we use different technique than the account passwords
            //test against our encryption, means we match 
            var passwordDecrypted = PasswordUtils.DecryptString(match.SmipSettings.Password, _configUtil.PasswordConfigSettings.EncryptionSettings.EncryptDecryptKey);
            return password.Equals(passwordDecrypted);
        }
        #endregion

        protected override UserModel MapToModel(User entity, bool verbose = false)
        {
            if (entity != null)
            {
                return new UserModel
                {
                    ID = entity.ID,
                    ObjectIdAAD = entity.ObjectIdAAD,
                    Organization = entity.OrganizationId == null ? null : MapToModelOrganization(entity.OrganizationId.ToString()),
                    LastLogin = entity.LastLogin,
                    Created = entity.Created,
                    SmipSettings = entity.SmipSettings
                };
            }
            else
            {
                return null;
            }

        }

        protected OrganizationModel MapToModelOrganization(string id)
        {
            var org = _organizations.FirstOrDefault(x => x.ID.Equals(id));
            if (org == null) return null;
            return new OrganizationModel() { 
                ID = id,
                Name = org.Name
            };
        }

        protected override void MapToEntity(ref User entity, UserModel model)
        {
            entity.ObjectIdAAD = model.ObjectIdAAD;
            entity.OrganizationId = model.Organization == null ?
                MongoDB.Bson.ObjectId.Parse(Constants.BSON_OBJECTID_EMPTY) :
                MongoDB.Bson.ObjectId.Parse(model.Organization.ID);
            entity.LastLogin = model.LastLogin;
            entity.DisplayName = model.DisplayName;

            //update smip settings - except SMIP password - copy it into mode first to essentially maintain existing value
            model.SmipSettings.Password = entity.SmipSettings.Password;
            //now all settings will be mapped to entity and password is preserved
            entity.SmipSettings = model.SmipSettings;
        }

    }
}