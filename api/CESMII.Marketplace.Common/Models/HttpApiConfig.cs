using System.Collections.Generic;

namespace CESMII.Marketplace.Common.Models
{
    public class HttpApiConfig
    {
        public string Url { get; set; }
        public string ContentType { get; set; } = "application/json";
        public bool IsPost { get; set; } = true;
        public string QueryString { get; set; }
        public string Body { get; set; }
        public string BearerToken { get; set; }
    }
}
