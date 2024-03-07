namespace CESMII.Marketplace.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using CESMII.Marketplace.Data.Entities;
    using CESMII.Marketplace.Common.Enums;

    public class CartModel : AbstractModel
    {
        public string Name { get; set; }

        public CartStatusEnum Status { get; set; } = CartStatusEnum.Pending;

        public string CreatedById { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime Created { get; set; }

        public string UpdatedById { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? Updated { get; set; }
        public bool IsActive { get; set; }

        public DateTime? Completed { get; set; }

        public virtual List<CartItemModel> Items { get; set; }
    }
}
