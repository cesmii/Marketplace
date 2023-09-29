using System.Net.Http.Json;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.MongoDB;
using static System.Net.WebRequestMethods;

namespace CESMII.Marketplace.RestApi
{
    public class TestSuite_MarketplaceAPI
    {
        [Fact]
        public void MarketItemsAvailable_On_RestCall()
        {
            string strHostHttps; 
            HttpClient client = new HttpClient();

            strHostHttps = utils.GetConnection("MARKETPLACE_URL1");   // We expect to find a value like this http://localhost:5000/api
            Assert.NotNull(strHostHttps);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var items = GetFirstItem(client, $"{strHostHttps}/Marketplace/All");

            Assert.NotNull(items);

            int count = items.Count();
            Assert.NotEqual(0, count);
        }


        private MarketplaceItemModel[]? GetFirstItem(HttpClient client, string strPath)
        {
            MarketplaceItemModel[]? ReturnValue = null;

            try
            {
                var response = client.GetAsync(strPath).Result;
                if (response.IsSuccessStatusCode)
                {
                    var output = response.Content.ReadFromJsonAsync<MarketplaceItemModel[]>().Result;
                    if ( output != null )
                        ReturnValue = output;
                }
            }
            catch (Exception)
            {
                //Console.WriteLine($"::error::Exception in GetFirstItem: {ex.Message}");
            }

            return ReturnValue;
        }
    }
}
