namespace CESMII.Marketplace.Data.Entities
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;
    using System.ComponentModel.DataAnnotations.Schema;

    [BsonIgnoreExtraElements]
    [Table("ImageItem")]
    public class ImageItemSimple : AbstractEntity
    {
        private BsonObjectId _marketplaceItemId;
        /// <summary>
        /// Note not required. If null, available for all
        /// </summary>
        public BsonObjectId MarketplaceItemId
        {
            get { return _marketplaceItemId ?? new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(Common.Constants.BSON_OBJECTID_EMPTY)); }

            set
            {
                _marketplaceItemId = value;
            }
        }

        /// <summary>
        /// This is image/*
        /// </summary>
        public string Type { get; set; }
        public string FileName { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ImageItem : ImageItemSimple
    {
        /// <summary>
        /// This can either be a base64 string or a url to point to an external image
        /// </summary>
        /// <remarks>Because this could be large, we break the entity model into two distinct classes. Sometimes I just
        /// want the basic info and don't need the source. In those scenarios, the imageItemSimple above is used. 
        /// I include the table attribute to help map the simple to the same collection in Mongo. 
        /// If no table name is specified, it just uses reflection and this class name to come up with the collection name.
        /// </remarks>
        public string Src { get; set; }
    }
}