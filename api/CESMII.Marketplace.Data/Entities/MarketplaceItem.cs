namespace CESMII.Marketplace.Data.Entities
{
    using System;
    using System.Collections.Generic;

    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class MarketplaceItem : MarketplaceAbstractEntity
    {
        /* TBD - Other properties to add
            Industries - many to many
            Categories - many to many
            LicensingInfo
            Programming Languages
            External Link (link to an external resource with additional details)
            Publish Date
         */
        /// <summary>
        /// Unique Name. Must have no spaces or special characters.
        /// </summary>
        /// <remarks>This will be used to form the name in the url for better SEO.</remarks>
        public string Name { get; set; }

        /// <summary>
        /// Friendly name viewed and seen by the user on all screens.
        /// </summary>
        public string DisplayName { get; set; }

        public string Version { get; set; }

        public string Abstract { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Type of marketplace item: profile, app
        /// </summary>
        public BsonObjectId ItemTypeId { get; set; }

        /// <summary>
        /// This is the status associated with the item in a more general context. ie: Under Review, Approved, Peer Reviewed)
        /// This status controls visibility of content. (ie: Draft, Live, Archive)
        /// </summary>
        public BsonObjectId StatusId { get; set; }

        /// <summary>
        /// TBD - May need to convert this to list.
        /// </summary>
        public int? AuthorId { get; set; }

        /// <summary>
        /// TBD - May need to convert this to list.
        /// </summary>
        /// 
        [BsonIgnore]
        public virtual User Author { get; set; }

        /// <summary>
        /// The author may not be someone within the system. In this case, show as a simple string field.
        /// TBD - May need to convert this to list.
        /// </summary>
        /// <remarks>
        /// This is equivalent to .
        /// </remarks>
        public string ExternalAuthor { get; set; }

        /// <summary>
        /// profile can have many metatags - this is stored and retrieved in JSON format as a list of strings
        /// </summary>
        /// <remarks>
        /// This is equivalent to .
        /// </remarks>
        public List<string> MetaTags { get; set; }

        public List<BsonObjectId> Categories { get; set; }

        public List<BsonObjectId> IndustryVerticals { get; set; }

        public List<RelatedItem> RelatedItems { get; set; }

        public List<RelatedProfileItem> RelatedProfiles { get; set; }

        public BsonObjectId Analytics { get; set; }

        public BsonObjectId PublisherId { get; set; }

        public bool IsFeatured { get; set; }

        /// <summary>
        /// Has this been verified by CESMII
        /// </summary>
        public bool IsVerified { get; set; }

        public DateTime? PublishDate { get; set; }

        public BsonObjectId ImagePortraitId { get; set; }
        public BsonObjectId ImageSquareId { get; set; }
        public BsonObjectId ImageLandscapeId { get; set; }

        public string _ccName1;
        public string _ccEmail1;
        public string _ccName2;
        public string _ccEmail2;

    }

    /// <summary>
    /// A very simple marketplace item used for related data and lookup scenarios
    /// where keeping data small is helpful
    /// </summary>
    public class MarketplaceItemSimple : AbstractEntity
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }


    /// <summary>
    /// This contains the structure to use for related items to a marketplace item. 
    /// It is the marketplaceItem id and the related type (recommended, required, related)
    /// </summary>
    public class RelatedItem : AbstractEntity
    {
        public BsonObjectId MarketplaceItemId { get; set; }
        /// <summary>
        /// This will map to an enum we have in the code for RelatedType
        /// </summary>
        public BsonObjectId RelatedTypeId { get; set; }
    }

    /// <summary>
    /// This contains the structure to use for related profile items to a marketplace item. 
    /// It is the profile id and the related type (recommended, required, related)
    /// </summary>
    public class RelatedProfileItem : AbstractEntity
    {
        /// <summary>
        /// This is the id from the CloudLib
        /// </summary>
        public string ProfileId { get; set; }
        /// <summary>
        /// This will map to a lookup record for RelatedType
        /// </summary>
        public BsonObjectId RelatedTypeId { get; set; }
    }


}