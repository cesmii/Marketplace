using CESMII.Marketplace.DAL.Models;
using System.Collections.Generic;

namespace CESMII.Marketplace.Api.Shared.Models
{
    public class MarketplaceSearchModel : PagerFilterSimpleModel
    {
        ///// <summary>
        ///// List of category ids
        ///// </summary>
        //public List<string> Categories { get; set; }

        ///// <summary>
        ///// List of industry verticals
        ///// </summary>
        //public List<string> IndustryVerticals { get; set; }

        //public string PublisherId { get; set; }

        public List<LookupGroupByModel> Filters { get; set; }
        
        /// <summary>
        /// List of item types - sm-app, sm-profile and others in the future. 
        /// </summary>
        public List<LookupItemFilterModel> ItemTypes { get; set; }
    }
}
