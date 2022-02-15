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
    }
}
