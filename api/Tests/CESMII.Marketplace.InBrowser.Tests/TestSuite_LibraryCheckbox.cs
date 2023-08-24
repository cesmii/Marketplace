using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools.V112.ServiceWorker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace_InBrowser_Tests
{
    public class TestSuite_LibraryCheckbox : IDisposable
    {
        public IWebDriver driver { get; private set; }
        public IDictionary<String, Object> vars { get; private set; }
        public IJavaScriptExecutor js { get; private set; }

        public TestSuite_LibraryCheckbox()
        {
            driver = TestUtils.CreateChromeDriver();
            js = (IJavaScriptExecutor)driver;
            driver.Manage().Window.Maximize();
            string strStartUrl = TestUtils.GetStartUrl();
            driver.Navigate().GoToUrl(strStartUrl);

            vars = new Dictionary<String, Object>();
        }

        public void Dispose()
        {
            driver.Quit();
        }

        //[Theory]
        //[MemberData(nameof(Marketplace_TestData_Checkbox_Vertical.MyData), MemberType = typeof(Marketplace_TestData_Checkbox_Vertical))]
        //[MemberData(nameof(Marketplace_TestData_Checkbox_Category.MyData), MemberType = typeof(Marketplace_TestData_Checkbox_Category))]
        //[MemberData(nameof(Marketplace_TestData_Checkbox_Publisher.MyData), MemberType = typeof(Marketplace_TestData_Checkbox_Publisher))]
        public void CorrectItemCount_On_ClickOnCheckbox(string strTestType, string strWebPageGroup, string strExpectedItemName, int iItemIndex, int cExpected, int cMaxItems)
        {
            // Validate incoming parameters
            Assert.Equal("checkbox", strTestType);
            Assert.True(strExpectedItemName.Length > 0);
            Assert.True(cExpected > 0);

            int iGroup = TestUtils.GetGroupIndex(strWebPageGroup);
            bool bValidCategory = iGroup > -1;
            Assert.True(bValidCategory);

            // Select SM_App category
            TestUtils.Set_SM_App_State_Selected(driver);

            // Select SM_Hardware category
            TestUtils.Set_SM_Hardware_State_Selected(driver);

            // Set "See All" in for the group of checkboxes we are testing
            TestUtils.Set_Category_See_All_State_Selected(driver, iGroup);
//            System.Threading.Thread.Sleep(50);

            // Get the checkbox in question
            IWebElement? iwe = TestUtils.GetWebElementFromGroup(driver, iGroup, iItemIndex);
//            System.Threading.Thread.Sleep(50);

            string strNameOnWebPage = (iwe == null || iwe.Text == null) ? "" : iwe.Text;
            Assert.Equal(strExpectedItemName.Trim(), strNameOnWebPage.Trim());

            // Make sure we start with all items currently selected.
            bool bMaxAtStart = TestUtils.WaitForMax(driver, cMaxItems, 100, 10);
            Assert.True(bMaxAtStart);

            // Click on selected category.
            bool bSuccess = (driver == null || iwe == null) ? false : TestUtils.ClickWhenPageIsReady(driver, iwe, 50, 10);
            Assert.True(bSuccess);

            // Query number items found.
            int cFound = TestUtils.GetItemCountFromWebpage($"Checkbox query for {strNameOnWebPage}", driver, cMaxItems, 250, 50);

            Assert.Equal(cExpected, cFound);
        }


        public static bool ClickWhenPageIsReady(IWebDriver d, IWebElement e, int retry = 10, int msWait = 50)
        {
            bool bSuccess = false;
            bool bFound = true;
            while (bFound)
            {
                IWebElement? iwePreloader = TestUtils.TryFindElement(d, "preloader", 0, 0);
                bFound = (iwePreloader != null);
            }

            try
            {
                e.Click();
                bSuccess = true;
            }
            catch { }

            return bSuccess;
        }


    }

}
