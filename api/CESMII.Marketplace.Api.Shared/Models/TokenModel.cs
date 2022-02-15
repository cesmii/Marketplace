namespace CESMII.Marketplace.Api.Shared.Models
{
    using System;

    public class TokenModel
    {
        public string UserID { get; set; }

        public string UserName { get; set; }

        public string Token { get; set; }

        public DateTime ExpirationDate { get; set; }

        public bool IsImpersonating { get; set; }

        public string BaseUrl { get; set; }
    }
}