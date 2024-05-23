using MongoDB.Bson;
using System;

namespace CESMII.Marketplace.Data.Entities
{
    public class StripeAuditLog : AbstractEntity
    {
        public string Type { get; set; }

        public string Message { get; set; }

        public string AdditionalInfo { get; set; }

        public string Data { get; set; }
        public string Session { get; set; }


        public BsonObjectId CreatedById { get; set; }

        public DateTime Created { get; set; }
    }
}
