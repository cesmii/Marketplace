namespace CESMII.Marketplace.Common.Models
{

    /// <summary>
    /// Used by multiple areas. Checkout of a user. 
    /// If user is anonymous / guest, then the id will be null 
    /// and organization will just have organization.name.
    /// </summary>
    public class UserCheckoutModel
    {
        public string ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        /// <summary>
        /// Convenience getter. If user is logged in, then both first and last name 
        /// are present in one of the name fields.
        /// </summary>
        public string FullName { get {
                return $"{FirstName} {LastName}".Trim();
            } 
        }
        public OrganizationCheckoutModel Organization { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

    }

    /// <summary>
    /// Simplified version of org for checkout. 
    /// If guest user, ID will be null and credits will be null
    /// </summary>
    public class OrganizationCheckoutModel
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

}
