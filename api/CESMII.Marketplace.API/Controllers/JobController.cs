using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.Marketplace.Common;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Api.Shared.Controllers;
using CESMII.Marketplace.Api.Shared.Models;
using CESMII.Marketplace.Api.Shared.Extensions;
using CESMII.Marketplace.JobManager;
using CESMII.Marketplace.JobManager.Models;

namespace CESMII.Marketplace.Api.Controllers
{
    [Route("api/[controller]")]
    public class JobController : BaseController<JobController>
    {
        private readonly IJobFactory _jobFactory;
        private readonly IDal<JobLog, JobLogModel> _dalJobLog;
        private readonly UserDAL _dalUser;

        public JobController(IJobFactory jobFactory, 
            IDal<JobLog, JobLogModel> dalJobLog,
            UserDAL dalUser,
            ConfigUtil config, ILogger<JobController> logger) 
            : base(config, logger)
        {
            _jobFactory = jobFactory;
            _dalJobLog = dalJobLog;
            _dalUser = dalUser;
        }

        #region Job Factory
        [HttpPost, Route("Execute")]
        //[ProducesResponseType(200, Type = typeof(NodeSetModel))]
        [Authorize]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ExecuteJob([FromBody] JobPayloadModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"JobController|Execute|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var user = _dalUser.GetById(User.GetUserID());
            var result = await _jobFactory.ExecuteJob(model, user);

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
        public IActionResult GetByID([FromBody] IdStringModel model)
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
                return Ok(_dalJobLog.GetAllPaged(model.Skip, model.Take, false, true));
            }

            model.Query = model.Query.ToLower();
            var result = _dalJobLog.Where(s => s.CreatedById.Equals(new MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.Parse(User.GetUserID()))) &&
                            (string.IsNullOrEmpty(model.Query) || s.Name.ToLower().Contains(model.Query))
                            ,null, null, false, true);
            return Ok(result);
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
            var result = await _dalJobLog.Delete(model.ID, User.GetUserID());
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
