using System.Collections.Generic;

namespace CESMII.Marketplace.Api.Shared.Models
{
    using Newtonsoft.Json.Linq;

    public class ResultMessageModel 
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class ResultMessageWithDataModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        //public JRaw Data { get; set; }  //TBD - come back to this. Not returning the data as expected. Returns []
        public dynamic Data { get; set; }
    }

    public class ResultMessageExportModel : ResultMessageWithDataModel
    {
        public List<string> Warnings { get; set; }
    }

}
