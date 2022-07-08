using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Enums;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Data.Extensions;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class PublisherController : BaseController<PublisherController>
    {

        private readonly IDal<Publisher, PublisherModel> _dal;

        public PublisherController(
            IDal<Publisher, PublisherModel> dal,
            ConfigUtil config, ILogger<PublisherController> logger) 
            : base(config, logger)
        {
            _dal = dal;
        }


        [HttpGet, Route("All")]
        [ProducesResponseType(200, Type = typeof(List<PublisherModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetAll()
        {
            return Ok(_dal.GetAll());
        }

        [HttpPost, Route("search")]
        [ProducesResponseType(200, Type = typeof(DALResult<PublisherModel>))]
        [ProducesResponseType(400)]
        public IActionResult Search([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"PublisherController|Search|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //lowercase model.query
            model.Query = string.IsNullOrEmpty(model.Query) ? model.Query : model.Query.ToLower();

            //get all active and those matching certain look up types. 
            Func<Publisher, bool> predicate = x => x.IsActive;
            //now trim further by name if needed. 
            if (!string.IsNullOrEmpty(model.Query))
            {
                predicate = predicate.And(x => x.Name.ToLower().Contains(model.Query) ||
                                x.DisplayName.ToLower().Contains(model.Query));
            }
            var result = _dal.Where(predicate, model.Skip, model.Take, true, false);

            return Ok(result);
        }

        [HttpPost, Route("admin/search")]
        [Authorize(Policy = nameof(PermissionEnum.CanManagePublishers))]
        [ProducesResponseType(200, Type = typeof(DALResult<PublisherModel>))]
        [ProducesResponseType(400)]
        public IActionResult AdminSearch([FromBody] PagerFilterSimpleModel model)
        {
            return this.Search(model);
        }

        [HttpPost, Route("GetByID")]
        [ProducesResponseType(200, Type = typeof(PublisherModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"PublisherController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"PublisherController|GetById|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            return Ok(result);
        }

        [HttpPost, Route("GetByName")]
        [ProducesResponseType(200, Type = typeof(PublisherModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByName([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"PublisherController|GetByName|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //name is supposed to be unique. Note name is different than display name.
            //if we get more than one match, throw exception
            var matches = _dal.Where(x => x.Name.ToLower().Equals(model.ID.ToLower()), null, null, false, true).Data;

            if (matches == null || !matches.Any())
            {
                _logger.LogWarning($"PublisherController|GetByName|No records found matching this name: {model.ID}");
                return BadRequest($"No records found matching this name: {model.ID}");
            }
            if (matches.Count > 1)
            {
                _logger.LogWarning($"PublisherController|GetByName|Multiple records found matching this name: {model.ID}");
                return BadRequest($"Multiple records found matching this name: {model.ID}");
            }

            return Ok(matches[0]);
        }
    }
}
