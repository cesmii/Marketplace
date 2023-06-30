using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using CESMII.Marketplace.UI.Tests;
using Microsoft.VisualBasic.FileIO;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace CESMII.Marketplace
{

    [TestFixture]
    public class Publishers_List_Item_Search
    {
        private string strStartUrl = "https://marketplace-front-stage.azurewebsites.net/library";  // Staging
        // private string strStartUrl = "https://marketplace-front.azurewebsites.net/library";     // Production
        private IWebDriver? driver = null;
        public IDictionary<string, object> vars { get; private set; }

        private Dictionary<string, int> dictPublisherItems = new Dictionary<string, int>();

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
            Console_WriteLine($"Setup: Entering function");
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

            bStaging = strStartUrl.Contains("-stage.");
            InitPublisherList();


            // Open the website
            driver.Navigate().GoToUrl(strStartUrl);

            // Fetch max number of items. When we query a count, we use this
            // values as a "not quite ready" flag.
            if (cMaxItems == -1)
            {
                for (int iRetry = 0; iRetry < 10 && cMaxItems == -1; iRetry++)
                {
                    cMaxItems = utils.MP_QueryPublisherItemCount(driver, cMaxItems, "SysInit");
                }
            }
        }


        [TearDown]
        protected void TearDown()
        {
            Console_WriteLine($"TearDown: Entering function");
            if (driver != null)
                driver.Quit();
        }

        [TestCase("5G Technologies USA", 2, 3)]
        [TestCase("Adapdix", 1, 1)]
        [TestCase("Amatrol", 1, 1)]
        [TestCase("Augury", 1, 1)]
        [TestCase("Beeond", 0, 1)]
        [TestCase("C5MI Insight LLC", 0,3)]
        [TestCase("CESMII - The Smart Manufacturing Institute", 0, 0)]
        [TestCase("CoreWatts", 1, 1)]
        [TestCase("Emerson", 1, 1)]
        [TestCase("Falkonry", 1, 1)]
        [TestCase("Litmus Automation", 1, 1)]
        [TestCase("SAS", 0, 2)]
        [TestCase("ShelfAware", 0, 1)]
        [TestCase("SymphonyAI Industrial", 1, 1)]
        [TestCase("ThinkIQ", 0, 1)]
        [TestCase("Toward Zero", 1, 1)]
        public void PublisherQueryPublisherName(string strPublisher, int cStagingItems, int cProductionItems)
        {
            Console_WriteLine($"PublisherQueryPublisherName: Entering function strPublisher={strPublisher}");

            if (driver != null)
            {
                int cExpected = (bStaging) ? cStagingItems : cProductionItems;
                if (cExpected == 0)
                {
                    Console_WriteLine($"PublisherQueryPublisherName: Skipped driver.manage().timeouts().implicitlyWait(Duration.ofSeconds(10));publisher {strPublisher}, which has no records.");
                    Assert.Pass();
                }
                else
                {
                    int cItemsOnWebPage = -1;
                    for (int iRetry = 0; iRetry < 100; iRetry++)
                    {
                        Console_WriteLine($"For publisher ({strPublisher}) iRetry={iRetry} Send Keys");
                        utils.MP_SendKeysToQueryBox(driver, strPublisher, 5);
                        System.Threading.Thread.Sleep(iRetry*10);
                        int cTemp = utils.MP_QueryPublisherItemCount(driver, cMaxItems, strPublisher);
                        if (cTemp != cMaxItems && cTemp != -1)
                        {
                            // Valid value
                            cItemsOnWebPage = cTemp;
                            break;
                        }

                    }
                    Console_WriteLine($"For publisher ({strPublisher}), Item count = {cItemsOnWebPage}");


                    if (cItemsOnWebPage == cExpected)
                    {
                        Console_WriteLine($"PublisherQueryPublisherName: Pass. Publisher:{strPublisher}");
                        Assert.Pass();
                    }
                    else
                    {
                        Console_WriteLine($"MP_QueryPublisherItemCount: Fail. Publisher:{strPublisher}, ItemsOnWebPage:{cItemsOnWebPage} versus Expected:{cExpected}");
                        Assert.Fail();
                    }
                }
            }
        }





        [Test]
        public void PublisherEnumerateListAndSelect()
        {
            Console_WriteLine($"PublisherEnumerateListAndSelect: Entering function");

            if (driver == null)
                throw new Exception($"Error accessing ChromeDriver: driver is null");

            // Open the website
            // driver.Navigate().GoToUrl(strStartUrl);

            // 2 | setWindowSize | 1228x1442 | 
            //        driver.Manage().Window.Size = new System.Drawing.Size(1228, 1800);

            bool bFound = false;
            for (int nRetries = 100; (!bFound && nRetries > 0); nRetries--)
            {
                var xxElement = GetPublisher(0);
                if (xxElement != null)
                    bFound = true;
                System.Threading.Thread.Sleep(c50);
            }

            // Click to open the "Show More" for the publisher's list
            // click | css=.info-section:nth-child(3) > .btn | 
            try { driver.FindElement(By.CssSelector(".info-section:nth-child(3) > .btn")).Click(); } catch (Exception ex) { }
            System.Threading.Thread.Sleep(c250);

            // 4 | click | css=.info-section:nth-child(3) .selectable:nth-child(1) | 

            for (int iPublisher = 0; iPublisher < 50; iPublisher++)
            {
                // 5 | click | css=.ml-sm-auto | 
                // Clear All
                driver.FindElement(By.CssSelector(".ml-sm-auto")).Click();
                System.Threading.Thread.Sleep(c1000);

                var xxPublisher = GetPublisher(iPublisher);
                if (xxPublisher == null)
                    break;

                string strPublisher = string.Empty;
                int cItems = 0;
                if (xxPublisher != null)
                {
                    strPublisher = xxPublisher.Text;
                    try { xxPublisher.Click(); } catch (Exception ex) { }
                    System.Threading.Thread.Sleep(c1000);
                    cItems = QueryItemCount();
                }
                Console_WriteLine($"For publisher {iPublisher} ({strPublisher}), Item count = {cItems}");

                if (!ValidatePublisherItems(strPublisher, cItems, out int cExpected))
                {
                    Console_WriteLine($"Error in publisher details. Publisher={strPublisher} Expected = {cExpected} Found={cItems}");
                }
            }

            Assert.Pass();
        }


        private bool ValidatePublisherItems(string strPublisher, int cItems, out int cExpected)
        {
            Console_WriteLine($"ValidatePublisherItems: Entering function ");
            cExpected = -1;

            bool bSuccess = false;
            if (dictPublisherItems.ContainsKey(strPublisher))
            {
                cExpected = dictPublisherItems[strPublisher];
                if (cItems == cExpected)
                    bSuccess = true;
            }

            Console_WriteLine($"ValidatePublisherItems: Publisher: {strPublisher} Expected:{cExpected} Found: {cItems}");

            return bSuccess;
        }

        public void InitPublisherList()
        {
            if (dictPublisherItems.Count == 0)
            {
                if (bStaging)
                {
                    dictPublisherItems.Add("5G Technologies USA Ltd.", 2);
                    dictPublisherItems.Add("Adapdix", 1);
                    dictPublisherItems.Add("Amatrol", 1);
                    dictPublisherItems.Add("Augury", 1);
                    dictPublisherItems.Add("CoreWatts", 1);
                    dictPublisherItems.Add("Emerson", 1);
                    dictPublisherItems.Add("Falkonry Inc.", 1);
                    dictPublisherItems.Add("Litmus Automation", 1);
                    dictPublisherItems.Add("SymphonyAI Industrial", 1);
                    dictPublisherItems.Add("Toward Zero", 1);
                }
                else
                {
                    dictPublisherItems.Add("5G Technologies USA Ltd.", 3);
                    dictPublisherItems.Add("Adapdix", 1);
                    dictPublisherItems.Add("Amatrol", 1);
                    dictPublisherItems.Add("Augury", 1);
                    dictPublisherItems.Add("Beeond, Inc.", 1);
                    dictPublisherItems.Add("C5MI Insight LLC", 3);
                    dictPublisherItems.Add("CESMII - The Smart Manufacturing Institute", 0);
                    dictPublisherItems.Add("CoreWatts", 1);
                    dictPublisherItems.Add("Emerson", 1);
                    dictPublisherItems.Add("Falkonry Inc.", 1);
                    dictPublisherItems.Add("Litmus Automation", 1);
                    dictPublisherItems.Add("SAS", 2);
                    dictPublisherItems.Add("ShelfAware", 1);
                    dictPublisherItems.Add("SymphonyAI Industrial", 1);
                    dictPublisherItems.Add("ThinkIQ", 1);
                    dictPublisherItems.Add("Toward Zero", 1);
                }

                //astrPublishers = new string[dictPublisherItems.Count];
                //int iItem = 0;
                //foreach (KeyValuePair<string, int> kvp in dictPublisherItems)
                //{
                //    astrPublishers[iItem] = kvp.Key;
                //    iItem++;
                //}

            }
        }

        private IWebElement GetPublisher(int nPublisher)
        {
            // Make sure the "+ Sell all / - See less" is expanded
            utils.PublisherListShowAll(driver);

            // Select Nth publisher in list
            IWebElement iweReturn = null;
            int nItem = nPublisher + 1;
            if (nItem < 8)
            {
                for (int iRetry = 5; iRetry > 0 && iweReturn == null; iRetry--)
                {
                    try
                    {
                        iweReturn = driver.FindElement(By.CssSelector($".info-section:nth-child(3) .selectable:nth-child({nItem})")); // .Click();
                    }
                    catch (Exception ex)
                    {
                    }

                    if (iweReturn == null)
                    {
                        Console_WriteLine($"GetPublisher [{nPublisher}] - iRetry={iRetry}");
                        System.Threading.Thread.Sleep(c500);
                    }
                }
            }
            else
            {
                for (int iRetry = 5; iRetry > 0 && iweReturn == null; iRetry--)
                {
                    try
                    {
                        iweReturn = driver.FindElement(By.CssSelector($".info-section:nth-child(3) .selectable:nth-child({nItem}) span")); // .Click();
                    }
                    catch (Exception ex)
                    {
                    }


                    if (iweReturn == null)
                    {
                        Console_WriteLine($"GetPublisher [{nPublisher}] - iRetry={iRetry}");
                        System.Threading.Thread.Sleep(c500);
                    }
                }
            }

            return iweReturn;
        }

        private int QueryItemCount()
        {
            int cItems = 0;

            try
            {
                var eleItemCounter = driver.FindElement(By.CssSelector(".text-left"));
                System.Threading.Thread.Sleep(c1000);
                if (eleItemCounter == null)
                {
                    Console_WriteLine($"QueryItemCount: null value returned");
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
            { }

            return cItems;
        }

        private void Console_WriteLine(string strOutput)
        {
            DateTime dt = DateTime.Now;
            string strOutput2 = $"{dt.Year:0000}-{dt.Month:00}-{dt.Day:00}T{dt.Hour:00}:{dt.Minute:00}{dt.Second:00}.{dt.Millisecond:000} {strOutput}";
            Console.WriteLine(strOutput2);
            System.Diagnostics.Debug.WriteLine(strOutput2);
        }

    }
}
