namespace CESMII.Marketplace.Common.Models
{
    public class MarketplaceConfig
    {
        public MarketplaceItemConfig SmApp { get; set; }
        public MarketplaceItemConfig SmProfile { get; set; }
        public MarketplaceItemConfig SmHardware { get; set; }
        public MarketplaceItemConfig SmDatasource { get; set; }
        public bool EnableCloudLibSearch { get; set; }
    }

    public class MarketplaceItemConfig
    {
        public string TypeId { get; set; }
        //public string DefaultImageIdSquare { get; set; }
        public string DefaultImageIdPortrait { get; set; }
        public string DefaultImageIdLandscape { get; set; }
    }
}
