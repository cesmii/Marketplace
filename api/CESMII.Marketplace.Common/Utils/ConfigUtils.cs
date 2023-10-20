namespace CESMII.Marketplace.Common
{
    using CESMII.Marketplace.Common.Models;
    using Microsoft.Extensions.Configuration;
    using System;

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
                var settings = new MarketplaceConfig();

                var c = _configuration.GetSection("MarketplaceSettings");
                c.Bind(settings);

                // Allow an override in support of unit test environment.
                string? strEnableCloudLibSearch = Environment.GetEnvironmentVariable("EnableCloudLibSearch");
                if (bool.TryParse(strEnableCloudLibSearch, out bool bResult))
                    settings.EnableCloudLibSearch = bResult;

                Console.WriteLine($"::notice::MarketplaceSettings.EnableCloudLibSearch={settings.EnableCloudLibSearch}");
                string strValue = (settings.DefaultItemTypeId == null) ? "null" : settings.DefaultItemTypeId;
                Console.WriteLine($"::notice::MarketplaceSettings.DefaultItemTypeId={strValue}");

                strValue = (settings.SmProfile == null) ? null : "Not Null!";
                Console.WriteLine($"::notice::MarketplaceSettings.SmProfile={strValue}");

                return settings;
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

    }
}
