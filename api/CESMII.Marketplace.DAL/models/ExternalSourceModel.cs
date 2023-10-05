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
        /// This is the .NET type to instantiate for this external source. 
        /// </summary>
        public string TypeName { get; set; }
        public string BaseUrl { get; set; }
        public string AccessToken { get; set; }
        /// <summary>
        /// List of 1:many urls that may be used in the calling of the API
        /// </summary>
        public List<KeyValuePair<string, string>> Urls { get; set; }

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
    }
}
