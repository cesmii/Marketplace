using System.Net.Http.Json;
using CESMII.Marketplace.DAL.Models;
using static System.Net.WebRequestMethods;

namespace CESMII.Marketplace.RestApi
{
    public class TestSuite_MarketplaceAPI
    {
        private static string strHostHttps = "https://localhost:5001/api";
        // private static string strHostHttps = "https://MyMarketplace:5001/api";

        [Fact]
        public void MarketItemsAvailable_On_Https_Api_Marketplace_All()
        {
            HttpClient client = new HttpClient();
            // client.BaseAddress = new Uri("https://localhost:5001/api");

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var items = GetFirstItem(client, $"{strHostHttps}/Marketplace/All");
            Assert.NotNull(items);

            int count = items.Count();
            Assert.NotEqual(0, count);
        }


        private MarketplaceItemModel[] GetFirstItem(HttpClient client, string strPath)
        {
            MarketplaceItemModel[] ReturnValue = null;
            var response = client.GetAsync(strPath).Result;
            if (response.IsSuccessStatusCode)
            {
                var output = response.Content.ReadFromJsonAsync<MarketplaceItemModel[]>().Result;
                ReturnValue = output;
            }

            return ReturnValue;
        }
    }
}