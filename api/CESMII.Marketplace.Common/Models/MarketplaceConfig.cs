using System.Collections.Generic;

namespace CESMII.Marketplace.Common.Models
{
    public class MarketplaceConfig
    {
        public string DefaultItemTypeId { get; set; }
        public MarketplaceItemConfig SmProfile { get; set; }
        public bool EnableCloudLibSearch { get; set; }
    }

    public class MarketplaceItemConfig 
    {
        public string TypeId { get; set; }
        public string Code { get; set; }
        public string DefaultImageIdPortrait { get; set; }
        public string DefaultImageIdBanner { get; set; }
        public string DefaultImageIdLandscape { get; set; }
    }
}
