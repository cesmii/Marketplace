namespace CESMII.Marketplace.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Common.Enums;

    public class JobLogModel : AbstractModel
    {
        public string Name { get; set; }

        public TaskStatusEnum Status { get; set; }

        public UserSimpleModel CreatedBy { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime Created { get; set; }

        public UserSimpleModel UpdatedBy { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? Updated { get; set; }
        public bool IsActive { get; set; }

        public DateTime? Completed { get; set; }

        public virtual List<JobLogMessage> Messages { get; set; }

        /// <summary>
        /// Field to store response data returned by a specific job execution 
        /// </summary>
        /// <remarks>This will be encrypted to protect any sensitive data that may be returned.</remarks>
        public string ResponseData { get; set; }
    }
}
