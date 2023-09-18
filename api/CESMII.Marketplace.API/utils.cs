using System;

namespace CESMII.Marketplace.Api
{
    public class utils
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
