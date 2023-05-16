namespace CESMII.Marketplace.DAL.Models
{
    using CESMII.Marketplace.Common.Enums;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class MarketplaceItemModelBase : AbstractMarketplaceModel
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
        [RegularExpression("^[-_,A-Za-z0-9]*$", ErrorMessage = "Name invalid. No special characters or spaces permitted.")]
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        /// <summary>
        /// Friendly name viewed and seen by the user on all screens.
        /// </summary>
        public string DisplayName { get; set; }

        public string Version { get; set; }
        
        /// <summary>
        /// Namespace URI. Only applies to profiles pulled from CloudLib
        /// </summary>
        public string Namespace { get; set; }

        public string Abstract { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Type of marketplace item: profile, app
        /// </summary>
        public virtual LookupItemModel Type { get; set; }

        /// <summary>
        /// This is the status associated with the item in a more general context. ie: Under Review, Approved, Peer Reviewed)
        /// This status controls visibility of content. (ie: Draft, Live, Archive)
        /// </summary>
        public virtual LookupItemModel Status { get; set; }

        /// <summary>
        /// TBD - May need to convert this to list.
        /// </summary>
        public int? AuthorId { get; set; }

        /// <summary>
        /// TBD - May need to convert this to list.
        /// </summary>
        public virtual UserModel Author { get; set; }
        
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

        public PublisherModel Publisher { get; set; }

        public bool IsFeatured { get; set; }

        /// <summary>
        /// Has this been verified by CESMII
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// List of items that have been marked as related for this marketplace item by admin. Could be
        /// apps, hardware, profiles.
        /// </summary>
        /// <remarks>This will get merged in with util.SimilarItems set in controller based on common processes, industry verts, etc. </remarks>
        public List<MarketplaceItemRelatedModel> SimilarItems { get; set; }

        /// <summary>
        /// Groups of related items. 
        /// </summary>
        /// <remarks>Doing it this way allows for additional groups to be added w/o additional back end coding</remarks>
        public List<RelatedItemsGroupBy> RelatedItemsGrouped { get; set; }

        public ImageItemSimpleModel ImagePortrait { get; set; }
        
        [Obsolete("Retire ImageSquare")]
        public ImageItemSimpleModel ImageSquare { get; set; }
        public ImageItemSimpleModel ImageLandscape { get; set; }

        public virtual List<JobDefinitionSimpleModel> JobDefinitions { get; set; }

        public string ccName1 { get; set; }
        public string ccEmail1 { get; set; }
        public string ccName2 { get; set; }
        public string ccEmail2 { get; set; }

    }

    public class MarketplaceItemModel : MarketplaceItemModelBase
    {
        public virtual List<LookupItemModel> Categories { get; set; }

        public virtual List<LookupItemModel> IndustryVerticals { get; set; }

        public virtual MarketplaceItemAnalyticsModel Analytics { get; set; }

    }

    public class AdminMarketplaceItemModel : MarketplaceItemModelBase
    {
        public List<LookupItemFilterModel> Categories { get; set; }

        public List<LookupItemFilterModel> IndustryVerticals { get; set; }

        /// <summary>
        /// List of items that are related to this item.
        /// </summary>
        public virtual List<MarketplaceItemFilterModel> RelatedItems { get; set; }

        /// <summary>
        /// List of items that are related to this item.
        /// </summary>
        public virtual List<ProfileItemFilterModel> RelatedProfiles { get; set; }
    }

    /// <summary>
    /// A very simple marketplace item used for related data and lookup scenarios
    /// where keeping data small is helpful
    /// </summary>
    public class MarketplaceItemSimpleModel: AbstractModel
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// A model with abbreviated marketplace item data used for related data 
    /// where keeping data small is helpful
    /// </summary>
    public class MarketplaceItemRelatedModel : MarketplaceItemSimpleModel
    {
        public string Version { get; set; }

        /// <summary>
        /// Namespace URI. Only applies to profiles pulled from CloudLib
        /// </summary>
        public string Namespace { get; set; }

        public string Abstract { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Type of marketplace item: profile, app
        /// </summary>
        public virtual LookupItemModel Type { get; set; }

        public ImageItemSimpleModel ImagePortrait { get; set; }

        public ImageItemSimpleModel ImageLandscape { get; set; }

        public LookupItemModel RelatedType { get; set; }
    }


    public class MarketplaceItemFilterModel : MarketplaceItemRelatedModel
    {
        /// <summary>
        /// This represents the Marketplace item id.
        /// </summary>
        public string RelatedId { get; set; }
        public bool Selected { get; set; }
    }

    public class RelatedItemsGroupBy
    {
        public LookupItemModel RelatedType { get; set; }
        public List<MarketplaceItemRelatedModel> Items { get; set; }
    }
}