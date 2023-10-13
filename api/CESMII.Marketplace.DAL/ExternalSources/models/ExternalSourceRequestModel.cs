namespace CESMII.Marketplace.DAL.ExternalSources.Models
{
    public class ExternalSourceRequestModel
    {
        public string ID { get; set; }
        public string SourceId { get; set; }
        public bool IsTracking { get; set; } = false;
    }
}