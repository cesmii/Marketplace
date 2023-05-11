namespace CESMII.Marketplace.Data.Entities
{
    using System;

    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    [BsonIgnoreExtraElements]
    public class User : AbstractEntity 
    {
        /// <summary>
        /// Object Id value stored in Azure. This should not change during lifetime of Azure AD user account.
        /// </summary>
        public string ObjectIdAAD { get; set; }
        /// <summary>
        /// Display Name from Azure AD. This is a convenience helper to make it eaiser to display friendly
        /// name within our eco system. This will be updated on each login. 
        /// This is not expected to be unique AND is expected it can change. 
        /// </summary>
        public string DisplayName { get; set; }

        [BsonDefaultValue("")]
        public string Email { get; set; }

        public DateTime? Created { get; set; }

        public DateTime? LastLogin { get; set; }

        public BsonObjectId OrganizationId { get; set; }

        public SmipSettings SmipSettings { get; set; }

        public string? SelfServiceSignUp_Organization_Name { get; set; }
        public bool? SelfServiceSignUp_IsCesmiiMember { get; set; }

    }

    /// <summary>
    /// Temporary class to store and use SMIP settings
    /// </summary>
    /// <remarks>Eventually, this will go away in favor of a SSO unified user record</remarks>
    public class SmipSettings
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string GraphQlUrl { get; set; }

        public string Authenticator { get; set; }

        public string AuthenticatorRole { get; set; }
    }

}