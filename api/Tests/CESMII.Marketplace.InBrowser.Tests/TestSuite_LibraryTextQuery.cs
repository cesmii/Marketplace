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
            driver = new ChromeDriver();
            js = (IJavaScriptExecutor)driver;
            driver.Manage().Window.Maximize();
            driver.Navigate().GoToUrl("http://localhost:3000/library?p=1&t=10");
            vars = new Dictionary<String, Object>();
        }
        public void Dispose()
        {
            driver.Quit();
        }

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        [Theory]
        [MemberData(nameof(Marketplace_TestData_TextQuery_Vertical.MyData), MemberType = typeof(Marketplace_TestData_TextQuery_Vertical))]
        [MemberData(nameof(Marketplace_TestData_TextQuery_Category.MyData), MemberType = typeof(Marketplace_TestData_TextQuery_Category))]
        [MemberData(nameof(Marketplace_TestData_TextQuery_Publisher.MyData), MemberType = typeof(Marketplace_TestData_TextQuery_Publisher))]
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

            // Set "See All" in category
            // TestUtils.Set_Category_See_All_State_Selected(driver, iGroup);

            // Get count for all items.
            // We use this value to watch for a change in actual count.
            // int cMaxItems = TestUtils.GetItemCountFromWebpage($"GetMaxCount - TextQuery - {strItemName}", driver, -1, 10, 50);

            // Find the text query box.
            IWebElement? iwe = TestUtils.TryFindElement(driver, ".with-append");
            Assert.NotNull(iwe);

            // Enter item into text query.
            // iwe.Click();
            bool bSuccess = TestUtils.ClickWhenPageIsReady(driver, iwe, 50, 50);
            Assert.True(bSuccess);

            iwe.SendKeys(strItemName.Trim());
            iwe.SendKeys(Keys.Enter);

// Maybe we need this?!?
// System.Threading.Thread.Sleep(2000);

            // Query number items found.
            int cFound = TestUtils.GetItemCountFromWebpage($"TextQuery - {strItemName}", driver, cMaxItems, 20, 10);

            Assert.Equal(cExpected, cFound);
        }

    }
}
