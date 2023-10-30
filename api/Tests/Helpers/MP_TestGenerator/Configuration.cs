using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace MP_TestGenerator
{
    internal class Configuration
    {
        public static (string,string, string) GetConfig(string[] args)
        {
            IConfigurationRoot config = CreateConfiguration();

            string strConnection = "mongodb://testuser:password@localhost:27017"; //config["MongoDBSnapshot2023-07-19:ConnectionString"];
            string strDatabase = "test"; // config["MongoDBSnapshot2023-07-19:DatabaseName"];
            string strOutputFilePath = (args.Length > 0) ? args[0] : "C:\\CESMII\\Marketplace_TestData.cs";

            return (strConnection, strDatabase, strOutputFilePath);
        }

        private static IConfigurationRoot CreateConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true);

            var config = builder.Build();
            return config;
        }
    }
}
