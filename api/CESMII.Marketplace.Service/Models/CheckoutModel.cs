using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CESMII.Marketplace.DAL.Models;

namespace CESMII.Marketplace.Service.Models
{

    public class CheckoutModel
    {
        public CartModel Cart { get; set; }

        public string SuccessUrl { get; set; }

        public string CancelUrl { get; set; }
    }
}
