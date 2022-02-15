namespace CESMII.Marketplace.DAL.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public abstract class AbstractModel
    {
        public string ID { get; set; }
    }

    public abstract class AbstractMarketplaceModel : AbstractModel
    {
        public UserSimpleModel CreatedBy { get; set; }
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime Created { get; set; }
        public UserSimpleModel UpdatedBy { get; set; }
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? Updated { get; set; }
        public bool IsActive { get; set; }
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? PublishDate { get; set; }
    }

}