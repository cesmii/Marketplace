using System.Net.Http.Json;
using CESMII.Marketplace.DAL.Models;

namespace CESMII.Marketplace.RestApi
{
    public class TestSuite_MarketplaceAPI
    {
        [Fact]
        public void MarketItemsAvailable_On_Api_Marketplace_All()
        {
            HttpClient client = new HttpClient();
            // client.BaseAddress = new Uri("https://localhost:5001/api");

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var items = GetFirstItem(client, "https://localhost:5001/api/Marketplace/All");
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