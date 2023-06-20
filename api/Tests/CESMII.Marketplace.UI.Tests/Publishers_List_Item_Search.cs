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

[TestFixture]
public class Publishers_List_Item_Search
{
    // private string strStartUrl = "https://marketplace-front-stage.azurewebsites.net/library";  // Staging
    private string strStartUrl = "https://marketplace-front.azurewebsites.net/library";     // Production
    private IWebDriver? driver = null;
    public IDictionary<string, object> vars { get; private set; }

    private Dictionary<string, int> dictPublisherItems = new Dictionary<string, int>();

    private bool bStaging = true;

    private IJavaScriptExecutor js;
    [SetUp]
    public void SetUp()
    {
        try
        {
            driver = new ChromeDriver();
        }
        catch (Exception ex)
        {

        }
        js = (IJavaScriptExecutor)driver;
        vars = new Dictionary<string, object>();

        bStaging = strStartUrl.Contains("-stage.");
    }
    [TearDown]
    protected void TearDown()
    {
        if (driver != null)
            driver.Quit();
    }
    [Test]
    public void testCase()
    {
        // Open the website
        driver.Navigate().GoToUrl(strStartUrl);

        // 2 | setWindowSize | 1228x1442 | 
//        driver.Manage().Window.Size = new System.Drawing.Size(1228, 1800);

        bool bFound = false;
        for (int nRetries = 100; (!bFound && nRetries > 0); nRetries--)
        {
            var xxElement = GetPublisher(0);
            if (xxElement != null)
                bFound = true;
            System.Threading.Thread.Sleep(50);
        }

        // Click to open the "Show More" for the publisher's list
        // click | css=.info-section:nth-child(3) > .btn | 
        try { driver.FindElement(By.CssSelector(".info-section:nth-child(3) > .btn")).Click(); } catch (Exception ex) { }
        System.Threading.Thread.Sleep(250);

        // 4 | click | css=.info-section:nth-child(3) .selectable:nth-child(1) | 

        for (int iPublisher = 0; iPublisher < 50; iPublisher++)
        {
            // 5 | click | css=.ml-sm-auto | 
            // Clear All
            driver.FindElement(By.CssSelector(".ml-sm-auto")).Click();
            System.Threading.Thread.Sleep(50);

            var xxPublisher = GetPublisher(iPublisher);
            if (xxPublisher == null)
                break;

            string strPublisher = string.Empty;
            int cItems = 0;
            if (xxPublisher != null)
            {
                strPublisher = xxPublisher.Text;
                try { xxPublisher.Click(); } catch (Exception ex) { }
                System.Threading.Thread.Sleep(250);
                cItems = QueryItemCount();
            }
            System.Diagnostics.Debug.WriteLine($"For publisher {iPublisher} ({strPublisher}), Item count = {cItems}");

            if (!ValidatePublisherItems(bStaging, strPublisher, cItems, out int cExpected))
            {
                throw new Exception($"Error in publisher details. Publisher={strPublisher} Expected = {cExpected} Found={cItems}");
            }
        }
    }


    private bool ValidatePublisherItems(bool bStaging, string strPublisher, int cItems, out int cExpected)
    {
        cExpected = -1;
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
                dictPublisherItems.Add("Litmus Automation", 1);
                dictPublisherItems.Add("SAS", 2);
                dictPublisherItems.Add("ShelfAware", 1);
                dictPublisherItems.Add("SymphonyAI Industrial", 1
                dictPublisherItems.Add("ThinkIQ", 1);
                dictPublisherItems.Add("Toward Zero", 1);
                dictPublisherItems.Add("Amatrol", 1);
                dictPublisherItems.Add("Augury", 1);
                dictPublisherItems.Add("Beeond, Inc.", 1);
                dictPublisherItems.Add("C5MI Insight LLC", 3);
                dictPublisherItems.Add("CESMII - The Smart Manufacturing Institute", 0);
                dictPublisherItems.Add("CoreWatts", 1);
                dictPublisherItems.Add("Emerson", 1);
                dictPublisherItems.Add("Falkonry Inc.", 1);
            }
        }

        bool bSuccess = false;
        if (dictPublisherItems.ContainsKey(strPublisher))
        {
            cExpected = dictPublisherItems[strPublisher];
            if (cItems == cExpected)
                bSuccess = true;
        }

        return bSuccess;
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
            for (int iRetry = 10; iRetry > 0 && iweReturn == null; iRetry--)
            {
                try
                {
                    iweReturn = driver.FindElement(By.CssSelector($".info-section:nth-child(3) .selectable:nth-child({nItem})")); // .Click();
                }
                catch (Exception ex)
                {
                }

                if (iweReturn == null)
                    System.Threading.Thread.Sleep(50);
            }
        }
        else
        {
            for (int iRetry = 10; iRetry > 0 && iweReturn == null; iRetry--)
            {
                try
                {
                    iweReturn = driver.FindElement(By.CssSelector($".info-section:nth-child(3) .selectable:nth-child({nItem}) span")); // .Click();
                }
                catch (Exception ex)
                {
                }


                if (iweReturn == null)
                    System.Threading.Thread.Sleep(50);
            }
        }

        return iweReturn;
    }

    private int QueryItemCount()
    {
        int cItems = 0;
        System.Threading.Thread.Sleep(4000);

        try
        {
            var eleItemCounter = driver.FindElement(By.CssSelector(".text-left"));
            if (eleItemCounter == null)
            {
                System.Diagnostics.Debug.WriteLine($"QueryItemCount: null value returned");
                return -1;
            }

            System.Threading.Thread.Sleep(5000);

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

}
