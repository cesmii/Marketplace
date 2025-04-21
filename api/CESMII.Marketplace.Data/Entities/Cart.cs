using System;
using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.Common.Models;

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

        public string SessionId { get; set; }
        public long? CreditsApplied { get; set; }

        /// <summary>
        /// This is a simplified form of the user which contains basic 
        /// info to use within the checkout process. 
        /// If user is a guest user, user.ID and user.Organization.id will be null
        /// </summary>
        public UserCheckoutModel CheckoutUser { get; set; }

    }

}
