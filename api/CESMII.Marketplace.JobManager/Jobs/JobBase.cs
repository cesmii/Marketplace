﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.Data.Repositories;
using CESMII.Marketplace.JobManager.Models;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Enums;

namespace CESMII.Marketplace.JobManager.Jobs
{
    public abstract class JobBase : IJob, IDisposable
    {
        protected readonly ILogger<IJob> _logger;
        protected bool _disposed = false;
        protected readonly IHttpApiFactory _httpFactory;
        protected readonly IDal<JobLog, JobLogModel> _dalJobLog;

        protected JobEventArgs _jobEventArgs { get; set; }

        public event JobRunEventHandler JobRun; // event

        public JobBase(
            ILogger<IJob> logger,
            IHttpApiFactory httpFactory,
            IDal<JobLog, JobLogModel> dalJobLog)
        {
            _logger = logger;
            _httpFactory = httpFactory;
            _dalJobLog = dalJobLog;
        }

        public virtual void Initialize(JobDefinitionModel jobDefinition, string payload, string logId, UserModel user)
        {
            _jobEventArgs = new JobEventArgs() {
                Config = jobDefinition,
                Payload = payload,
                JobLogId = logId,
                User = user
            };
        }

        /// <summary>
        /// Async version of the DoWork method. 
        /// This will be serialized and executed when the job is performed. 
        /// </summary>
        /// <param name="e"></param>
        public virtual async Task<string> Run()
        {
            //dervied classes will do the work then call here to trigger onComplete notification
            return await this.OnJobRun(this, this._jobEventArgs);
        }

        protected virtual Task<string> OnJobRun(object sender, JobEventArgs e)
        {
            //if not null then call delegate
            return JobRun?.Invoke(sender, e);
        }

        protected void CreateJobLogMessage(string message, TaskStatusEnum status)
        {
            var logItem = _dalJobLog.GetById(_jobEventArgs.JobLogId);
            logItem.Status = status;
            if (status == TaskStatusEnum.Failed || status == TaskStatusEnum.Cancelled || status == TaskStatusEnum.Completed)
            {
                logItem.Completed = DateTime.UtcNow;
            }
            logItem.Messages.Add(new JobLogMessage() { Message = message, Created = DateTime.UtcNow });
            _dalJobLog.Update(logItem, _jobEventArgs.User.ID);
        }

        protected void SetJobLogResponse(string responseData, string message, TaskStatusEnum status)
        {
            var logItem = _dalJobLog.GetById(_jobEventArgs.JobLogId);
            logItem.Status = status;
            if (status == TaskStatusEnum.Failed || status == TaskStatusEnum.Cancelled || status == TaskStatusEnum.Completed)
            {
                logItem.Completed = DateTime.UtcNow;
            }
            //TBD - encrypt response data. 
            logItem.ResponseData = responseData;

            logItem.Messages.Add(new JobLogMessage() { Message = message, Created = DateTime.UtcNow });
            _dalJobLog.Update(logItem, _jobEventArgs.User.ID);
        }

        /// <summary>
        /// Override this in the descendant classes to handle disposal of unmanaged resources.
        /// </summary>
        public virtual void Dispose()         
        {
            //only dispose once
            if (_disposed) return;
            
            //do clean up of unmanaged resources
            _disposed = true;
        }
    }

}
