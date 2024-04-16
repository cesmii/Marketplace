using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.DAL.ExternalSources.Models
{
    public class ExternalSourceRequestModel : ExternalSourceSimple
    {
        public bool IsTracking { get; set; } = false;
    }
}