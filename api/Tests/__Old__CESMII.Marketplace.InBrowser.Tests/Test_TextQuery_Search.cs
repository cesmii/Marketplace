namespace CESMII.Marketplace
{
    using CESMII.Marketplace.UI.Tests;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using System.Diagnostics;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.


    [TestFixture]
    public class Test_TextSearch
    {
        // Note: The URL requests "Sm-App" and "Sm-Hardware" be included in the queries.
        private string strStartUrl = "http://localhost:3000/library?sm=sm-app,sm-hardware&p=1&t=10";
        // private string strStartUrl = "https://marketplace-front-stage.azurewebsites.net/library?sm=sm-app,sm-hardware&p=1&t=10";  // Staging
        // private string strStartUrl = "https://marketplace-front.azurewebsites.net/library?sm=sm-app,sm-hardware&p=1&t=10";     // Production
        private IWebDriver? driver = null;
        public IDictionary<string, object> vars { get; private set; }

        // private Dictionary<string, int> dictPublisherItems = new Dictionary<string, int>();

        private bool bStaging = true;
        private const int c50 = 100;
        private const int c250 = 300;
        private const int c500 = 800;
        private const int c1000 = 1500;

        private static int cMaxItems = -1;

    //    private static string[] astrPublishers = null;

        private IJavaScriptExecutor js;
        [SetUp]
        public void SetUp()
        {
            utils.Console_WriteLine($"Setup: Entering function for Test_TextSearch");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--headless");
                driver = new ChromeDriver(options);
                driver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 10); // 10 seconds
            }
            catch (Exception ex)
            {
                throw new Exception($"Error accessing ChromeDriver: {ex.Message}");
            }
            js = (IJavaScriptExecutor)driver;
            vars = new Dictionary<string, object>();

            bStaging = true; // strStartUrl.Contains("-stage.");

            // Open the website
            driver.Navigate().GoToUrl(strStartUrl);

            // Fetch max number of items. When we query a count, we use this
            // values as a "not quite ready" flag.
            if (cMaxItems == -1)
            {
                for (int iRetry = 0; iRetry < 10 && cMaxItems == -1; iRetry++)
                {
                    cMaxItems = utils.QueryItemCount(driver, cMaxItems, "SysInit");
                }
            }

            // Make sure "Show More" is visible for all three groups
            utils.MarketplaceSeeAllSeeLess(driver, 1, true);
            utils.MarketplaceSeeAllSeeLess(driver, 2, true);
            utils.MarketplaceSeeAllSeeLess(driver, 3, true);

            sw.Stop();
            utils.Console_WriteLine($"Setup: Completed in {sw.ElapsedMilliseconds} ms");
        }


        [TearDown]
        protected void TearDown()
        {
            utils.Console_WriteLine($"TearDown: Entering function");
            if (driver != null)
                driver.Quit();
        }

        // Test Fixture Start
        // ============================================================
        // ============================================================
        // Tests of type 'textquery'
        [TestCase("textquery", "Category", "Air Compressing", 2)]
        [TestCase("textquery", "Category", "Analytics", 10)]
        [TestCase("textquery", "Category", "Analytics Lifecycle", 1)]
        [TestCase("textquery", "Category", "APS", 4)]
        [TestCase("textquery", "Category", "Asset Health", 5)]
        [TestCase("textquery", "Category", "Asset Monitoring", 8)]
        [TestCase("textquery", "Category", "Asset Optimization", 5)]
        [TestCase("textquery", "Category", "Asset Performance Management", 6)]
        [TestCase("textquery", "Category", "Augmented Reality", 4)]
        [TestCase("textquery", "Category", "Blowing", 2)]
        [TestCase("textquery", "Category", "Chilling", 2)]
        [TestCase("textquery", "Category", "CNC Monitoring", 3)]
        [TestCase("textquery", "Category", "Communications", 1)]
        [TestCase("textquery", "Category", "Continuous Improvement", 8)]
        [TestCase("textquery", "Category", "Continuous Manufacturing Intelligence", 2)]
        [TestCase("textquery", "Category", "Continuous Monitoring", 5)]
        [TestCase("textquery", "Category", "Continuous Supply Chain Intelligence", 5)]
        [TestCase("textquery", "Category", "Cost of Quality", 3)]
        [TestCase("textquery", "Category", "Data Analytics", 4)]
        [TestCase("textquery", "Category", "Data Collection", 5)]
        [TestCase("textquery", "Category", "Data Management", 8)]
        [TestCase("textquery", "Category", "Digital Manufacturing", 5)]
        [TestCase("textquery", "Category", "Digitized Quality Checks", 2)]
        [TestCase("textquery", "Category", "Edge Computing", 5)]
        [TestCase("textquery", "Category", "Energy Efficiency", 3)]
        [TestCase("textquery", "Category", "Energy Monitoring", 6)]
        [TestCase("textquery", "Category", "Enterprise Data Management", 4)]
        [TestCase("textquery", "Category", "Enterprise Integration", 4)]
        [TestCase("textquery", "Category", "Heating", 2)]
        [TestCase("textquery", "Category", "Historian", 4)]
        [TestCase("textquery", "Category", "IIoT", 11)]
        [TestCase("textquery", "Category", "In-House Quality Inspections", 2)]
        [TestCase("textquery", "Category", "Industrial Software", 6)]
        [TestCase("textquery", "Category", "Industry 4.0", 7)]
        [TestCase("textquery", "Category", "Inventory Management", 3)]
        [TestCase("textquery", "Category", "Inventory Optimization", 4)]
        [TestCase("textquery", "Category", "Logistics and Yard Solutions", 2)]
        [TestCase("textquery", "Category", "Machine Connectivity", 4)]
        [TestCase("textquery", "Category", "Machine Learning", 9)]
        [TestCase("textquery", "Category", "Machine Monitoring", 4)]
        [TestCase("textquery", "Category", "Machine Vision", 3)]
        [TestCase("textquery", "Category", "Machining", 3)]
        [TestCase("textquery", "Category", "Maintenance", 9)]
        [TestCase("textquery", "Category", "Manufacturing Operations Solutions", 2)]
        [TestCase("textquery", "Category", "Material Analysis Solutions", 2)]
        [TestCase("textquery", "Category", "Material Traceability", 2)]
        [TestCase("textquery", "Category", "MES", 10)]
        [TestCase("textquery", "Category", "Molding", 2)]
        [TestCase("textquery", "Category", "OEE", 6)]
        [TestCase("textquery", "Category", "OT Data Lake", 2)]
        [TestCase("textquery", "Category", "Palletizing", 1)]
        [TestCase("textquery", "Category", "Planning and Scheduling", 2)]
        [TestCase("textquery", "Category", "Plant Reliability", 3)]
        [TestCase("textquery", "Category", "Power Factor", 4)]
        [TestCase("textquery", "Category", "Power Quality", 3)]
        [TestCase("textquery", "Category", "Predictive Maintenance", 8)]
        [TestCase("textquery", "Category", "Process Optimization", 7)]
        [TestCase("textquery", "Category", "Product Quality", 1)]
        [TestCase("textquery", "Category", "Production Quality", 1)]
        [TestCase("textquery", "Category", "Quality Assessment", 2)]
        [TestCase("textquery", "Category", "Quality Assurance", 6)]
        [TestCase("textquery", "Category", "Quality Traceability", 2)]
        [TestCase("textquery", "Category", "Remote Monitoring", 5)]
        [TestCase("textquery", "Category", "Renewables Monitoring", 2)]
        [TestCase("textquery", "Category", "Short Interval Control", 2)]
        [TestCase("textquery", "Category", "Smart Manufacturing", 7)]
        [TestCase("textquery", "Category", "Supplier Inventory Visibility", 2)]
        [TestCase("textquery", "Category", "Supplier Quality", 2)]
        [TestCase("textquery", "Category", "Supply Chain", 3)]
        [TestCase("textquery", "Category", "Supply Chain Automation", 2)]
        [TestCase("textquery", "Category", "Supply Chain Management", 2)]
        [TestCase("textquery", "Category", "Supply Chain Optimization", 5)]
        [TestCase("textquery", "Category", "Warranty Management", 1)]
        [TestCase("textquery", "Category", "Workflow Mapping", 3)]
        [TestCase("textquery", "Vertical", "Academia", 4)]
        [TestCase("textquery", "Vertical", "Aerospace", 13)]
        [TestCase("textquery", "Vertical", "Agriculture", 6)]
        [TestCase("textquery", "Vertical", "Assembly", 3)]
        [TestCase("textquery", "Vertical", "Automotive", 14)]
        [TestCase("textquery", "Vertical", "Biotechnology", 1)]
        [TestCase("textquery", "Vertical", "Building Materials and Supplies", 4)]
        [TestCase("textquery", "Vertical", "Cement", 7)]
        [TestCase("textquery", "Vertical", "Chemical", 9)]
        [TestCase("textquery", "Vertical", "CNC", 7)]
        [TestCase("textquery", "Vertical", "Consumer and Paper Goods", 6)]
        [TestCase("textquery", "Vertical", "Consumer Products", 4)]
        [TestCase("textquery", "Vertical", "Defense", 8)]
        [TestCase("textquery", "Vertical", "Discrete Manufacturing", 7)]
        [TestCase("textquery", "Vertical", "Distribution", 6)]
        [TestCase("textquery", "Vertical", "Electronics", 5)]
        [TestCase("textquery", "Vertical", "Energy", 8)]
        [TestCase("textquery", "Vertical", "Energy (Oil & Gas)", 12)]
        [TestCase("textquery", "Vertical", "Food & Beverage", 9)]
        [TestCase("textquery", "Vertical", "Forestry", 4)]
        [TestCase("textquery", "Vertical", "Foundry", 4)]
        [TestCase("textquery", "Vertical", "Government", 8)]
        [TestCase("textquery", "Vertical", "Greenhouses", 3)]
        [TestCase("textquery", "Vertical", "Heavy Industry", 9)]
        [TestCase("textquery", "Vertical", "Hospitality", 3)]
        [TestCase("textquery", "Vertical", "Industrial Gas", 5)]
        [TestCase("textquery", "Vertical", "Intelligence", 3)]
        [TestCase("textquery", "Vertical", "Life Sciences", 5)]
        [TestCase("textquery", "Vertical", "Lumber", 5)]
        [TestCase("textquery", "Vertical", "Machine Shops", 7)]
        [TestCase("textquery", "Vertical", "Manufacturing", 8)]
        [TestCase("textquery", "Vertical", "Medical Devices", 3)]
        [TestCase("textquery", "Vertical", "Metals", 8)]
        [TestCase("textquery", "Vertical", "Mining", 11)]
        [TestCase("textquery", "Vertical", "Packaging", 4)]
        [TestCase("textquery", "Vertical", "Personal Care", 6)]
        [TestCase("textquery", "Vertical", "Petrochemical", 6)]
        [TestCase("textquery", "Vertical", "Pharmaceuticals", 6)]
        [TestCase("textquery", "Vertical", "Plastics", 7)]
        [TestCase("textquery", "Vertical", "Plastics and Rubber", 2)]
        [TestCase("textquery", "Vertical", "Power", 3)]
        [TestCase("textquery", "Vertical", "Process Manufacturing", 7)]
        [TestCase("textquery", "Vertical", "Pulp & Paper", 6)]
        [TestCase("textquery", "Vertical", "Refining", 6)]
        [TestCase("textquery", "Vertical", "Remanufacturing", 4)]
        [TestCase("textquery", "Vertical", "Retailers", 3)]
        [TestCase("textquery", "Vertical", "Sawmills", 5)]
        [TestCase("textquery", "Vertical", "Seafood", 3)]
        [TestCase("textquery", "Vertical", "Semiconductor", 7)]
        [TestCase("textquery", "Vertical", "Small-Medium Enterprises", 9)]
        [TestCase("textquery", "Vertical", "Steel", 9)]
        [TestCase("textquery", "Vertical", "Telecom", 5)]
        [TestCase("textquery", "Vertical", "Textile", 7)]
        [TestCase("textquery", "Vertical", "Utilities", 11)]
        [TestCase("textquery", "Publisher", "5G Technologies USA Ltd.", 3)]
        [TestCase("textquery", "Publisher", "Adapdix", 1)]
        [TestCase("textquery", "Publisher", "Amatrol", 1)]
        [TestCase("textquery", "Publisher", "Augury", 1)]
        [TestCase("textquery", "Publisher", "Beeond, Inc.", 1)]
        [TestCase("textquery", "Publisher", "C5MI Insight LLC ", 3)]
        [TestCase("textquery", "Publisher", "CESMII - The Smart Manufacturing Institute", 1)]
        [TestCase("textquery", "Publisher", "CoreWatts", 1)]
        [TestCase("textquery", "Publisher", "Emerson", 1)]
        [TestCase("textquery", "Publisher", "Falkonry Inc.", 1)]
        [TestCase("textquery", "Publisher", "Litmus Automation", 2)]
        [TestCase("textquery", "Publisher", "SAS", 2)]
        [TestCase("textquery", "Publisher", "ShelfAware", 1)]
        [TestCase("textquery", "Publisher", "SymphonyAI Industrial", 1)]
        [TestCase("textquery", "Publisher", "ThinkIQ", 1)]
        [TestCase("textquery", "Publisher", "Toward Zero", 1)]
        // Test Fixture End

        public void QueryTypeItemIntoQueryBox(string strTestType, string strItemType, string strItemName, int cExpectedStaging, int cExpectedProduction)
        { 
            utils.Console_WriteLine($"QueryTypeItemIntoQueryBox: Entering function strPublisher={strItemName}");

            if (strTestType != "textquery")
                throw new Exception($"QueryTypeItemIntoQueryBox: Expected test type of 'checkbox' not found. Instead found {strTestType}");

            if (driver != null)
            {
                int cExpected = (bStaging) ? cExpectedStaging : cExpectedProduction;
                int cFound = -1;
                if (cExpected > 0)
                {
                    for (int iRetry = 0; iRetry < 50; iRetry++)
                    {
                        utils.Console_WriteLine($"For {strItemType} ({strItemName}) iRetry={iRetry} Send Keys");
                        utils.SendKeysToQueryBox(driver, strItemName, 5);
                        System.Threading.Thread.Sleep(iRetry * 10);
                        int cTemp = utils.QueryItemCount(driver, cMaxItems, strItemName);
                        if (cTemp != cMaxItems && cTemp != -1)
                        {
                            // Valid value
                            cFound = cTemp;
                            break;
                        }
                    }
                    utils.Console_WriteLine($"For {strItemType} ({strItemName}), Item count = {cFound}");

                }
                if (cExpected == 0)
                {
                    utils.Console_WriteLine($"QueryTypeItemIntoQueryBox: Skipped {strItemType} {strItemName}, which has no records.");
                    Assert.Pass();
                }
                else if (cFound == cExpected)
                {
                    utils.Console_WriteLine($"QueryTypeItemIntoQueryBox: Pass. {strItemType}:{strItemName}");
                    Assert.Pass();
                }
                else
                {
                    utils.Console_WriteLine($"QueryTypeItemIntoQueryBox: Fail. {strItemType}:{strItemName}, ItemsOnWebPage:{cFound} versus Expected:{cExpected}");
                    Assert.Fail();
                }
            }
        }

        //private IWebElement GetPublisher(int iGroup)
        //{
        //    // Make sure the "+ Sell all / - See less" is expanded
        //    utils.MarketplaceSeeAllSeeLess(driver, iGroup, true);

        //    // Select Nth publisher in list
        //    IWebElement iweReturn = null;
        //    int nItem = iGroup + 1;
        //    if (nItem < 8)
        //    {
        //        for (int iRetry = 5; iRetry > 0 && iweReturn == null; iRetry--)
        //        {
        //            try
        //            {
        //                iweReturn = driver.FindElement(By.CssSelector($".info-section:nth-child(3) .selectable:nth-child({nItem})")); // .Click();
        //            }
        //            catch (Exception ex)
        //            {
        //            }

        //            if (iweReturn == null)
        //            {
        //                utils.Console_WriteLine($"GetPublisher [{iGroup}] - iRetry={iRetry}");
        //                System.Threading.Thread.Sleep(c500);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        for (int iRetry = 5; iRetry > 0 && iweReturn == null; iRetry--)
        //        {
        //            try
        //            {
        //                iweReturn = driver.FindElement(By.CssSelector($".info-section:nth-child(3) .selectable:nth-child({nItem}) span")); // .Click();
        //            }
        //            catch (Exception ex)
        //            {
        //            }

        //            if (iweReturn == null)
        //            {
        //                utils.Console_WriteLine($"GetPublisher [{iGroup}] - iRetry={iRetry}");
        //                System.Threading.Thread.Sleep(c500);
        //            }
        //        }
        //    }

        //    return iweReturn;
        //}

        //private int QueryItemCount()
        //{
        //    int cItems = 0;

        //    try
        //    {
        //        var eleItemCounter = driver.FindElement(By.CssSelector(".text-left"));
        //        System.Threading.Thread.Sleep(c1000);
        //        if (eleItemCounter == null)
        //        {
        //            utils.Console_WriteLine($"QueryItemCount: null value returned");
        //            return -1;
        //        }

        //        var str2 = eleItemCounter.Text;
        //        var ai = str2.Split(new char[] { ' ' });
        //        if (ai.Length == 2)
        //        {
        //            int.TryParse(ai[0], out cItems);
        //        }
        //    }
        //    catch (Exception ex)
        //    { }

        //    return cItems;
        //}
    }
}
