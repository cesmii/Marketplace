namespace CESMII.Marketplace.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using CESMII.Marketplace.Common.Enums;
    using CESMII.Marketplace.DAL.Models;
    using CESMII.Marketplace.Data.Entities;
    using MongoDB.Bson;

    public class MarketplaceItemAnalyticsModel : AbstractModel
    {
        public string MarketplaceItemId { get; set; }
        /// <summary>
        /// Id of SM Profile from CloudLib
        /// </summary>
        public string CloudLibId { get; set; }
        public string Url { get; set; }
        public int PageVisitCount { get; set; }
        public int SearchResultCount { get; set; }
        public int LikeCount{ get; set; }
        public int DislikeCount { get; set; }
        public int MoreInfoCount { get; set; }
        public int ShareCount { get; set; }

        public int DownloadCount { get; set; }

       // public List<MarketplaceDownloadHistory> DownloadHistory { get; set; }
    }
}
