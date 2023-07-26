namespace CESMII.Marketplace.InBrowser.Tests
{
    using CESMII.Marketplace.UI.Tests;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using System.Collections;
    using System.Diagnostics;

    [TestFixture]
    internal class Test_CheckboxSearch
    {
        // Note: The URL requests "Sm-App" and "Sm-Hardware" be included in the queries.
        private string strStartUrl = "http://localhost:3000/library?sm=sm-app,sm-hardware&p=1&t=10";
        // private string strStartUrl = "https://marketplace-front-stage.azurewebsites.net/library?sm=sm-app,sm-hardware&p=1&t=10";  // Staging
        // private string strStartUrl = "https://marketplace-front.azurewebsites.net/library?sm=sm-app,sm-hardware&p=1&t=10";     // Production
        private IWebDriver? driver = null;

        private bool bStaging = true;
        private const int c50 = 100;
        private const int c250 = 300;
        private const int c500 = 800;
        private const int c1000 = 1500;

        private static int cMaxItems = -1;

        private static bool bNeedToRun = true;

        private IJavaScriptExecutor js;
        [SetUp]
        public void SetUp()
        {
            utils.Console_WriteLine($"Setup: Entering function for Test_CheckboxSearch");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("--headless");
                driver = new ChromeDriver(options);
                driver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 2); // 10 seconds
            }
            catch (Exception ex)
            {
                throw new Exception($"Error accessing ChromeDriver: {ex.Message}");
            }
            js = (IJavaScriptExecutor)driver;
            //vars = new Dictionary<string, object>();

            bStaging = true; // strStartUrl.Contains("-stage.");

//            if (bNeedToRun)
            {
                // Open the website

                string str1 = driver.Title;
                driver.Navigate().GoToUrl(strStartUrl);
                string str2 = driver.Title;

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

                bNeedToRun = false;
            }


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


        #region MyTests
        // Test Fixture Start
        // Tests of type 'checkbox'
        [TestCase("checkbox", "Category", "Air Compressing", 2)]
        [TestCase("checkbox", "Category", "Analytics", 6)]
        [TestCase("checkbox", "Category", "Analytics Lifecycle", 1)]
        [TestCase("checkbox", "Category", "APS", 2)]
        [TestCase("checkbox", "Category", "Asset Health", 5)]
        [TestCase("checkbox", "Category", "Asset Monitoring", 8)]
        [TestCase("checkbox", "Category", "Asset Optimization", 5)]
        [TestCase("checkbox", "Category", "Asset Performance Management", 5)]
        [TestCase("checkbox", "Category", "Augmented Reality", 3)]
        [TestCase("checkbox", "Category", "Blowing", 2)]
        [TestCase("checkbox", "Category", "Chilling", 2)]
        [TestCase("checkbox", "Category", "CNC Monitoring", 3)]
        [TestCase("checkbox", "Category", "Communications", 1)]
        [TestCase("checkbox", "Category", "Continuous Improvement", 8)]
        [TestCase("checkbox", "Category", "Continuous Manufacturing Intelligence", 2)]
        [TestCase("checkbox", "Category", "Continuous Monitoring", 5)]
        [TestCase("checkbox", "Category", "Continuous Supply Chain Intelligence", 5)]
        [TestCase("checkbox", "Category", "Cost of Quality", 2)]
        [TestCase("checkbox", "Category", "Data Analytics", 4)]
        [TestCase("checkbox", "Category", "Data Collection", 5)]
        [TestCase("checkbox", "Category", "Data Management", 5)]
        [TestCase("checkbox", "Category", "Digital Manufacturing", 4)]
        [TestCase("checkbox", "Category", "Digitized Quality Checks", 2)]
        [TestCase("checkbox", "Category", "Edge Computing", 5)]
        [TestCase("checkbox", "Category", "Energy Efficiency", 3)]
        [TestCase("checkbox", "Category", "Energy Monitoring", 6)]
        [TestCase("checkbox", "Category", "Enterprise Data Management", 3)]
        [TestCase("checkbox", "Category", "Enterprise Integration", 4)]
        [TestCase("checkbox", "Category", "Heating", 2)]
        [TestCase("checkbox", "Category", "Historian", 2)]
        [TestCase("checkbox", "Category", "IIoT", 10)]
        [TestCase("checkbox", "Category", "In-House Quality Inspections", 2)]
        [TestCase("checkbox", "Category", "Industrial Software", 6)]
        [TestCase("checkbox", "Category", "Industry 4.0", 5)]
        [TestCase("checkbox", "Category", "Inventory Management", 3)]
        [TestCase("checkbox", "Category", "Inventory Optimization", 4)]
        [TestCase("checkbox", "Category", "Logistics and Yard Solutions", 2)]
        [TestCase("checkbox", "Category", "Machine Connectivity", 4)]
        [TestCase("checkbox", "Category", "Machine Learning", 8)]
        [TestCase("checkbox", "Category", "Machine Monitoring", 4)]
        [TestCase("checkbox", "Category", "Machine Vision", 2)]
        [TestCase("checkbox", "Category", "Machining", 3)]
        [TestCase("checkbox", "Category", "Maintenance", 6)]
        [TestCase("checkbox", "Category", "Manufacturing Operations Solutions", 2)]
        [TestCase("checkbox", "Category", "Material Analysis Solutions", 2)]
        [TestCase("checkbox", "Category", "Material Traceability", 2)]
        [TestCase("checkbox", "Category", "MES", 4)]
        [TestCase("checkbox", "Category", "Molding", 2)]
        [TestCase("checkbox", "Category", "OEE", 5)]
        [TestCase("checkbox", "Category", "OT Data Lake", 2)]
        [TestCase("checkbox", "Category", "Palletizing", 1)]
        [TestCase("checkbox", "Category", "Planning and Scheduling", 2)]
        [TestCase("checkbox", "Category", "Plant Reliability", 3)]
        [TestCase("checkbox", "Category", "Power Factor", 4)]
        [TestCase("checkbox", "Category", "Power Quality", 2)]
        [TestCase("checkbox", "Category", "Predictive Maintenance", 7)]
        [TestCase("checkbox", "Category", "Process Optimization", 7)]
        [TestCase("checkbox", "Category", "Product Quality", 1)]
        [TestCase("checkbox", "Category", "Production Quality", 1)]
        [TestCase("checkbox", "Category", "Quality Assessment", 2)]
        [TestCase("checkbox", "Category", "Quality Assurance", 6)]
        [TestCase("checkbox", "Category", "Quality Traceability", 2)]
        [TestCase("checkbox", "Category", "Remote Monitoring", 5)]
        [TestCase("checkbox", "Category", "Renewables Monitoring", 2)]
        [TestCase("checkbox", "Category", "Short Interval Control", 2)]
        [TestCase("checkbox", "Category", "Smart Manufacturing", 3)]
        [TestCase("checkbox", "Category", "Supplier Inventory Visibility", 2)]
        [TestCase("checkbox", "Category", "Supplier Quality", 2)]
        [TestCase("checkbox", "Category", "Supply Chain", 3)]
        [TestCase("checkbox", "Category", "Supply Chain Automation", 2)]
        [TestCase("checkbox", "Category", "Supply Chain Management", 2)]
        [TestCase("checkbox", "Category", "Supply Chain Optimization", 5)]
        [TestCase("checkbox", "Category", "Warranty Management", 1)]
        [TestCase("checkbox", "Category", "Workflow Mapping", 3)]
        [TestCase("checkbox", "Vertical", "Academia", 4)]
        [TestCase("checkbox", "Vertical", "Aerospace", 13)]
        [TestCase("checkbox", "Vertical", "Agriculture", 6)]
        [TestCase("checkbox", "Vertical", "Assembly", 3)]
        [TestCase("checkbox", "Vertical", "Automotive", 14)]
        [TestCase("checkbox", "Vertical", "Biotechnology", 1)]
        [TestCase("checkbox", "Vertical", "Building Materials and Supplies", 4)]
        [TestCase("checkbox", "Vertical", "Cement", 7)]
        [TestCase("checkbox", "Vertical", "Chemical", 9)]
        [TestCase("checkbox", "Vertical", "CNC", 7)]
        [TestCase("checkbox", "Vertical", "Consumer and Paper Goods", 6)]
        [TestCase("checkbox", "Vertical", "Consumer Products", 4)]
        [TestCase("checkbox", "Vertical", "Defense", 8)]
        [TestCase("checkbox", "Vertical", "Discrete Manufacturing", 7)]
        [TestCase("checkbox", "Vertical", "Distribution", 6)]
        [TestCase("checkbox", "Vertical", "Electronics", 5)]
        [TestCase("checkbox", "Vertical", "Energy", 8)]
        [TestCase("checkbox", "Vertical", "Energy (Oil & Gas)", 12)]
        [TestCase("checkbox", "Vertical", "Food & Beverage", 9)]
        [TestCase("checkbox", "Vertical", "Forestry", 4)]
        [TestCase("checkbox", "Vertical", "Foundry", 4)]
        [TestCase("checkbox", "Vertical", "Government", 8)]
        [TestCase("checkbox", "Vertical", "Greenhouses", 3)]
        [TestCase("checkbox", "Vertical", "Heavy Industry", 9)]
        [TestCase("checkbox", "Vertical", "Hospitality", 3)]
        [TestCase("checkbox", "Vertical", "Industrial Gas", 5)]
        [TestCase("checkbox", "Vertical", "Intelligence", 3)]
        [TestCase("checkbox", "Vertical", "Life Sciences", 5)]
        [TestCase("checkbox", "Vertical", "Lumber", 5)]
        [TestCase("checkbox", "Vertical", "Machine Shops", 7)]
        [TestCase("checkbox", "Vertical", "Manufacturing", 8)]
        [TestCase("checkbox", "Vertical", "Medical Devices", 3)]
        [TestCase("checkbox", "Vertical", "Metals", 8)]
        [TestCase("checkbox", "Vertical", "Mining", 11)]
        [TestCase("checkbox", "Vertical", "Packaging", 4)]
        [TestCase("checkbox", "Vertical", "Personal Care", 6)]
        [TestCase("checkbox", "Vertical", "Petrochemical", 6)]
        [TestCase("checkbox", "Vertical", "Pharmaceuticals", 6)]
        [TestCase("checkbox", "Vertical", "Plastics", 7)]
        [TestCase("checkbox", "Vertical", "Plastics and Rubber", 2)]
        [TestCase("checkbox", "Vertical", "Power", 3)]
        [TestCase("checkbox", "Vertical", "Process Manufacturing", 7)]
        [TestCase("checkbox", "Vertical", "Pulp & Paper", 6)]
        [TestCase("checkbox", "Vertical", "Refining", 6)]
        [TestCase("checkbox", "Vertical", "Remanufacturing", 4)]
        [TestCase("checkbox", "Vertical", "Retailers", 3)]
        [TestCase("checkbox", "Vertical", "Sawmills", 5)]
        [TestCase("checkbox", "Vertical", "Seafood", 3)]
        [TestCase("checkbox", "Vertical", "Semiconductor", 7)]
        [TestCase("checkbox", "Vertical", "Small-Medium Enterprises", 9)]
        [TestCase("checkbox", "Vertical", "Steel", 9)]
        [TestCase("checkbox", "Vertical", "Telecom", 5)]
        [TestCase("checkbox", "Vertical", "Textile", 7)]
        [TestCase("checkbox", "Vertical", "Utilities", 11)]
        [TestCase("checkbox", "Publisher", "5G Technologies USA Ltd.", 3)]
        [TestCase("checkbox", "Publisher", "Adapdix", 1)]
        [TestCase("checkbox", "Publisher", "Amatrol", 1)]
        [TestCase("checkbox", "Publisher", "Augury", 1)]
        [TestCase("checkbox", "Publisher", "Beeond, Inc.", 1)]
        [TestCase("checkbox", "Publisher", "C5MI Insight LLC ", 3)]
        [TestCase("checkbox", "Publisher", "CESMII - The Smart Manufacturing Institute", 1)]
        [TestCase("checkbox", "Publisher", "CoreWatts", 1)]
        [TestCase("checkbox", "Publisher", "Emerson", 1)]
        [TestCase("checkbox", "Publisher", "Falkonry Inc.", 1)]
        [TestCase("checkbox", "Publisher", "Litmus Automation", 2)]
        [TestCase("checkbox", "Publisher", "SAS", 2)]
        [TestCase("checkbox", "Publisher", "ShelfAware", 1)]
        [TestCase("checkbox", "Publisher", "SymphonyAI Industrial", 1)]
        [TestCase("checkbox", "Publisher", "ThinkIQ", 1)]
        [TestCase("checkbox", "Publisher", "Toward Zero", 1)]
        // Test Fixture End
        #endregion

        public void ClickItemInCheckbox(string strTestType, string strItemType, string strItemName, int cExpectedStaging)
        {
            string str2 = driver.Title;

            int cExpectedProduction = 0;
            utils.Console_WriteLine($"ClickItemInCheckbox: Entering function");

            if (strTestType != "checkbox")
                throw new Exception($"ClickItemInCheckbox: Expected test type of 'checkbox' not found. Instead found {strTestType}");

            // Clean up input.
            strItemName = strItemName.Trim();
            strItemType = strItemType.Trim();
            
            int iGroup = (strItemType == "Vertical") ? 1 
                       : (strItemType == "Category") ? 2 
                       : (strItemType == "Publisher") ? 3 
                       : -1;

            if (driver == null)
                throw new Exception($"Error accessing ChromeDriver: driver is null");

            int cExpectedItems = (bStaging) ? cExpectedStaging : cExpectedProduction;
            int cFoundItems = -1;
            bool bException = false;
            if (cExpectedItems > 0)
            {
                // Wait until page is ready to read
                //bool bFound = false;
                //for (int nRetries = 100; (!bFound && nRetries > 0); nRetries--)
                //{
                //    var xxElement = GetWebElementFromGroup(driver, iGroup, 0);
                //    if (xxElement != null)
                //        bFound = true;
                //    System.Threading.Thread.Sleep(c50);
                //}

                // Make sure "Show More" is visible
                //utils.MarketplaceSeeAllSeeLess(driver, iGroup, true);
                //System.Threading.Thread.Sleep(c250);

                // Clear All -- We navigate instead of Clicking [Clear All] to keep URL parameters
                driver.FindElement(By.CssSelector(".ml-sm-auto")).Click();  // Clear All
                // driver.Navigate().GoToUrl(strStartUrl);
                System.Threading.Thread.Sleep(c1000);

                int iItem = utils.FindItemInList(bStaging, iGroup, strItemName);
                if (iItem > -1)
                {
                    var mywebelement = GetWebElementFromGroup(driver, iGroup, iItem);
                    if (mywebelement != null)
                    {
                        try
                        {
                            mywebelement.Click();
                            System.Threading.Thread.Sleep(c1000);
                            cFoundItems = QueryItemCountOnWebpage(driver, cMaxItems);
                        }
                        catch (Exception ex)
                        {
                            bException = true;
                            utils.Console_WriteLine($"ClickItemInCheckbox: Exception for {strItemType}: {strItemName}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    utils.Console_WriteLine($"ClickItemInCheckbox: Cannot find item in list. bStaging:{bStaging}, iGroup:{iGroup}, strItemName:{strItemName}");
                }
            }


            if (!bException)
            {
                if (cExpectedItems == 0)
                {
                    utils.Console_WriteLine($"ClickItemInCheckbox: No items for {strItemType}: {strItemName}: SKIPPED");
                    Assert.Pass();
                }
                else if (cExpectedItems == cFoundItems)
                {
                    utils.Console_WriteLine($"ClickItemInCheckbox: Test Result for {strItemType}: {strItemName}: PASS");
                    Assert.Pass();
                }
                else
                {
                    utils.Console_WriteLine($"ClickItemInCheckbox: Test Result for {strItemType}: {strItemName}: FAIL (Expected {cExpectedItems} but found {cFoundItems}");
                    Assert.Fail();
                }
            }
        }



        private IWebElement GetWebElementFromGroup(IWebDriver mywebdriver, int iGroup, int iItem)
        {
            // Make sure the "+ Sell all / - See less" is expanded
            // utils.MarketplaceSeeAllSeeLess(mywebdriver, iGroup, true);

            // Select Nth publisher in list
            IWebElement iweReturn = null;
            int nItem = iItem + 1;
            string strItemName = utils.GetItemName(iGroup, iItem, true);
            string strElement = (nItem < 8) ? $".info-section:nth-child({iGroup}) .selectable:nth-child({nItem})"
                                            : $".info-section:nth-child({iGroup}) .selectable:nth-child({nItem}) span";

            for (int iRetry = 5; iRetry > 0 && iweReturn == null; iRetry--)
            {
                try
                {
                    iweReturn = mywebdriver.FindElement(By.CssSelector(strElement)); // .Click();
                }
                catch (Exception ex)
                {
                    utils.Console_WriteLine($"GetWebElementFromGroup: Exception {ex.Message} ");
                }

                if (iweReturn == null)
                {
                    utils.Console_WriteLine($"GetWebElementFromGroup [{iItem}] - iRetry={iRetry}");
                    System.Threading.Thread.Sleep(c50);
                }
            }

            return iweReturn;
        }

        private int QueryItemCountOnWebpage(IWebDriver mywebdriver, int cMax)
        {
            int cItems = cMax;
            int cMaxRetry = 3;

            while (cItems == cMax && cMaxRetry > 0)
            {
                try
                {
                    var eleItemCounter = mywebdriver.FindElement(By.CssSelector(".text-left"));
                    System.Threading.Thread.Sleep(c250);
                    if (eleItemCounter == null)
                    {
                        utils.Console_WriteLine($"QueryItemCountOnWebpage: null value returned");
                        return -1;
                    }

                    var str2 = eleItemCounter.Text;
                    var ai = str2.Split(new char[] { ' ' });
                    if (ai.Length == 2)
                    {
                        int.TryParse(ai[0], out cItems);
                    }
                }
                catch (Exception ex)
                {
                    utils.Console_WriteLine($"QueryItemCountOnWebpage: exception");
                }

                cMaxRetry--;
            }

            return cItems;
        }
    }
}
