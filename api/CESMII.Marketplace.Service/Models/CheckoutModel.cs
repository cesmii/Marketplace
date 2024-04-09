using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CESMII.Marketplace.DAL.Models;

namespace CESMII.Marketplace.Service.Models
{

    //public class CheckoutModel
    //{
    //    public CartModel Cart { get; set; }

    //    public string ReturnUrl { get; set; }
    //}

    /// <summary>
    /// Returned from Stripe init checkout call
    /// </summary>
    public class CheckoutInitModel
    {
        public string SessionId { get; set; }
        public string ApiKey { get; set; }
    }
}
