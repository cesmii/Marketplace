namespace Marketplace_InBrowser_Tests
{
    using Marketplace_InBrowser_Tests;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.DevTools.V112.ServiceWorker;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class TestSuite_LibraryTextQuery : IDisposable
    {
        public IWebDriver driver { get; private set; }
        public IDictionary<String, Object> vars { get; private set; }
        public IJavaScriptExecutor js { get; private set; }
        public TestSuite_LibraryTextQuery()
        {
            Console.WriteLine("Entering Function TestSuite_LibraryTextQuery()");
            driver = TestUtils.CreateChromeDriver();
            js = (IJavaScriptExecutor)driver;
            driver.Manage().Window.Maximize();
            string strStartUrl = TestUtils.GetStartUrl();
            Console.WriteLine($"TestSuite_LibraryTextQuery() -- strStartUrl:{strStartUrl}");
            driver.Navigate().GoToUrl(strStartUrl);
            vars = new Dictionary<String, Object>();
        }


        public void Dispose()
        {
            driver.Quit();
        }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        [Theory]
//        [MemberData(nameof(Marketplace_TestData_TextQuery_Vertical.MyData), MemberType = typeof(Marketplace_TestData_TextQuery_Vertical))]
        [MemberData(nameof(Marketplace_TestData_TextQuery_Category.MyData), MemberType = typeof(Marketplace_TestData_TextQuery_Category))]
//        [MemberData(nameof(Marketplace_TestData_TextQuery_Publisher.MyData), MemberType = typeof(Marketplace_TestData_TextQuery_Publisher))]
        public void CorrectItemCount_On_EnterItemNameIntoQueryBox(string strTestType, string strWebPageGroup, string strItemName, int iItemIndex, int cExpected, int cMaxItems)
        {
            // Validate incoming parameters
            Assert.Equal("textquery", strTestType);
            Assert.True(strItemName.Length > 0);
            Assert.True(cExpected > 0);

            int iGroup = TestUtils.GetGroupIndex(strWebPageGroup);
            bool bValidCategory = iGroup > -1;
            Assert.True(bValidCategory);

            // Select SM_App category
            TestUtils.Set_SM_App_State_Selected(driver);

            // Select SM_Hardware category
            TestUtils.Set_SM_Hardware_State_Selected(driver);

            // Make sure we start with all items currently selected.
            bool bMaxAtStart = TestUtils.WaitForMax(driver, cMaxItems, 100, 10);
            Assert.True(bMaxAtStart);

            // Find the text query box.
            IWebElement? iwe = TestUtils.TryFindElement(driver, ".with-append");
            Assert.NotNull(iwe);

            // Enter item into text query.
            bool bSuccess = TestUtils.ClickWhenPageIsReady(driver, iwe, 50, 50);
            Assert.True(bSuccess);

            iwe.SendKeys(strItemName.Trim());
            iwe.SendKeys(Keys.Enter);

// Maybe we need this?!?
// System.Threading.Thread.Sleep(2000);

            // Query number items found.
            int cFound = TestUtils.GetItemCountFromWebpage($"Text query for {strItemName}", driver, cMaxItems, 250, 50);

            Assert.Equal(cExpected, cFound);
        }

    }
}
