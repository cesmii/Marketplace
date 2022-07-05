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
    public class JobLog : MarketplaceAbstractEntity
    {
        public string Name { get; set; }

        public int StatusId { get; set; }

        public DateTime? Completed { get; set; }

        public virtual List<JobLogMessage> Messages { get; set; }

        /// <summary>
        /// Field to store response data returned by a specific job execution 
        /// </summary>
        /// <remarks>This will be encrypted to protect any sensitive data that may be returned.</remarks>
        public string ResponseData { get; set; }
    }

    public class JobLogMessage
    {
        public string Message { get; set; }

        public DateTime Created { get; set; }
    }
}
