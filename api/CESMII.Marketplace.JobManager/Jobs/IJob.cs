using CESMII.Marketplace.DAL.Models;
using System.Threading.Tasks;

namespace CESMII.Marketplace.JobManager.Jobs
{
    public interface IJob
    {
        /// <summary>
        /// Create a job log model and initialize basic data
        /// </summary>
        /// <returns>id of the job log model</returns>
        void Initialize(JobDefinitionModel jobDefinition, string payload, string logId, UserModel user);

        /// <summary>
        /// This is the method to call when you want to execute a job. This will take the payload data and other 
        /// key identifying information and run the code within the job.run method
        /// </summary>
        /// <param name="payload"></param>
        Task<string> Run();
    }

}