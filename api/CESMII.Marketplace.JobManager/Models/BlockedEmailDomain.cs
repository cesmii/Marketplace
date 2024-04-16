using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.JobManager.Models
{
    /// <summary>
    /// This is a one off table used to check for blocked email domains on the Apogean blocked list.
    /// </summary>
    /// <seealso cref="CESMII.Marketplace.JobManager.Jobs.OnTimeEdgeApiConfig"/>
    public class BlockedEmailDomain : AbstractEntity
    {
        public string domain { get; set; }
    }
}
