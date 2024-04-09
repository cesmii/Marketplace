namespace CESMII.Marketplace.Common
{
    using Microsoft.Extensions.Configuration;
    using CESMII.Marketplace.Common.Models;

    public class ConfigUtil
    {
        private readonly IConfiguration _configuration;

        public ConfigUtil(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public GeneralConfig GeneralSettings
        {
            get
            {
                var result = new GeneralConfig();
                _configuration.GetSection("GeneralSettings").Bind(result);
                return result;
            }
        }

        public MongoDBConfig MongoDBSettings
        {
            get
            {
                var result = new MongoDBConfig();
                _configuration.GetSection("MongoDBSettings").Bind(result);
                return result;
            }
        }


        public PasswordConfig PasswordConfigSettings
        {
            get
            {
                var result = new PasswordConfig();
                _configuration.GetSection("PasswordSettings").Bind(result);
                return result;
            }
        }

        public CorsConfig CorsSettings
        {
            get
            {
                var result = new CorsConfig();
                _configuration.GetSection("CorsSettings").Bind(result);
                return result;
            }
        }

        public AuthenticationConfig AuthenticationSettings
        {
            get
            {
                var result = new AuthenticationConfig();
                _configuration.GetSection("AuthenticationSettings").Bind(result);
                return result;
            }
        }
        
        public MailConfig MailSettings
        {
            get
            {
                var result = new MailConfig();
                _configuration.GetSection("MailSettings").Bind(result);
                return result;
            }
        }

        public JWTConfig JWTSettings
        {
            get
            {
                var result = new JWTConfig();
                _configuration.GetSection("JwtSettings").Bind(result);
                return result;
            }
        }

        public MarketplaceConfig MarketplaceSettings
        {
            get
            {
                var result = new MarketplaceConfig();
                _configuration.GetSection("MarketplaceSettings").Bind(result);
                return result;
            }
        }

        public CloudLibraryConfig CloudLibSettings
        {
            get
            {
                var result = new CloudLibraryConfig();
                _configuration.GetSection("CloudLibConfig").Bind(result);
                return result;
            }
        }

        public StripeConfig StripeSettings
        {
            get
            {
                var result = new StripeConfig();
                _configuration.GetSection("StripeSettings").Bind(result);
                return result;
            }
        }

    }
}
