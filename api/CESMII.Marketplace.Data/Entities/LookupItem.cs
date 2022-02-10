namespace CESMII.Marketplace.Data.Entities
{
    using MongoDB.Bson.Serialization.Attributes;
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
   
    [BsonIgnoreExtraElements]
    public class LookupItem : AbstractEntity
    {
       
        public string Name { get; set; }

        
        public string Code { get; set; }


        public virtual LookupType LookupType { get; set; }

        
        public int DisplayOrder { get; set; }

     
        public bool IsActive { get; set; }
    }

  
}