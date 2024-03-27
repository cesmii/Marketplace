using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.Marketplace.Data.Entities
{
    /// <summary>
    /// This is the Mongo DB version of this entity.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class JobDefinition : MarketplaceAbstractEntity
    {
        public string Name { get; set; }

        public BsonObjectId MarketplaceItemId { get; set; }

        public string TypeName { get; set; }

        /// <summary>
        /// A material icon name value. Can be null.
        /// </summary>
        public string IconName { get; set; }

        /// <summary>
        /// Field to store settings and structure unique to this job
        /// </summary>
        /// <remarks>This will be encrypted to protect any sensitive data that may be returned.</remarks>
        public string Data { get; set; }

        /// <summary>
        /// The action type will be link to navigate to a custom job page on front end to then do something.
        /// Or the action type will be execute job which will cause a job to execute on click
        /// </summary>
        public int ActionType { get; set; }

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
        public string ClassName { get; set; } = "btn btn-link mr-2 mt-2";

        /// <summary>
        /// Can this job be seen on front end ui if not logged in?
        /// </summary>
        public bool RequiresAuthentication { get; set; } = false;
    }

}
