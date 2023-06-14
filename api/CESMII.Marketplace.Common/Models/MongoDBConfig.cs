namespace CESMII.Marketplace.Common.Models
{
    using System.Collections.Generic;

    public class MongoDBConfig
    {
        public string DatabaseName { get; set; }
        
        public string ConnectionString { get; set; }

        public string NLogCollectionName { get; set; }
    }
}
