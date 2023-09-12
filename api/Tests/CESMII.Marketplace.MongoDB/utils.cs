using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.Marketplace.MongoDB
{
    internal class utils
    {
        public static string GetConnection()
        {
            var c = Environment.GetEnvironmentVariable("TEST_CONNECTIONSTRING");
            return (c == null) ? "" : c;
        }

        public static string GetDatabase() 
        {
            var d = Environment.GetEnvironmentVariable("TEST_DATABASE_NAME");
            return (d == null) ? "" : d;
        }
    }
}
