using System.Collections.Generic;

namespace CESMII.Marketplace.Common.Models
{
    public class CloudLibraryConfig
    {
        public string EndPoint { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<string> ExcludedNodeSets { get; set; }
    }
}
