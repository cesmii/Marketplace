using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.Marketplace.Data.Entities
{
    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ExternalSource : MarketplaceAbstractEntity
    {
        public string Name { get; set; }
        /// <summary>
        /// Expected to be unique across external sources. Using code rather than id so that
        /// we don't have to maintain a settings file for each environment with code for each source. 
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// This identifies which item type this source is associated with
        /// </summary>
        public BsonObjectId ItemTypeId { get; set; }
        public BsonObjectId PublisherId { get; set; }
        /// <summary>
        /// This is the .NET type to instantiate for this external source DAL. 
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// This is the .NET type to instantiate for this external source admin DAL. 
        /// </summary>
        public string AdminTypeName { get; set; }
        public string BaseUrl { get; set; }

        /// <summary>
        /// Field to store settings and structure unique to this external source
        /// Structure will be understood and usable within the DAL that processes this external source. 
        /// </summary>
        /// <remarks>This will be encrypted to protect any sensitive data that may be returned.</remarks>
        public string Data { get; set; }
        /// <summary>
        /// IsEnabled is a way to turn on or off external source without deleting.
        /// </summary>
        public bool Enabled { get; set; }
        public BsonObjectId DefaultImageIdPortrait { get; set; }
        public BsonObjectId DefaultImageIdBanner { get; set; }
        public BsonObjectId DefaultImageIdLandscape { get; set; }

        /// <summary>
        /// If an exception occurs when calling the external API, should we stop the whole search. 
        /// If false, we log exception but do not fail the search.
        /// </summary>
        /// <remarks>Note this does not apply when trying to get an individual record</remarks>
        public virtual bool FailOnException { get; set; } = false;
    }

    public class ExternalSourceSimple
    {
        /// <summary>
        /// This is the native id of the source item
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// This is the source id which defines the source in our data. 
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string SourceId { get; set; }

        /// <summary>
        /// This is a friendly code value (expected to be unique) and nicer for use in urls. 
        /// </summary>
        public string Code { get; set; }

    }

    /// <summary>
    /// External Source Simple with a little more info
    /// </summary>
    public class ExternalSourceSimpleInfo : ExternalSourceSimple
    {
        /// <summary>
        /// This is the name for descriptive purposes. 
        /// </summary>
        public string Name { get; set; }
    }
}
