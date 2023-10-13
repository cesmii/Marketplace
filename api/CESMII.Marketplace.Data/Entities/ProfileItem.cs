namespace CESMII.Marketplace.Data.Entities
{
    using System.Collections.Generic;

    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// Profile Item is a small subset of data related to SM Profiles from the CloudLib. 
    /// The sole purpose of this collection is to establish a mechanism to manually establish/relate SM Profiles
    /// to other marketplace items or SM profiles in the CloudLib. 
    /// </summary>
    //TBD - rename collection name once we cutover to this.
    [BsonIgnoreExtraElements]
    public class ProfileItem : MarketplaceAbstractEntity
    {
        /// <summary>
        /// Represents the id from the external source system
        /// Unique. Must have no spaces or special characters.
        /// </summary>
        [BsonElement("ProfileId")]  //TBD - rename column name once we cutover to this.
        public string ExternalId { get; set; }

        /// <summary>
        /// Represents marketplace items that are marked as related (required, recommended, similar)
        /// </summary>
        public List<RelatedItem> RelatedItems { get; set; }

        /// <summary>
        /// Represents SM profiles items that are marked as related (required, recommended, similar)
        /// This only uses the CloudLibrary id to associate the items together. 
        /// </summary>
        [BsonElement("RelatedProfiles")]  //TBD - rename column name once we cutover to this.
        public List<RelatedProfileItem> RelatedExternalItems { get; set; }
    }
}