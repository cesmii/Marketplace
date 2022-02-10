namespace CESMII.Marketplace.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;


    public abstract class AbstractEntity
    {
        //TBD - revisit making ID a string. 
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ID { get; set; }

    }

    public abstract class MarketplaceAbstractEntity : AbstractEntity
    {
        public BsonObjectId CreatedById { get; set; }

        public DateTime Created { get; set; }

        public BsonObjectId UpdatedById { get; set; }

        public DateTime? Updated { get; set; }

        public virtual bool IsActive { get; set; }

    }

}