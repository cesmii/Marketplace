using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP_TestGenerator.Entities
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
