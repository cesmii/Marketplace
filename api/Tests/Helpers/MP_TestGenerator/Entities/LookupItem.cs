using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP_TestGenerator.Entities
{
    using MongoDB.Bson.Serialization.Attributes;
  
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
