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

        /// <summary>
        /// Friendly display name
        /// </summary>
        public string DisplayName { get; set; }

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
        /// Css Class for the link or button associated with the item. 
        /// </summary>
        public string ClassName { get; set; } = "btn btn-link mr-2 mt-2";

        /// <summary>
        /// Flag for controlling if this job be seen on front end ui when not logged in.
        /// </summary>
        public bool RequiresAuthentication { get; set; } = false;
    }

}
