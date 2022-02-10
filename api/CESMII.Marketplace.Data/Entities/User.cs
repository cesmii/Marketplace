﻿namespace CESMII.Marketplace.Data.Entities
{
    using System;
    using System.Collections.Generic;

    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    public class User : AbstractEntity 
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        // User can belong to many permissions
        public virtual List<BsonObjectId> Permissions { get; set; }

        public bool IsActive { get; set; }

        public DateTime Created { get; set; }

        public DateTime? LastLogin { get; set; }

        public DateTime? RegistrationComplete { get; set; }

        [BsonIgnore]
        public BsonObjectId OrganizationId { get; set; }

    }

}