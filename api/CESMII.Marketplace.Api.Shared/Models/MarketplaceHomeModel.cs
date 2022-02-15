namespace CESMII.Marketplace.Api.Shared.Models
{
    using System.Collections.Generic;
    using CESMII.Marketplace.DAL.Models;

    public class MarketplaceHomeModel
    {
        public List<MarketplaceItemModel> FeaturedItems { get; set; }
        public List<MarketplaceItemModel> NewItems { get; set; }
        public List<MarketplaceItemModel> PopularItems { get; set; }
    }
}
