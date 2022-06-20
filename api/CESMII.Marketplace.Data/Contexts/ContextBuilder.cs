using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CESMII.Marketplace.Data.Contexts
{
    /// <summary>
    /// Context Builder is utilized for Unit Tests project. Does not affect runtime otherwise but the context while unit testing cannot be created via DI.
    /// </summary>
    public class ContextBuilder
    {
        private readonly IConfiguration _configuration;

        public ContextBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

    }
}