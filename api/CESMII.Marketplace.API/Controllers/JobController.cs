using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.Marketplace.Common;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.JobManager;
using CESMII.Marketplace.JobManager.Models;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class JobController : BaseController<JobController>
    {
        private readonly IJobFactory _jobFactory;
        private readonly IDal<JobDefinition, JobDefinitionModel> _dal;
        private readonly IDal<JobLog, JobLogModel> _dalJobLog;

        public JobController(IJobFactory jobFactory,
            IDal<JobDefinition, JobDefinitionModel> dal,
            IDal<JobLog, JobLogModel> dalJobLog,
            UserDAL dalUser,
            ConfigUtil config, ILogger<JobController> logger) 
            : base(config, logger, dalUser)
        {
            _jobFactory = jobFactory;
            _dal = dal;
            _dalJobLog = dalJobLog;
        }

        [HttpPost, Route("GetByName")]
        //[ProducesResponseType(200, Type = typeof(NodeSetModel))]
        [ProducesResponseType(200, Type = typeof(JobDefinitionModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByName([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"JobController|GetByName|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //name is supposed to be unique. Note name is different than display name.
            //if we get more than one match, throw exception
            var matches = _dal.Where(x => x.Name.ToLower().Equals(model.ID.ToLower()), null, null, false, true).Data;

            if (!matches.Any())
            {
                _logger.LogWarning($"JobController|GetByName|No records found matching this name: {model.ID}");
                return BadRequest($"No records found matching this name: {model.ID}");
            }
            if (matches.Count > 1)
            {
                _logger.LogWarning($"JobController|GetByName|Multiple records found matching this name: {model.ID}");
                return BadRequest($"Multiple records found matching this name: {model.ID}");
            }

            var result = matches[0];
            return Ok(result);
        }



        #region Job Factory
        [HttpPost, Route("Execute")]
        //[ProducesResponseType(200, Type = typeof(NodeSetModel))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ExecuteJob([FromBody] JobPayloadModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"JobController|ExecuteJob|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }
            //check if job requires user to be authorized
            var job = _dal.GetById(model.JobDefinitionId);
            if (job == null)
            {
                _logger.LogWarning($"JobController|ExecuteJob|Job {model.JobDefinitionId} not found. (null)");
                return BadRequest($"Job not found.");
            }
            if (job.RequiresAuthentication && !User.Identity.IsAuthenticated)
            {
                _logger.LogWarning($"JobController|ExecuteJob|Job {model.JobDefinitionId}|User is not authenticated. This job requires the user to be logged in.");
                return Unauthorized();
            }

            //execute job
            var result = await _jobFactory.ExecuteJob(model, User.Identity.IsAuthenticated ? base.LocalUser : null);

            return Ok(new ResultMessageWithDataModel() {
                Data = result,
                IsSuccess = true,
                Message = "Job started."
            });

        }
        #endregion

        #region Job Log
        [HttpPost, Route("log/GetByID")]
        //[ProducesResponseType(200, Type = typeof(NodeSetModel))]
        [ProducesResponseType(200, Type = typeof(JobLogModel))]
        [ProducesResponseType(400)]
        public IActionResult GetLogByID([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"JobController|Log|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dalJobLog.GetById(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"JobController|Log|GetById|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            return Ok(result);
        }

        /// <summary>
        /// Get my job logs
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("log/Mine")]
        [Authorize]
        [ProducesResponseType(200, Type = typeof(DALResult<JobLogModel>))]
        public IActionResult GetMine([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("JobController|Log|GetMine|Invalid model.");
                return BadRequest("Profile|GetMine|Invalid model");
            }

            if (string.IsNullOrEmpty(model.Query))
            {
                return Ok(_dalJobLog.Where(s => s.CreatedById.Equals(MongoDB.Bson.ObjectId.Parse(LocalUser.ID)) &&
                                s.IsActive
                                , null, null, false, true));
            }

            model.Query = model.Query.ToLower();
            return Ok(_dalJobLog.Where(s => s.CreatedById.Equals(MongoDB.Bson.ObjectId.Parse(LocalUser.ID)) &&
                            (string.IsNullOrEmpty(model.Query) || s.Name.ToLower().Contains(model.Query)) &&
                            s.IsActive
                            , null, null, false, true));
        }

        /// <summary>
        /// Delete an existing job log. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("log/Delete")]
        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdStringModel model)
        {
            //This is a soft delete
            var result = await _dalJobLog.Delete(model.ID, LocalUser.ID);
            if (result < 0)
            {
                _logger.LogWarning($"JobController|Log|Delete|Could not delete item. Invalid id:{model.ID}.");
                return BadRequest("Could not delete item. Invalid id.");
            }
            _logger.LogInformation($"JobController|Log|Delete|Deleted item. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }
        #endregion

    }

}
