namespace CESMII.Marketplace.JobManager.Models
{
    /// <summary>
    /// This is the model used by the endpoint and will be converted into 
    /// </summary>
    public class JobPayloadModel 
    {
        public string JobDefinitionId { get; set; }
        public string MarketplaceItemId { get; set; }
        public string Payload{ get; set; }
    }
}
