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
        public MarketplaceItemSimpleModel MarketplaceItem { get; set; }

        /// <summary>
        /// Job description.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Friendly display name
        /// </summary>
        public string DisplayName { get; set; }

        public string TypeName { get; set; }

        /// <summary>
        /// A material icon name value. Can be null.
        /// </summary>
        public string IconName { get; set; }

        /// <summary>
        /// JSON string of customizable data unique to each job. These are settings that apply to any user 
        /// executing this job.
        /// </summary>
        public string Data { get; set; }
        public virtual bool IsActive { get; set; }

        /// <summary>
        /// The action type will be link to navigate to a custom job page on front end to then do something.
        /// Or the action type will be execute job which will cause a job to execute on click
        /// </summary>
        public JobActionTypeEnum ActionType { get; set; } = JobActionTypeEnum.Standard;

        /// <summary>
        /// Css Class for the link or button associated with the item. 
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Flag for controlling if this job be seen on front end ui when not logged in.
        /// </summary>
        public bool RequiresAuthentication { get; set; }
    }

    public class JobDefinitionSimpleModel : AbstractModel
    {
        /// <summary>
        /// Job description.
        /// </summary>
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string IconName { get; set; }
        public JobActionTypeEnum ActionType { get; set; } = JobActionTypeEnum.Standard;
        public string ClassName { get; set; }
        public bool RequiresAuthentication { get; set; }
    }

}
