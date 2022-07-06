using System;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using CESMII.Marketplace.JobManager.Jobs;
using CESMII.Marketplace.JobManager.Models;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL;
using CESMII.Marketplace.DAL.Models;
using System.Collections.Generic;
using CESMII.Marketplace.Common.Enums;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Data.Repositories;

namespace CESMII.Marketplace.JobManager
{
    public interface IJobFactory
    {
        Task<string> ExecuteJob(JobPayloadModel model, UserModel user);
    }


    public class JobFactory : IJobFactory
    {
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger<JobFactory> _logger;
        protected readonly IDal<MarketplaceItem, MarketplaceItemModel> _dalMarketplace;
        protected readonly IDal<JobLog, JobLogModel> _dalJobLog;
        protected readonly IDal<JobDefinition, JobDefinitionModel> _dalJobDefinition;

        public JobFactory(
            IServiceScopeFactory serviceScopeFactory,
            IConfiguration configuration,
            ILogger<JobFactory> logger,
            IDal<MarketplaceItem, MarketplaceItemModel> dalMarketplace,
            IDal<JobLog, JobLogModel> dalJobLog,
            IDal<JobDefinition, JobDefinitionModel> dalJobDefinition
        )
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _logger = logger;
            _dalMarketplace = dalMarketplace;
            _dalJobLog = dalJobLog;
            _dalJobDefinition = dalJobDefinition;
        }

        /// <summary>
        /// Execute the job associated with this marketplace item
        /// </summary>
        public async Task<string> ExecuteJob(JobPayloadModel model, UserModel user)
        {
            //call marketplace dal, get marketplace data,
            var item = _dalMarketplace.GetById(model.MarketplaceItemId);
            
            //get job def data - common settings to dictate how job will run, type name used for instantiation.
            if (item.JobDefinitions == null || !item.JobDefinitions.Any(x => x.ID.Equals(model.JobDefinitionId)))
            {
                var msg = $"There is no job associated with marketplace item '{item.DisplayName}'.";
                _logger.LogError($"{msg} Job Id {model.JobDefinitionId}");
                throw new ArgumentNullException(msg);
            }
            var jobDef = _dalJobDefinition.GetById(model.JobDefinitionId);

            //the rest of the fields are set in the dal
            var logItem = new JobLogModel()
            {
                Messages = new List<JobLogMessage>() {
                new JobLogMessage() {
                    Message = $"Starting job '{jobDef.Name}' for '{item.DisplayName}'...",
                    Created = DateTime.UtcNow
                }
            }
            };
            var logId = await _dalJobLog.Add(logItem, user.ID);

            Task backgroundTask = null;

            //slow task - kick off in background
            _ = Task.Run(async () =>
            {
                //kick off the importer
                //wrap in scope in the internal method so that we don't lose the scope of the dependency injected objects once the 
                //web api request completes and disposes of the import service object (and its module vars)
                try
                {
                    backgroundTask = ExecuteJobInternal(jobDef, model.Payload, logId, user);
                    await backgroundTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in background job engine.");
                    //update import log to indicate unexpected failure
                    var dalJobLog = GetJobLogDalIsolated();
                    CreateJobLogMessage(dalJobLog, logId, user.ID, "Unhandled exception in background job engine.", TaskStatusEnum.Failed);
                }
            });

            //return result async
            return logId;
        }

        /// <summary>
        /// Re-factor - Moved this to its own method to be shared by two different endpoints. Only other changes were
        /// returning result message model false instead of badRequest. 
        /// </summary>
        /// <param name="nodeSetXmlList"></param>
        /// <param name="authorToken"></param>
        /// <returns></returns>
        private async Task ExecuteJobInternal(JobDefinitionModel jobDef, string payload, string logId, UserModel user)
        {
            //var dalJobLog = GetJobLogDalIsolated();

            var sw = Stopwatch.StartNew();
            _logger.LogInformation($"JobFactory|ExecuteJobInternal|JobLogId:{logId}|Starting...");

            //wrap in scope so that we don't lose the scope of the dependency injected objects once the 
            //web api request completes and disposes of the import service object (and its module vars)
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                //initialize scoped services for DI, initialize job class
                var dalJobLog = scope.ServiceProvider.GetService<IDal<JobLog, JobLogModel>>();
                var logger = scope.ServiceProvider.GetService<ILogger<IJob>>();
                var httpFactory = scope.ServiceProvider.GetService<IHttpApiFactory>();

                try
                {
                    //instantiate job class
                    var job = InstantiateJob(jobDef.TypeName, logger, httpFactory, dalJobLog);

                    //initialize job
                    job.Initialize(jobDef, payload, logId, user);

                    //execute job async
                    await job.Run();
                }
                catch (Exception e)
                {
                    //log complete message to logger and abbreviated message to user. 
                    _logger.LogCritical(e, $"JobFactory|ExecuteJobInternal|JobLogId:{logId}|Error|{e.Message}");
                    //failed complete message
                    CreateJobLogMessage(dalJobLog, logId, user.ID, $"Job execution failed: {e.Message}.", TaskStatusEnum.Failed);
                }
            }
        }

        /// <summary>
        /// Instantiate a job. Expect the type name to result in a IJob type. 
        /// </summary>
        private IJob InstantiateJob(string typeName, params object?[]? args)
        {
            //instantiate job, call prep, call do work, call post process.
            var jobType = Type.GetType(typeName);
            if (jobType == null)
            {
                _logger.LogWarning($"JobFactory|ExecuteJob|Could not find job instance class: {typeName}");
                throw new ArgumentException($"Invalid job instance class {typeName}");
            }

            return (IJob)Activator.CreateInstance(jobType,args);

        }

        /// <summary>
        /// Create and isolate a 2nd context outside the scope of the main context and submit 
        /// log messages to it. 
        /// </summary>
        /// <returns></returns>
        private IDal<JobLog, JobLogModel> GetJobLogDalIsolated()
        {
            return _dalJobLog;
            //var configUtil = new ConfigUtil(_configuration);
            //ILogger<MongoDB.Driver.MongoClient> logger = new();
            //MongoClientGlobal client = new MongoClientGlobal(configUtil, logger);
            //IMongoRepository<JobLog, JobLogModel> repository = new MongoRepository<JobLog,JobLogModel>(client, );
            //return new JobLogDAL(repo);
        }

        private static void CreateJobLogMessage(IDal<JobLog, JobLogModel> dalJobLog, string logId, string userId,
            string message, TaskStatusEnum status)
        {
            var logItem = dalJobLog.GetById(logId);
            logItem.Status = status;
            if (status == TaskStatusEnum.Failed || status == TaskStatusEnum.Cancelled || status == TaskStatusEnum.Completed)
            {
                logItem.Completed = DateTime.UtcNow;
            }
            logItem.Messages.Add(new JobLogMessage() { Message = message, Created = DateTime.UtcNow });
            dalJobLog.Update(logItem, userId);
        }
    }

}
