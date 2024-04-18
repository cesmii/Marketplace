namespace CESMII.Marketplace.Data.Entities
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    [BsonIgnoreExtraElements]
    public class Organization : AbstractEntity 
    {
        public string Name { get; set; }

    }

}