namespace CESMII.Marketplace.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using MongoDB.Bson;

    public class PublisherModelBase : AbstractModel
    {
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

        public bool Verified { get; set; }

        public string Description { get; set; }

        public string CompanyUrl { get; set; }

        public List<SocialMediaLinkModel> SocialMediaLinks { get; set; }

        public List<MarketplaceItemModel> MarketplaceItems { get; set; }

        public virtual bool IsActive { get; set; }

    }

    public class PublisherModel : PublisherModelBase
    {
        public virtual List<LookupItemModel> Categories { get; set; }

        public virtual List<LookupItemModel> IndustryVerticals { get; set; }
    }

    public class AdminPublisherModel : PublisherModelBase
    {
        public List<LookupItemFilterModel> Categories { get; set; }

        public List<LookupItemFilterModel> IndustryVerticals { get; set; }
    }

}
