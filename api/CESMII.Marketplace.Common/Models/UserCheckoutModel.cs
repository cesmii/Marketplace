namespace CESMII.Marketplace.Common.Models
{

    /// <summary>
    /// Used by multiple areas. Checkout of a guest user, trial for onTimeEdge job
    /// </summary>
    public class UserCheckoutModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

    }
}
