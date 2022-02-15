using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CESMII.Marketplace.Data.Entities
{
   
    public class MarketplaceDownloadHistory 
    {

        public DateTime Date { get; set; }

        public BsonObjectId UserId { get; set; }

    }
}