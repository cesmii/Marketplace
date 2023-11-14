using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.Api.Shared.Models
{
    public class RelatedLookupModel
    {
        public string ID { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string Namespace { get; set; }
        public ExternalSourceSimple ExternalSource { get; set; }
    }

    public class RelatedSearchModel
    {
        public MarketplaceSearchModel Criteria { get; set; }
        /// <summary>
        /// External Source code
        /// </summary>
        public string Code { get; set; }
    }

}
