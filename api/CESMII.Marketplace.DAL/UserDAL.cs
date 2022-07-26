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
        protected List<Permission> _permissions;
        protected readonly ConfigUtil _configUtil;

        public UserDAL(IMongoRepository<User> repo,
            IMongoRepository<Organization> repoOrganization,
            IMongoRepository<Permission> repoPermission,
            ConfigUtil configUtil) : base(repo)
        {
            _configUtil = configUtil;
            //when mapping the results, we also get related data. For efficiency, get the orgs and permissions now 
            _organizations = repoOrganization.GetAll();
            _permissions = repoPermission.GetAll();
        }

        /// <summary>
        /// The user Add flow works differently than other adds. After adding the record here, the calling code will also 
        /// generate a token to send to user so they can complete registration. 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        //add this layer so we can instantiate the new entity here.
        public async Task<string> Add(UserModel model, string userId)
        {
            //generate random password and then encrypt in here. 
            var password = PasswordUtils.GenerateRandomPassword(_configUtil.PasswordConfigSettings.RandomPasswordLength);

            var entity = new User
            {
                ID = ""
                ,Created = DateTime.UtcNow
                //not actually used during registration but keep it here because db expects non null.
                //The user sets their own pw on register complete
                ,Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, password)
            };

            this.MapToEntity(ref entity, model);
            //do this after mapping to enforce isactive is true on add
            entity.IsActive = true;

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // Return id for newly added user
            return entity.ID;
        }

        /// <summary>
        /// Get rule and related data
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public async Task<UserModel> Validate(string userName, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                // Null value is used on initial creation, therefore null may not be passed into this method.
                var ex = new ArgumentNullException(password, "Password required.");
                _logger.Error(ex); // Log this within all targets as an error.
                throw ex; // Throw an explicit exception, those using this should be aware this cannot be allowed.
            }

            //1. Validate against our encryption. Because we use the existing user's settings, we get the 
            // existing pw, parse it into parts and encrypt the new pw with the same settings to see if it matches
            var result = _repo.FindByCondition(u => u.UserName.ToLower() == userName.ToLower() && u.IsActive && u.RegistrationComplete.HasValue)
                .FirstOrDefault();
            if (result == null) return null;

            //test against our encryption, means we match 
            bool updateEncryptionLevel = false;
            if (PasswordUtils.ValidatePassword(_configUtil.PasswordConfigSettings.EncryptionSettings, result.Password, password, out updateEncryptionLevel))
            {
                //if the encryption level has been upgraded since original encryption, upgrade their pw now. 
                if (updateEncryptionLevel)
                {
                    result.Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, password);
                }
                result.LastLogin = DateTime.UtcNow;
                await _repo.UpdateAsync(result);
                return this.MapToModel(result);
            }

            // No user match found, username or password incorrect. 
            return null;
        }

        /// <summary>
        /// Complete user registration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public async Task<UserModel> CompleteRegistration(int id, string userName, string newPassword)
        {
            //get user - match on user id, user name and is active
            var result = _repo.FindByCondition(u => u.ID.Equals(id) && u.UserName.ToLower().Equals(userName) && u.IsActive) 
                .FirstOrDefault();
            if (result == null) return null;

            //only allow completing registration if NOT already completed
            if (result.RegistrationComplete.HasValue)
            {
                // Null value is used on initial creation, therefore null may not be passed into this method.
                var ex = new InvalidOperationException("User has already completed registration.");
                _logger.Error(ex); // Log this within all targets as an error.
                throw ex; // Throw an explicit exception, those using this should be aware this cannot be allowed.
            }

            //encrypt and save password w/ profile
            result.Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, newPassword);
            result.LastLogin = DateTime.UtcNow;
            result.RegistrationComplete = DateTime.UtcNow;
            await _repo.UpdateAsync(result);
            return this.MapToModel(result);
        }

        /// <summary>
        /// Complete user registration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public async Task<UserModel> ResetPassword(int id, string userName, string newPassword)
        {
            //get user - match on user id, user name and is active
            var result = _repo.FindByCondition(u => u.ID.Equals(id) && u.UserName.ToLower().Equals(userName) && u.IsActive)
                .FirstOrDefault();
            if (result == null) return null;

            //encrypt and save password w/ profile
            result.Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, newPassword);
            result.LastLogin = DateTime.UtcNow;
            await _repo.UpdateAsync(result);
            return this.MapToModel(result);
        }
        
        /// <summary>
        /// Update the user's pasword
        /// </summary>
        /// <remarks>
        /// This assumes the controller does all the proper validation and passes a non-encrypted value
        /// </remarks>
        /// <param name="user"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public async Task<bool> ChangePassword(string id, string oldPassword, string newPassword)
        {
            var existingUser = _repo.GetByID(id);
            if (existingUser == null || !existingUser.IsActive) throw new ArgumentNullException($"User not found with id {id}");

            //validate existing password
            var match = await this.Validate(existingUser.UserName, oldPassword);
            if (match == null)
            {
                return false;
            }

            //Encrypt new password 
            existingUser.Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, newPassword);
            //save changes
            await _repo.UpdateAsync(existingUser);
            
            return true;
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

        public override List<UserModel> GetAll(bool verbose = false)
        {
            var result = _repo.GetAll()
                //.Where(u => u.IsActive)  //TBD - ok to return inactive in the list of users?
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.UserName)
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
            var data = _repo.FindByCondition(
                x => x.IsActive,  //is active is a soft delete indicator. IsActive == false means deleted so we filter out those.
                skip, take,
                x => x.LastName, x => x.FirstName);
            var count = returnCount ? _repo.Count(x => x.IsActive) : 0;

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
                x => x.LastName, x => x.FirstName);
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
            //perform a soft delete by setting active to false
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);

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
            if (existingUser == null || !existingUser.IsActive) throw new ArgumentNullException($"User not found with id {id}");

            //validate existing password
            bool isValidExisting = await this.ValidateSmipPassword(existingUser.ID, existingUser.SmipSettings?.UserName, oldPassword);
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

        private async Task<bool> ValidateSmipPassword(string id, string userName, string password)
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
                u.SmipSettings?.UserName.ToLower() == userName.ToLower() &&
                u.IsActive &&
                u.RegistrationComplete.HasValue)
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
                    Email = entity.Email,
                    UserName = entity.UserName,
                    PermissionNames = entity.Permissions == null ? new List<string>() : MapToModelPermissionNames(entity.Permissions),
                    PermissionIds = entity.Permissions == null ? new List<string>() : MapToModelPermissionIds(entity.Permissions),
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    Organization = entity.OrganizationId == null ? null : MapToModelOrganization(entity.OrganizationId.ToString()),
                    Created = entity.Created,
                    LastLogin = entity.LastLogin,
                    IsActive = entity.IsActive,
                    RegistrationComplete = entity.RegistrationComplete,
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

        protected List<string> MapToModelPermissionNames(List<MongoDB.Bson.BsonObjectId> items)
        {
            var trimmedList = _permissions.Where(x => items.Select(x => x.ToString()).ToList().Contains(x.ID)).ToList();
            return trimmedList.Select(x => x.CodeName.ToString()).ToList();
        }
        protected List<string> MapToModelPermissionIds(List<MongoDB.Bson.BsonObjectId> items)
        {
            var trimmedList = _permissions.Where(x => items.Select(x => x.ToString()).ToList().Contains(x.ID)).ToList();
            return trimmedList.Select(x => x.ID).ToList();
        }

        protected override void MapToEntity(ref User entity, UserModel model)
        {
            entity.UserName = model.UserName;
            entity.Email = model.Email;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.IsActive = model.IsActive;
            entity.OrganizationId = model.Organization == null ?
                MongoDB.Bson.ObjectId.Parse(Constants.BSON_OBJECTID_EMPTY) :
                MongoDB.Bson.ObjectId.Parse(model.Organization.ID);

            //handle update of user permissions
            entity.Permissions = model.PermissionIds.Select(x => new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(x))).ToList();

            //update smip settings - except SMIP password - copy it into mode first to essentially maintain existing value
            model.SmipSettings.Password = entity.SmipSettings.Password;
            //now all settings will be mapped to entity and password is preserved
            entity.SmipSettings = model.SmipSettings;
        }

    }
}