using System;
using System.ComponentModel.DataAnnotations;

namespace CESMII.Marketplace.DAL.Models
{
    public class StripeAuditLogModel :  AbstractModel
    {
        public string Type { get; set; }

        public string Message { get; set; }

        public string AdditionalInfo { get; set; }

        public UserSimpleModel CreatedBy { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime Created { get; set; }

        public CartModel CartModel { get; set; }

        public Stripe.Checkout.Session Session { get; set; }

        public Stripe.Checkout.SessionCreateOptions SessionCreateOptions { get; set; }
    }
}
