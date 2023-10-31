using System;
using System.Collections.Generic;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common.Enums;

namespace CESMII.Marketplace.DAL.Models
{
    public class ExternalSourceModel : AbstractModel
    {
        /// <summary>
        /// This will tell us which item type this source is associated with.
        /// </summary>
        public LookupItemModel ItemType { get; set; }

        public PublisherModel Publisher { get; set; }

        /// <summary>
        /// Job description.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Expected to be unique across external sources. Using code rather than id so that
        /// we don't have to maintain a settings file for each environment with code for each source. 
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// This is the .NET type to instantiate for this external source DAL. 
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// This is the .NET type to instantiate for this external source admin DAL. 
        /// </summary>
        public string AdminTypeName { get; set; }
        public string BaseUrl { get; set; }

        /// <summary>
        /// Field to store settings and structure unique to this external source
        /// Structure will be understood and usable within the DAL that processes this external source. 
        /// </summary>
        /// <remarks>This will be encrypted to protect any sensitive data that may be returned.</remarks>
        public string Data { get; set; }
        /// <summary>
        /// IsEnabled is a way to turn on or off external source without deleting.
        /// </summary>
        public bool Enabled { get; set; }
        public ImageItemSimpleModel DefaultImagePortrait { get; set; }
        public ImageItemSimpleModel DefaultImageBanner { get; set; }
        public ImageItemSimpleModel DefaultImageLandscape { get; set; }
        public virtual bool IsActive { get; set; }
        /// <summary>
        /// If an exception occurs when calling the external API, should we stop the whole search. 
        /// If false, we log exception but do not fail the search.
        /// </summary>
        /// <remarks>Note this does not apply when trying to get an individual record</remarks>
        public virtual bool FailOnException { get; set; }
    }
}
