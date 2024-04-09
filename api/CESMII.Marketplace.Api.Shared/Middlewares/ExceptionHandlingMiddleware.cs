using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CESMII.Marketplace.Api.Shared.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred: {ex}");

                // Set response status code
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                // Optionally, return a custom error response
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync($"{{ \"error\": \"{ex.Message}\" }}");
            }
        }
    }
}
