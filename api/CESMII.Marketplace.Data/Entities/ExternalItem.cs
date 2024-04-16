namespace CESMII.Marketplace.Data.Entities
{
    using System.Collections.Generic;

    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// External Item is a small subset of data related to External items from external sources such as the CloudLib. 
    /// The sole purpose of this collection is to establish a mechanism to manually establish/relate external items
    /// to other marketplace items or other external items such as sm profiles in the CloudLib. 
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ExternalItem : MarketplaceAbstractEntity
    {
        /// <summary>
        /// Refers to an external item associated with this request info item
        /// </summary>
        public ExternalSourceSimple ExternalSource { get; set; }

        /// <summary>
        /// Represents marketplace items that are marked as related (required, recommended, similar)
        /// </summary>
        public List<RelatedItem> RelatedItems { get; set; }

        /// <summary>
        /// Represents SM profiles items that are marked as related (required, recommended, similar)
        /// This only uses the CloudLibrary id to associate the items together. 
        /// </summary>
        public List<RelatedExternalItem> RelatedItemsExternal { get; set; }
    }
}