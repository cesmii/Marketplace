﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.Marketplace.Api.Shared.Utils
{
    public class Github
    {
        public static bool QueryEnvironmentBool(string strEnv, bool bDefault)
        {
            string strGithubLog = Environment.GetEnvironmentVariable(strEnv);
            if (!string.IsNullOrEmpty(strGithubLog))
                bool.TryParse(strGithubLog, out bDefault);
            return bDefault;
        }

        public static void Write_If(bool bDisplay, string strOutput)
        {
            Console.WriteLine(strOutput);
        }
    }
}
