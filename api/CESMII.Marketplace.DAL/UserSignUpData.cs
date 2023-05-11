using CESMII.Common.SelfServiceSignUp.Models;

namespace CESMII.Marketplace.DAL.Models
{
    /// <summary>
    /// Marketplace-Specific handler for Self-Service Sign Up.
    /// </summary>
    public class UserSignUpData : IUserSignUpData
    {
        private readonly UserDAL _dalUser;

        public UserSignUpData(UserDAL dal)
        {
            _dalUser = dal;
        }

        /// <summary>
        /// Search for user by email address.
        /// </summary>
        /// <param name="strEmail"></param>
        /// <returns></returns>
        public int Where(string strEmail)
        {
            var mylist = _dalUser.Where(x => x.Email.ToLower().Equals(strEmail.ToLower()), null).Data;
            return mylist.Count;
        }

        /// <summary>
        /// Add user to database.
        /// </summary>
        /// <param name="usersum"></param>
        public async void AddUser(UserSignUpModel usersum)
        {
            UserModel um = new UserModel()
            {
                DisplayName = usersum.DisplayName,
                Email = usersum.Email,
                SelfServiceSignUp_Organization_Name = usersum.Organization,
                SelfServiceSignUp_IsCesmiiMember = usersum.IsCesmiiMember
            };

            await _dalUser.Add(um);
        }
    }
}
