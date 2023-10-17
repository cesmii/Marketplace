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
        /// This is only populated if this request info is associated with a particular external source item
        /// </summary>
        public Data.Entities.ExternalSourceSimple ExternalSource { get; set; }

        /// <summary>
        /// This is only populated if the external source data is populated. 
        /// We keep it separate from ExternalSource because it is only populated under certain
        /// scenarios and it is a more time consuming call to call external API to get item data. 
        /// </summary>
        public MarketplaceItemModel ExternalItem { get; set; }

        public string RequestTypeCode { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string CompanyName { get; set; }

        public string CompanyUrl { get; set; }

        public  LookupItemModel RequestType { get; set; }

        public string Description { get; set; }

        public string Email { get; set; }

        /// <summary>
        /// Encrypted email. Obfuscate name so it is less obvious. 
        /// </summary>
        public string Uid { get; set; }

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

        public string ccName1 { get; set; }
        public string ccEmail1 { get; set; }
        public string ccName2 { get; set; }
        public string ccEmail2 { get; set; }
    }
}