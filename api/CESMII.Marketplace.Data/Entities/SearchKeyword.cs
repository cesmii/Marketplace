using MongoDB.Bson.Serialization.Attributes;

namespace CESMII.Marketplace.Data.Entities
{
    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class SearchKeyword: AbstractEntity
    {
        public string Term { get; set; }
        public string Code { get; set; }
    }
}
