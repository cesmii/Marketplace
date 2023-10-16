using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.Marketplace.MongoDB
{
    public class utils
    {
        public static string GetConnection(string strVariable)
        {
            string strDefault = "http://localhost:5000/api";
            var c = Environment.GetEnvironmentVariable(strVariable);
            return (c == null) ? strDefault : c;
        }

    }
}
