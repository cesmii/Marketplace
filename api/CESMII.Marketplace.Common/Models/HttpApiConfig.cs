using System.Collections.Generic;
using System.Net.Http;

namespace CESMII.Marketplace.Common.Models
{
    public class HttpApiConfig
    {
        public string BaseAddress { get; set; }
        /// <summary>
        /// Url is only a portion of the address. It is intended to be combined with 
        /// the base address to form the entire address
        /// </summary>
        public string Url { get; set; }
        public string ContentType { get; set; } = "application/json";
        public string BodyContentType { get; set; } = "application/json";
        public HttpMethod Method { get; set; } = HttpMethod.Post;
        public string QueryString { get; set; }
        public HttpContent Body { get; set; }
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

        public List<KeyValuePair<string, string>> Headers { get; set; }
    }
}
