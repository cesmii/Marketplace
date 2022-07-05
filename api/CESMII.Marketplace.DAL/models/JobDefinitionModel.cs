using System;
using System.Collections.Generic;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common.Enums;

namespace CESMII.Marketplace.DAL.Models
{
    public class JobDefinitionModel : AbstractModel
    {
        /// <summary>
        /// Marketplace Item Id - This will guide us as to which data we need to act on.
        /// </summary>
        public string MarketplaceItemId { get; set; }

        /// <summary>
        /// Job description.
        /// </summary>
        public string Name { get; set; }

        public string TypeName { get; set; }
        
        /// <summary>
        /// JSON string of customizable data unique to each job. These are settings that apply to any user 
        /// executing this job.
        /// </summary>
        public string Data { get; set; }
    }

    public class JobDefinitionSimpleModel : AbstractModel
    {
        /// <summary>
        /// Job description.
        /// </summary>
        public string Name { get; set; }
    }

}
