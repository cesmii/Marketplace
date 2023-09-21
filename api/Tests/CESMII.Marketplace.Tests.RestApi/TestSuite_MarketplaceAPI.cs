using System.Net.Http.Json;
using CESMII.Marketplace.DAL.Models;
using CESMII.Marketplace.MongoDB;
using static System.Net.WebRequestMethods;

namespace CESMII.Marketplace.RestApi
{
    public class TestSuite_MarketplaceAPI
    {
        [Fact]
        public void MarketItemsAvailable_On_RestCall1()
        {
            string strHostHttps = "http://localhost:5000/api";
            HttpClient client = new HttpClient();

            string strTemp = utils.GetConnection("MARKETPLACE_URL1");
            if (!string.IsNullOrEmpty(strTemp) )
                strHostHttps = strTemp;

            Console.WriteLine($"MarketItemsAvailable_On_RestCall1: Attempting to connect to {strHostHttps}");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var items = GetFirstItem(client, $"{strHostHttps}/Marketplace/All");
            Assert.NotNull(items);

            int count = items.Count();
            Assert.NotEqual(0, count);
        }


        //[Fact]
        //public void MarketItemsAvailable_On_RestCall2()
        //{
        //    string strHostHttps = "https://localhost:5001/api";
        //    HttpClient client = new HttpClient();

        //    string strTemp = utils.GetConnection("MARKETPLACE_URL2");
        //    if (!string.IsNullOrEmpty(strTemp))
        //        strHostHttps = strTemp;

        //    Console.WriteLine($"MarketItemsAvailable_On_RestCall2: Attempting to connect to {strHostHttps}");
        //    client.DefaultRequestHeaders.Accept.Clear();
        //    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        //    var items = GetFirstItem(client, $"{strHostHttps}/Marketplace/All");
        //    Assert.NotNull(items);

        //    int count = items.Count();
        //    Assert.NotEqual(0, count);
        //}

        private MarketplaceItemModel[] GetFirstItem(HttpClient client, string strPath)
        {
            MarketplaceItemModel[] ReturnValue = null;

            try
            {
                var response = client.GetAsync(strPath).Result;
                if (response.IsSuccessStatusCode)
                {
                    var output = response.Content.ReadFromJsonAsync<MarketplaceItemModel[]>().Result;
                    ReturnValue = output;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            return ReturnValue;
        }
    }
}