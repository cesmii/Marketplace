using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using static System.Net.WebRequestMethods;

namespace Marketplace_InBrowser_Tests
{
    internal class TestUtils
    {
        public static string? GetStartUrl()
        {
            // The web page address within the MyNodeJS Docker container
            string? strBaseUrl = Environment.GetEnvironmentVariable("NODEJS"); 
            
               return $"{strBaseUrl}:3000/library?p=1&t=10";

            // return "http://localhost:3000/library?p=1&t=10";
            // return "http://localhost:4444/library?p=1&t=10";
        }
        public static IWebDriver CreateChromeDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--no-sandbox");
            options.AddArguments("--headless");
            options.AddArguments("--disable-dev-shm-usage");
            options.AddArguments("--window-size=1920,1080");
            options.AddArguments("--whitelisted-ips=''");     // Allow any connections
            IWebDriver driver = new ChromeDriver(options);


            // Options for desktop Windows?
            //var chromeOptions = new ChromeOptions();
            //chromeOptions.BrowserVersion = "118";
            //chromeOptions.PlatformName = "Windows 10";
            //IWebDriver driver = new RemoteWebDriver(new Uri("http://localhost:4444"), chromeOptions);
            return driver;
        }

#pragma warning disable CS0168 // Variable is declared but never used
        public static (IWebElement?,IWebElement?) TryFindElement2 (IWebDriver driver, string strSelector1, string strSelector2, int cRetry = 10, int cWaitMs = 25)
        {
            IWebElement? iweReturn1 = null;
            IWebElement? iweReturn2 = null;
            for (int iTry = 0; iTry < cRetry; iTry++)
            {
                try
                {
                    iweReturn1 = driver.FindElement(By.CssSelector(strSelector1));
                }
                catch (Exception ex)
                {

                }
                if (iweReturn1== null)
                {
                    try
                    {
                        iweReturn2 = driver.FindElement(By.CssSelector(strSelector2));
                    }
                    catch (Exception ex)
                    {

                    }
                    if (iweReturn2 != null)
                        break;
                }
                else
                {
                    break;
                }

                Console_WriteLine($"TryFindElement: iTry:{iTry}/{cRetry}");
                System.Threading.Thread.Sleep(cWaitMs);
            }

            return (iweReturn1, iweReturn2);
        }

        public static IWebElement? TryFindElement(IWebDriver? driver, string strSelector, int cRetry = 10, int cWaitMs = 25)
        {
            IWebElement? iweReturn = null;
            for (int iTry = 0; iTry < cRetry; iTry++)
            {
                try
                {
                    iweReturn = (driver == null) ? null : driver.FindElement(By.CssSelector(strSelector));
                }
                catch (Exception ex)
                {

                }
                if (iweReturn != null)
                {
                    break;
                }   

                // Console_WriteLine($"TryFindElement: iTry:{iTry}/{cRetry}");
                System.Threading.Thread.Sleep(cWaitMs);
            }

            return iweReturn;
        }

        /// <summary>
        /// MarketplaceSeeAllSeeLess - In three lists in marketplace library, toggle between the
        /// "See All" and "See Less" buttons.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="iGroup">1 is Industry Vertical, 2 is Processes, 3 is Publishers</param>
        /// <param name="bSeeAll">true to See All, false to See Less </param>
        /// <exception cref="Exception"></exception>
        public static void MarketplaceSeeAllSeeLess(IWebDriver driver, int iGroup, bool bSeeAll)
        {
            if (iGroup < 1 || iGroup > 3)
                throw new Exception($"MarketplaceSeeAllSeeLess: invalid iGroup value: {iGroup}");

            try
            {
                //var eleMoreLess = driver.FindElement(By.CssSelector($".info-section:nth-child({iGroup}) > .btn"));
                var eleMoreLess = TestUtils.TryFindElement(driver, $".info-section:nth-child({iGroup}) > .btn");
                if (eleMoreLess != null)
                {
                    string strValue = eleMoreLess.Text;
                    if (bSeeAll && strValue == "+ See all")
                        eleMoreLess.Click();
                    else if (!bSeeAll && strValue == "- See less")
                        eleMoreLess.Click();
                }
            }
            catch (Exception ex)
            {
                TestUtils.Console_WriteLine($"MarketplaceSeeAllSeeLess: Exception: {ex.Message}");
            }
        }

        public static bool WaitForMax(IWebDriver? d, int nMax, int cRetry, int cMilliseconds)
        {
            bool bSuccess = false;
            for (int i = 0; i < cRetry; i++)
            {
                int cCurrentValue = GetItemCountFromWebpage($"WaitForMax", d, -1, 1, 0);
//                Console_WriteLine($"WaitForMax: i:{i} cCurrentValue:{cCurrentValue} nMax:{nMax}");
                if (cCurrentValue == nMax)
                {
                    bSuccess = true;
                    break;
                }
                // Try again.
                System.Threading.Thread.Sleep(cMilliseconds);
            }

            return bSuccess;
        }

        public static void Console_WriteLine(string strOutput)
        {
            DateTime dt = DateTime.Now;
            string strOutput2 = $"{dt.Year:0000}-{dt.Month:00}-{dt.Day:00}T{dt.Hour:00}:{dt.Minute:00}:{dt.Second:00}.{dt.Millisecond:000} {strOutput}";
            Console.WriteLine(strOutput2);
            System.Diagnostics.Debug.WriteLine(strOutput2);
        }

        public static int GetItemCountFromWebpage(string strItem, IWebDriver? mywebdriver, int cMaxItems = -1, int cRetry = 25, int cWaitMs = 25)
        {
            int cItems = -2;

            if (mywebdriver != null)
            {
                for (int iRetry = cRetry; iRetry > 0; iRetry--)
                {
                    var (iwe1, iwe2) = TestUtils.TryFindElement2(mywebdriver, ".text-left", ".text-center:nth-child(1)", cRetry, cWaitMs);

                    if (iwe1 != null)
                    {
                        var strText = iwe1.Text;
                        var astr = strText.Split(new char[] { ' ' });
                        bool bFoundInteger = int.TryParse(astr[0], out cItems);

                        if (bFoundInteger && cItems != cMaxItems)
                        {
                            Console_WriteLine($"GetItemCountFromWebpage: iRetry:from {cRetry} to {iRetry} cMaxItems: {cMaxItems}  cItem:{cItems} ({strItem})");
                            break;
                        }
                    }
                    else if (iwe2 != null)
                    {
                        string strText2 = iwe2.Text;
                        if (strText2.StartsWith("There are no matching"))
                        {
                            cItems = 0;
                            break;
                        }
                    }

                    // Try again.
                    System.Threading.Thread.Sleep(cWaitMs);
                }
            }

            return cItems;
        }

        public static int GetGroupIndex(string strGroup)
        {
            if (strGroup == "Vertical") return 1;
            if (strGroup == "Category") return 2;
            if (strGroup == "Publisher") return 3;
            return -1;
        }

        public static void Set_SM_App_State_Selected(IWebDriver driver)
        {
            // Click on "SM_App" button (should be unselected)
            IWebElement? iwe = TestUtils.TryFindElement(driver, "#\\36 275769bb7e0831201e5c3e2 > .not-selected", 25, 25);
            Assert.True(iwe != null);

            // Set to selected state
            TestUtils.ClickWhenPageIsReady(driver, iwe);
        }

        public static void Set_SM_Hardware_State_Selected(IWebDriver driver)
        {
            // Click on "SM_Hardware" button (should be unselected)
            IWebElement? iwe = TestUtils.TryFindElement(driver, "#\\36 29763866827ef2028a17d61 > .not-selected");
            Assert.True(iwe != null);

            // Set to selected state
            TestUtils.ClickWhenPageIsReady(driver, iwe);
        }

        public static void Set_Category_See_All_State_Selected(IWebDriver driver, int iGroup)
        {
            // Click on "See All" button (should be unselected)
            IWebElement? iwe = TestUtils.TryFindElement(driver, $".info-section:nth-child({iGroup}) > .btn");
            Assert.True(iwe != null);

            // Set to selected state
            //iwe.Click();
            TestUtils.ClickWhenPageIsReady(driver, iwe);
        }

        public static IWebElement? GetWebElementFromGroup(IWebDriver mywebdriver, int iGroup, int iItem)
        {
            // Make sure the "+ Sell all / - See less" is expanded
            // utils.MarketplaceSeeAllSeeLess(mywebdriver, iGroup, true);

            // Select Nth publisher in list
            IWebElement? iweReturn = null;
            int nItem = iItem + 1;
            // string strItemName = utils.GetItemName(iGroup, iItem, true);
            string strElement = (nItem < 8) ? $".info-section:nth-child({iGroup}) .selectable:nth-child({nItem})"
                                            : $".info-section:nth-child({iGroup}) .selectable:nth-child({nItem}) span";

            iweReturn = TestUtils.TryFindElement(mywebdriver, strElement);

            return iweReturn;
        }
        public static bool ClickWhenPageIsReady(IWebDriver? d, IWebElement e, int retry = 10, int msWait = 50)
        {
            bool bSuccess = false;
            bool bFound = true;
            while (bFound)
            {
                IWebElement? iwePreloader = (d == null) ? null : TestUtils.TryFindElement(d, "preloader", 0, 0);
                bFound = (iwePreloader != null);
            }

            for (; retry > 0 && bSuccess == false; retry--)
            {
                try
                {
                    e.Click();
                    bSuccess = true;
                    break;
                }
                catch { }

                System.Threading.Thread.Sleep(msWait);
            }   

            return bSuccess;
        }


    }
}
