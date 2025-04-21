
namespace CESMII.Marketplace.Api.Shared.Models
{
    using CESMII.Marketplace.Common.Models;
    using CESMII.Marketplace.DAL.Models;

    public class PurchaseNotificationModel
    {
        public CartItemModel CartItem { get; set; }
        public UserCheckoutModel CheckoutUser { get; set; }
        public string BaseUrl { get; set; }
    }

    public class PurchaseReceiptModel
    {
        public CartModel Cart { get; set; }
        public string BaseUrl { get; set; }
    }

}
