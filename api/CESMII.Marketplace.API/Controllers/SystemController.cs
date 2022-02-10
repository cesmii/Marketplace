﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.Marketplace.Common;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Extensions;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class SystemController : BaseController<SystemController>
    {
        public SystemController(ConfigUtil config, ILogger<SystemController> logger)
            : base(config, logger)
        {
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

            _logger.LogCritical($"REACT|LogMessage|User:{User.GetUserID()}|Error:{model.Message}|Url:{model.Url}");

            return Ok(result);
        }

    }
}
