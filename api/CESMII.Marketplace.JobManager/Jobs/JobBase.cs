using System;
using System.Net.Mail;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using CESMII.Marketplace.DAL;
using CESMII.Marketplace.Data.Entities;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.Common;
using CESMII.Marketplace.Common.Enums;
using CESMII.Common.SelfServiceSignUp.Services;

namespace CESMII.Marketplace.JobManager.Jobs
{
    public abstract class JobBase : IJob, IDisposable
    {
        protected readonly ILogger<IJob> _logger;
        protected bool _disposed = false;
        protected readonly IHttpApiFactory _httpFactory;
        protected readonly IDal<JobLog, JobLogModel> _dalJobLog;
        protected readonly UserDAL _dalUser;
        protected readonly ConfigUtil _configUtil;
        protected readonly MailRelayService _mailRelayService;

        protected JobEventArgs _jobEventArgs { get; set; }

        public event JobRunEventHandler JobRun; // event

        public JobBase(
            ILogger<IJob> logger,
            IHttpApiFactory httpFactory,
            IDal<JobLog, JobLogModel> dalJobLog,
            UserDAL dalUser,
            IConfiguration configuration,
            MailRelayService mailRelayService)
        {
            _logger = logger;
            _httpFactory = httpFactory;
            _dalJobLog = dalJobLog;
            _dalUser = dalUser;
            _configUtil = new ConfigUtil(configuration);
            _mailRelayService = mailRelayService;
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

        protected void CreateJobLogMessage(string message, TaskStatusEnum status, bool isEncrypted = false)
        {
            var logItem = _dalJobLog.GetById(_jobEventArgs.JobLogId);
            logItem.Status = status;
            if (status == TaskStatusEnum.Failed || status == TaskStatusEnum.Cancelled || status == TaskStatusEnum.Completed)
            {
                logItem.Completed = DateTime.UtcNow;
            }
            logItem.Messages.Add(new JobLogMessage() { Message = message, Created = DateTime.UtcNow, isEncrypted = isEncrypted });
            _dalJobLog.Update(logItem, _jobEventArgs.User == null ? null : _jobEventArgs.User.ID);
        }

        protected void SetJobLogResponse(string responseData, string message, TaskStatusEnum status, bool isEncrypted = false)
        {
            var logItem = _dalJobLog.GetById(_jobEventArgs.JobLogId);
            logItem.Status = status;
            if (status == TaskStatusEnum.Failed || status == TaskStatusEnum.Cancelled || status == TaskStatusEnum.Completed)
            {
                logItem.Completed = DateTime.UtcNow;
            }
            //TBD - encrypt response data. 
            logItem.ResponseData = responseData;

            logItem.Messages.Add(new JobLogMessage() { Message = message, Created = DateTime.UtcNow, isEncrypted = isEncrypted });
            _dalJobLog.Update(logItem, _jobEventArgs.User == null ? null : _jobEventArgs.User.ID);
        }

        protected async Task<bool> SendEmail(string subject, string body)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_configUtil.MailSettings.MailFromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            return await _mailRelayService.SendEmail(message);
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
