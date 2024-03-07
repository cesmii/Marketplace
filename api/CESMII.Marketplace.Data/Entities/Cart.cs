using System;
using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using CESMII.Marketplace.Common.Enums;

namespace CESMII.Marketplace.Data.Entities
{
    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Cart : MarketplaceAbstractEntity
    {
        public string Name { get; set; }

        public CartStatusEnum Status { get; set; } = CartStatusEnum.Pending;

        public DateTime? Completed { get; set; }

        public List<CartItem> Items { get; set; }

    }

}
