using System;
using System.Threading.Tasks;
using CESMII.Marketplace.DAL.Models;

namespace CESMII.Marketplace.JobManager.Jobs
{
    public delegate Task<string> JobRunEventHandler(object sender, JobEventArgs e);

    /// <summary>
    /// This is the object used to pass data to the do work portion of the job execution wrapper object. 
    /// </summary>
    public class JobEventArgs : EventArgs
    {
        /// <summary>
        /// JSON formatted string
        /// </summary>
        public string Payload { get; set; }

        public UserModel User { get; set; }

        public JobDefinitionModel Config { get; set; }

        public string JobLogId { get; set; }
    }

}
