using System;

namespace CESMII.Marketplace.DAL.Models
{
    public class RequestInfoModel : AbstractMarketplaceModel
    {
        public string MarketplaceItemId { get; set; }

        /// <summary>
        /// This is only populated if this request info is associated with a particular marketplace item
        /// Only map on get
        /// </summary>
        public MarketplaceItemModel MarketplaceItem { get; set; }

        /// <summary>
        /// This is only populated if this request info is associated with a particular publisher
        /// Only map on get
        /// </summary>
        public PublisherModel Publisher { get; set; }

        public string PublisherId { get; set; }

        /// <summary>
        /// This is only populated if this request info is associated with a particular SM Profile
        /// Only map on get
        /// </summary>
        public MarketplaceItemModel SmProfile { get; set; }
        public long? SmProfileId { get; set; }

        public string RequestTypeCode { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string CompanyName { get; set; }

        public string CompanyUrl { get; set; }

        public  LookupItemModel RequestType { get; set; }

        public string Description { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Industries { get; set; }

        /// <summary>
        /// </summary>
        public LookupItemModel MembershipStatus { get; set; }

        /// <summary>
        /// Notes for admin to add additional commentary to request info item.
        /// </summary>
        public string Notes{ get; set; }

        /// <summary>
        /// Update status of inquiry from admin side to keep track of 
        /// open items.
        /// </summary>
        public LookupItemModel Status { get; set; }
    }





}