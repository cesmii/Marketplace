using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace MP_TestGenerator.Entities
{
    using System.Collections.Generic;

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

    public class SocialMediaLink
    {
        public string Icon { get; set; }

        public string Css { get; set; }
        public string Url { get; set; }
    }
}
