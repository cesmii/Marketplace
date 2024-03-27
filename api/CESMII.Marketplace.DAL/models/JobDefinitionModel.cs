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
        /// The action link is a relative or full url to navigate to a page on front end to then do something.
        /// In most cases, this should then navigate to a page on front end that knows how to take a job id and do something.
        /// The link will also have a custom replace format string to embed the {{jobid}} in the link so the front end knows what
        /// job to associate with link.
        /// </summary>
        public string ActionLink { get; set; }

        /// <summary>
        /// Css Class for the link or button associated with the item. 
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Can this job be seen on front end ui if not logged in?
        /// </summary>
        public bool RequiresAuthentication { get; set; }
    }

    public class JobDefinitionSimpleModel : AbstractModel
    {
        /// <summary>
        /// Job description.
        /// </summary>
        public string Name { get; set; }
        public string IconName { get; set; }
    }

}
