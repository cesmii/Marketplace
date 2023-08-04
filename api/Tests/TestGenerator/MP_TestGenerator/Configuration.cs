﻿using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace MP_TestGenerator
{
    internal class Configuration
    {
        public static (string,string, string) GetConfig(string[] args)
        {
            IConfigurationRoot config = CreateConfiguration();

            string strConnection = config["MongoDBSnapshot2023-07-19:ConnectionString"];
            string strDatabase = config["MongoDBSnapshot2023-07-19:DatabaseName"];
            string strOutputFilePath = (args.Length > 0) ? args[0] : "C:\\CESMII.Testing\\MP\\Marketplace_TestData.cs";

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
