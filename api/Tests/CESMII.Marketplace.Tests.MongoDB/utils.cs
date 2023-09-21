using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.Marketplace.MongoDB
{
    internal class utils
    {
        public static string GetEnvString(string strInput)
        {
            var c = Environment.GetEnvironmentVariable(strInput);
            return (c == null) ? "" : c;
        }
    }
}
