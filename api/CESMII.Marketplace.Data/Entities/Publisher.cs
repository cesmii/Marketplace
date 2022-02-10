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
    public class Publisher : MarketplaceAbstractEntity
    {
        /// <summary>
        /// Unique Name. Must have no spaces or special characters.
        /// </summary>
        /// <remarks>This will be used to form the name in the url for better SEO.</remarks>
        public string Name { get; set; }

        /// <summary>
        /// Friendly name viewed and seen by the user on all screens.
        /// </summary>
        public string DisplayName { get; set; }

        public bool Verified { get; set; }

        public string Description { get; set; }

        public string CompanyUrl { get; set; }

        public List<SocialMediaLink> SocialMediaLinks { get; set; }

        public List<BsonObjectId> Categories { get; set; }

        public List<BsonObjectId> IndustryVerticals { get; set; }
    }
}
