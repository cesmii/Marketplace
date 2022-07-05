using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

using Microsoft.Extensions.Logging;
using CESMII.Marketplace.Common.Models;

namespace CESMII.Marketplace.Common
{
    public interface IHttpApiFactory 
    {
        Task<string> Run(HttpApiConfig config, string actionType = "Post");
    }
    
    public class HttpApiFactory : IHttpApiFactory
    {
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly ILogger<HttpApiFactory> _logger;

        public HttpApiFactory(IHttpClientFactory httpClientFactory, ILogger<HttpApiFactory> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> Run(HttpApiConfig config, string actionType = "Post")
        {

            //log the final query as info
            _logger.LogInformation($"HttpApiFactory||Run||Url: {config.Url}");
            if (!string.IsNullOrEmpty(config.QueryString))
            { 
                _logger.LogInformation($"HttpApiFactory||Run||Query String: {config.QueryString}");
            }

            //make the API call
            HttpClient client = _httpClientFactory.CreateClient();
            try
            {
                client.BaseAddress = new Uri(config.Url);

                // Add an bearer token if necessary
                if (!string.IsNullOrEmpty(config.BearerToken))
                {
                    client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", config.BearerToken);
                }

                // Add an Accept header for JSON format. "application/json"
                if (!string.IsNullOrEmpty(config.ContentType))
                {
                    client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(config.ContentType));
                }

                // List data response.
                HttpResponseMessage response;

                if (config.IsPost)
                {
                    var data = new StringContent(config.Body);
                    // Blocking call! Program will wait here until a response is received or a timeout occurs.
                    response = await client.PostAsync(config.QueryString, data);
                }
                else
                {
                    // Blocking call! Program will wait here until a response is received or a timeout occurs.
                    response = await client.GetAsync(config.QueryString);
                }

                if (response.IsSuccessStatusCode)
                {
                    // Return the response body.
                    var data = response.Content.ReadAsStringAsync().Result;  //Make sure to add a reference to System.Net.Http.Formatting.dll
                    _logger.LogInformation($"HttpApiFactory||Run||Success...returning response");
                    return data;
                }
                else
                {
                    _logger.LogWarning($"HttpApiFactory||Run||Unexpected Response: {response.StatusCode}::{response.ReasonPhrase}");
                    return null;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"HttpApiFactory||Run||An Error occurred: {ex.Message}");
                throw;
            }
            finally
            {
                // Dispose once all HttpClient calls are complete. This is not necessary if the containing object will be disposed of; for example in this case the HttpClient instance will be disposed automatically when the application terminates so the following call is superfluous.
                client.Dispose();
            }
        }

    }
}
