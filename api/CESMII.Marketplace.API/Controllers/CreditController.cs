using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Service;
using Microsoft.Extensions.Logging;
using CESMII.Marketplace.Common;
using Microsoft.AspNetCore.Mvc;
using CESMII.Marketplace.Api.Shared.Models;

namespace CESMII.Marketplace.API.Controllers
{
    [Route("api/[controller]")]
    public class CreditController : BaseController<CreditController>
    {
        private readonly ICommonService<OrganizationModel> _organizationService;

        public CreditController(ICommonService<OrganizationModel> organizationService, UserDAL dalUser,
            ConfigUtil config, ILogger<CreditController> logger)
            : base(config, logger, dalUser)
        {
            _organizationService = organizationService;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(string))]
        [ProducesResponseType(400)]
        public IActionResult GetUserCredit()
        {
            // TODO call salesforce api to get credits.

            // Search for organization
            var filter = new PagerFilterSimpleModel() { Query = LocalUser.Organization.Name, Skip = 0, Take = 9999 };
            var listMatchOrganizationName = _organizationService.Search(filter).Data;

            long credits = 0;
            if (listMatchOrganizationName != null && listMatchOrganizationName.Count == 1)
            {
                credits = listMatchOrganizationName[0].Credits;
            }

            return Ok(new ResultMessageWithDataModel()
            {
                Data = credits,
                IsSuccess = true,
                Message = "Credits fetched..."
            });
        }
    }
}
