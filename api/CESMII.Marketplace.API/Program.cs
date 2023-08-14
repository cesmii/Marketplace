using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog.Web;

namespace CESMII.Marketplace.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // During test runs, our connection string arrives from GITHUB secrets on the command line.
            if (args.Length > 0 && args[0] != null)
            {
                string strInput = args[0];
                if (strInput.StartsWith("mongodb:"))
                    Startup.strTextMongoDBConnectionString = strInput;
            }
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                 .ConfigureLogging(logging =>
                 {
                     logging.ClearProviders();
                     logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                 })
                // Use NLog to provide ILogger instances.
                .UseNLog();
    }
}
