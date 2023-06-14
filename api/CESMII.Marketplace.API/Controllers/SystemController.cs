using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Api.Shared.Utils;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class SystemController : BaseController<SystemController>
    {
        private readonly IDal<MarketplaceItem, MarketplaceItemModel> _dalMarketplace;
        private readonly IDal<Publisher, PublisherModel> _dalPublisher;
        private readonly ICloudLibDAL<MarketplaceItemModelWithCursor> _dalCloudLib;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;

        public SystemController(ConfigUtil config, ILogger<SystemController> logger
            ,UserDAL dalUser
            ,IDal<MarketplaceItem, MarketplaceItemModel> dalMarketplace
            ,IDal<Publisher, PublisherModel> dalPublisher
            ,ICloudLibDAL<MarketplaceItemModelWithCursor> dalCloudLib
            ,IDal<LookupItem, LookupItemModel> dalLookup
            )
            : base(config, logger, dalUser)
        {
            _dalMarketplace = dalMarketplace;
            _dalPublisher = dalPublisher;
            _dalCloudLib = dalCloudLib;
            _dalLookup = dalLookup;
        }

        [AllowAnonymous, HttpPost, Route("log/public")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public IActionResult LogMessagePublic([FromBody] FrontEndErrorModel model)
        {
            var result = new ResultMessageWithDataModel() { IsSuccess = true, Message = "", Data = null };

            _logger.LogCritical($"REACT|LogMessage|User:Unknown|Error:{model.Message}|Url:{model.Url}");

            return Ok(result);
        }

        [HttpPost, Route("log/private")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public IActionResult LogMessagePrivate([FromBody] FrontEndErrorModel model)
        {
            var result = new ResultMessageWithDataModel() { IsSuccess = true, Message = "", Data = null };

            _logger.LogCritical($"REACT|LogMessage|User:{LocalUser.UserName}|Error:{model.Message}|Url:{model.Url}");

            return Ok(result);
        }

        /// <summary>
        /// Return dynamic portion of sitemap data for SEO purposes.
        /// Return marketplace items, profile items, publisher items
        /// Do not page data because we want it to include all data. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous, HttpPost, Route("dynamic/sitemap")]
        [ProducesResponseType(200, Type = typeof(List<SiteMapModel>))]
        public async Task<IActionResult> DynamicSitemap()
        {
            //limit to active and publish status of live
            var util = new MarketplaceUtil(_dalLookup);
            var predicates = new List<Func<MarketplaceItem, bool>> {x => x.IsActive};
            predicates.Add(util.BuildStatusFilterPredicate());

            var items = _dalMarketplace.Where(predicates,null, null, false, false).Data.Select(itm => new SiteMapModel
            {
                ID = itm.ID,
                Name = itm.Name,
                Type = itm.Type.Code,
                Updated = itm.Updated.HasValue ? itm.Updated.Value : itm.Created
            });
            var profiles = (await _dalCloudLib.GetAll()).Select(itm => new SiteMapModel
            {
                ID = itm.ID,
                Name = itm.Name,
                Type = itm.Type.Code,
                Updated =  !itm.Updated.HasValue && itm.Created == new DateTime(0) ? null :
                            itm.Updated.HasValue ? itm.Updated.Value : itm.Created
            });
            var publishers = _dalPublisher.GetAll().Select(itm => new SiteMapModel
            {
                ID = itm.ID,
                Name = itm.Name,
                Type = "publisher",
                Updated = null
            });

            var result = items.Union(profiles).Union(publishers).ToList();
            return Ok(result);
        }

        protected class SiteMapModel
        {
            public string ID { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public System.DateTime? Updated { get; set; }

        }


    }
}
