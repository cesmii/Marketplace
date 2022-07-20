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
        /// <summary>
        /// This only requires the token value, the HTTPClient will inject Bearer into the 
        /// header value automatically.
        /// </summary>
        public string BearerToken { get; set; }
        
        /// <summary>
        /// This is for the scenario where the API requires an authorization header but not a bearer
        /// token. 
        /// </summary>
        public KeyValuePair<string, string> AuthToken { get; set; }
    }
}
