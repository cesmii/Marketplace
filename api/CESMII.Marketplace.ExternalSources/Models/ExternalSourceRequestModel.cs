namespace CESMII.Marketplace.ExternalSources.Models
{
    public class ExternalSourceRequestModel
    {
        public string ID { get; set; }
        public string SourceId { get; set; }
        public bool IsTracking { get; set; } = false;
    }
}