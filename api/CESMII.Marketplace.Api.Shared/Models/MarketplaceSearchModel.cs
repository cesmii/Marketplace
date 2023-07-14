using CESMII.Marketplace.DAL.Models;
using System.Collections.Generic;

namespace CESMII.Marketplace.Api.Shared.Models
{
    public class MarketplaceSearchModel : PagerFilterSimpleModel
    {
        public string PageCursors { get; set; }
        public List<LookupGroupByModel> Filters { get; set; }

        /// <summary>
        /// List of item types - sm-app, sm-profile and others in the future. 
        /// </summary>
        public List<LookupItemFilterModel> ItemTypes { get; set; }

        /// <summary>
        /// When a user enters a reserved keyword, we must select that type and filter on it. 
        /// However, the impact to the search is modified to treat its selection slightly differently
        /// than selecting the item type as is. 
        /// List of item types - sm-app, sm-profile and others in the future. 
        /// </summary>
        /// <remarks>This is calculated on search in the controller.</remarks>
        public List<LookupItemFilterModel> KeyWordItemTypes { get; set; }
    }
}
