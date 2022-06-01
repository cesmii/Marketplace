using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Extensions;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class LookupController : BaseController<LookupController>
    {
        private readonly IDal<LookupItem, LookupItemModel> _dal;
        private readonly IDal<Publisher, PublisherModel> _dalPublisher;
        public LookupController(IDal<LookupItem, LookupItemModel> dal,
            IDal<Publisher, PublisherModel> dalPublisher,
            ConfigUtil config, ILogger<LookupController> logger) 
            : base(config, logger)
        {
            _dal = dal;
            _dalPublisher = dalPublisher;
        }


        [HttpGet, Route("All")]
        [ProducesResponseType(200, Type = typeof(List<LookupItemModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetAll()
        {
            var result = _dal.GetAll().OrderBy(x => x.LookupType.EnumValue.ToString()).ThenBy(x => x.DisplayOrder )
                .ThenBy(x => x.Name).ToList();

            //append publishers
            var publishers = _dalPublisher.GetAll();
            result = result.Union(publishers.OrderBy(p => p.DisplayName).Select(pub => new LookupItemModel
            {
                ID = pub.ID,
                Name = pub.DisplayName,
                IsActive = true,
                DisplayOrder = 999,
                LookupType = new LookupTypeModel() { EnumValue = LookupTypeEnum.Publisher, Name = LookupTypeEnum.Publisher.ToString() } 
                //tbd any other lookup fields
            }).ToList()).ToList();

            return Ok(result);
        }


        [HttpGet, Route("searchcriteria")]
        [ProducesResponseType(200, Type = typeof(MarketplaceSearchModel))]
        [ProducesResponseType(400)]
        public IActionResult GetSearchCriteria() //[FromBody] LookupGroupByModel model)
        {
            //Filter out MarketplaceStatus from collection
            var allItems = _dal.GetAll().Where(x => x.LookupType.EnumValue == LookupTypeEnum.Process 
                || x.LookupType.EnumValue == LookupTypeEnum.IndustryVertical);

            if (allItems == null)
            {
                   _logger.LogWarning($"LookupController|GetSearchCriteria|No records found");
                    return BadRequest($"No records found");
            }
            
            //group the result by lookup type
            var grpItems = allItems.GroupBy(x => new { x.LookupType.EnumValue, x.LookupType.Name });
            var filters = new List<LookupGroupByModel>();
            foreach (var item in grpItems)
            {
                filters.Add(new LookupGroupByModel
                {
                    Name = item.Key.Name.ToString(),
                    EnumValue = item.Key.EnumValue,
                    Items = item.ToList().Select(itm => new LookupItemFilterModel
                    {
                        ID = itm.ID,
                        Name = itm.Name,
                        IsActive = itm.IsActive,
                        DisplayOrder = itm.DisplayOrder
                        //tbd any other lookup fields
                    }).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToList()                    
                });
            }

            // get publisher profiles, append to group by model
            var publishers = _dalPublisher.GetAll();

            filters.Add(new LookupGroupByModel()
            {
                Name = "Publishers",
                EnumValue = LookupTypeEnum.Publisher,
                Items = publishers.OrderBy(p => p.DisplayName).Select(pub => new LookupItemFilterModel 
                {
                    ID = pub.ID,
                    Name = pub.DisplayName,
                    IsActive = true,
                    DisplayOrder = 999
                    //tbd any other lookup fields
                }).ToList()
            });

            //get item types
            var itemTypes = _dal.GetAll().Where(x => x.LookupType.EnumValue == LookupTypeEnum.SmItemType && x.IsActive);

            //leave query null, skip, take defaults
            var result = new MarketplaceSearchModel() { 
                Filters = filters, 
                ItemTypes = itemTypes.Select(itm => new LookupItemFilterModel
                {
                    ID = itm.ID,
                    Name = itm.Name,
                    IsActive = itm.IsActive,
                    DisplayOrder = itm.DisplayOrder
                    //tbd any other lookup fields
                }).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToList()
            };

            //return result
            return Ok(result);
        }

    }
}
