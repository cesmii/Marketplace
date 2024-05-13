namespace CESMII.Marketplace.Common.Models
{

    public class StripeConfig
    {
        public string PublishKey { get; set; }
        public string SecretKey { get; set; }
        public string WebhookSecretKey { get; set; }
        /// <summary>
        /// Allow config setting to turn off checkout at a global level
        /// </summary>
        public bool EnableCheckout { get; set; }
        /// <summary>
        /// Allow config setting to turn off product updates at a global level
        /// </summary>
        public bool EnableProductUpdates { get; set; }
    }
}
