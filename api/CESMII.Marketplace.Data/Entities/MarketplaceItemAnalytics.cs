namespace CESMII.Marketplace.Data.Entities
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    [BsonIgnoreExtraElements]
    public class MarketplaceItemAnalytics : AbstractEntity
    {

        /// <summary>
        /// Id of marketplace items - This will be null for CloudLib items
        /// </summary>
        public BsonObjectId MarketplaceItemId { get; set; }
        /// <summary>
        /// Refers to an external item associated with this request info item
        /// </summary>
        public ExternalSourceSimple ExternalSource { get; set; }

        public int PageVisitCount { get; set; }
        public int SearchResultCount { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public int MoreInfoCount { get; set; }
        public int ShareCount { get; set; }
        public int DownloadCount { get; set; }
    }
}