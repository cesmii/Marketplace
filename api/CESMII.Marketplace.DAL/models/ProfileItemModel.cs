namespace CESMII.Marketplace.DAL.Models
{
    using CESMII.Marketplace.Common.Enums;

    /// <summary>
    /// Extend marketplaceitemmodel for the export scenario.
    /// </summary>
    public class ProfileItemExportModel
    {
        public MarketplaceItemModel Item { get; set; }
        public string NodesetXml { get; set; }
    }

    /// <summary>
    /// A profile related marketplace item. Used in the admin tool to make selections
    /// where keeping data small is helpful
    /// </summary>
    public class ProfileItemRelatedModel
    {
        /// <summary>
        /// This represents the ProfileId.
        /// </summary>
        public string RelatedId { get; set; }
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Namespace { get; set; }
        public string Description { get; set; }
        public LookupItemModel RelatedType { get; set; }
    }

}