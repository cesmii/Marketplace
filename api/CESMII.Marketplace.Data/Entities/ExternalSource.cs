using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.Marketplace.Data.Entities
{
    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ExternalSource : MarketplaceAbstractEntity
    {
        public string Name { get; set; }

        /// <summary>
        /// This identifies which item type this source is associated with
        /// </summary>
        public BsonObjectId ItemTypeId { get; set; }
        public BsonObjectId PublisherId { get; set; }
        /// <summary>
        /// This is the .NET type to instantiate for this external source. 
        /// </summary>
        public string TypeName { get; set; }
        public string BaseUrl { get; set; }
        public string AccessToken { get; set; }
        /// <summary>
        /// List of 1:many urls that may be used in the calling of the API
        /// </summary>
        public List<ExternalSourceUrls> Urls { get; set; }

        /// <summary>
        /// Field to store settings and structure unique to this external source
        /// Structure will be understood and usable within the DAL that processes this external source. 
        /// </summary>
        /// <remarks>This will be encrypted to protect any sensitive data that may be returned.</remarks>
        public string Data { get; set; }
        /// <summary>
        /// IsEnabled is a way to turn on or off external source without deleting.
        /// </summary>
        public bool Enabled { get; set; }
        public BsonObjectId DefaultImageIdPortrait { get; set; }
        public BsonObjectId DefaultImageIdBanner { get; set; }
        public BsonObjectId DefaultImageIdLandscape { get; set; }
    }

    public class ExternalSourceUrls
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

}
