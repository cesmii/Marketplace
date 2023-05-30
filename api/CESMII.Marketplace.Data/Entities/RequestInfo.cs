namespace CESMII.Marketplace.Data.Entities
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
   
    [BsonIgnoreExtraElements]
    public class RequestInfo : MarketplaceAbstractEntity
    {
        private BsonObjectId _marketplaceItemId;
        private BsonObjectId _publisherId;
        public BsonObjectId MarketplaceItemId {
            get { return _marketplaceItemId ?? new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY)); }

            set {
                _marketplaceItemId =  value ;
            } 
        }

        public BsonObjectId PublisherId {
            get { return _publisherId ?? new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY)); }

            set
            {
                _publisherId =  value;
            }
        }

        /// <summary>
        /// Simple high level information related to an SM profile from CloudLib
        /// </summary>
        public long? SmProfileId { get; set; }

        public BsonObjectId RequestTypeId { get; set; } //map like statusid for marketplaceitem
       
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string CompanyName { get; set; }

        public string CompanyUrl { get; set; }

        public string Description { get; set; }
        
        public string Email { get; set; }

        public string Phone { get; set; }

        public string Industries { get; set; }

        /// <summary>
        /// </summary>
        public BsonObjectId MembershipStatusId { get; set; }

        /// <summary>
        /// Notes for admin to add additional commentary to request info item.
        /// </summary>
        public string Notes { get; set; }

        public BsonObjectId StatusId { get; set; }
        public string ccName1 { get; set; }
        public string ccEmail1 { get; set; }
        public string ccName2 { get; set; }
        public string ccEmail2 { get; set; }

    }
}