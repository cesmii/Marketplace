using System;

using CESMII.Marketplace.Data.Entities;

namespace CESMII.Marketplace.JobManager.Models
{
    public abstract class JobBaseModel : AbstractEntity
    {
        public int StatusId { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
