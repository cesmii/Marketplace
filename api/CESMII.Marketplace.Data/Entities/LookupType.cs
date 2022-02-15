namespace CESMII.Marketplace.Data.Entities
{
    using MongoDB.Bson.Serialization.Attributes;

    using CESMII.Marketplace.Common.Enums;

    [BsonIgnoreExtraElements]
    public class LookupType
    {
        public LookupTypeEnum EnumValue { get; set; }
        //public int EnumValue { get; set; }
        public string Name { get; set; }
    }

}