using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

using Microsoft.Extensions.Logging;
using CESMII.Marketplace.Common.Models;

namespace CESMII.Marketplace.Common
{
    public interface IHttpApiFactory 
    {
        Task<string> Run(HttpApiConfig config);
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

        public async Task<string> Run(HttpApiConfig config)
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
                client.BaseAddress = new Uri(config.BaseAddress);

                // Add an bearer token if necessary
                if (!string.IsNullOrEmpty(config.BearerToken))
                {
                    client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", config.BearerToken);
                }

                // Add another type of auth token if necessary. 
                if (!config.AuthToken.Equals(default(KeyValuePair<string, string>)))
                {
                    if (!string.IsNullOrEmpty(config.AuthToken.Key))
                    {
                        client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(config.AuthToken.Key, config.AuthToken.Value);
                    }
                    //if key is empty, we do a special workaround
                    else
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", config.AuthToken.Value);
                    }
                }

                // Add an Accept header for JSON format. "application/json"
                if (!string.IsNullOrEmpty(config.ContentType))
                {
                    client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(config.ContentType));
                }

                //prepare the request
                var queryString = !string.IsNullOrEmpty(config.QueryString) ? "?" + config.QueryString : "" ;
                var urlFinal = $"{config.Url}{queryString}";
                using (var requestMessage = new HttpRequestMessage(config.Method, urlFinal))
                {
                    //add the body as JSON content
                    requestMessage.Content = config.Body;

                    //add headers passed in
                    if (config.Headers != null)
                    {
                        foreach (var h in config.Headers)
                        {
                            requestMessage.Headers.Add(h.Key, h.Value);
                        }
                    }

                    //call the api
                    HttpResponseMessage response = await client.SendAsync(requestMessage);

                    if (response.IsSuccessStatusCode)
                    {
                        // Return the response body.
                        var data = response.Content.ReadAsStringAsync().Result;  //Make sure to add a reference to System.Net.Http.Formatting.dll
                        _logger.LogInformation($"HttpApiFactory||Run||Success...returning response");
                        return data;
                    }
                    else
                    {
                        var msg = $"{(int)response.StatusCode}-{response.ReasonPhrase}";
                        throw new HttpRequestException(msg);
                    }
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
