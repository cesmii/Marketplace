namespace CESMII.Marketplace.Api.Shared.Models
{
    public class IdStringModel
    {
        public string ID { get; set; }
       
    }

    public class IdStringWithTrackingModel : IdStringModel
    {
        public bool IsTracking { get; set; } = false;
        
    }


    public class IdIntModel
    {
        public int ID { get; set; }
    }

    public class IdLongModel
    {
        public long ID { get; set; }
    }
}
