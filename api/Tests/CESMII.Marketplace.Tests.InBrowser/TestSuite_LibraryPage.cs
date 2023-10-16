namespace Marketplace_InBrowser_Tests
{
    using Marketplace_InBrowser_Tests;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using Xunit;
    public class TestSuite_LibraryPage : IDisposable
    {
        public IWebDriver? driver { get; private set; }
        public IDictionary<String, Object> vars { get; private set; }
        public IJavaScriptExecutor js { get; private set; }
        public TestSuite_LibraryPage()
        {
            driver = TestUtils.CreateChromeDriver();
            js = (IJavaScriptExecutor)driver;
            driver.Manage().Window.Maximize();
            string? strStartUrl = TestUtils.GetStartUrl();
            driver.Navigate().GoToUrl(strStartUrl);
            vars = new Dictionary<String, Object>();
            System.Threading.Thread.Sleep(500);
        }
        public void Dispose()
        {
            if (driver != null)
            {
                driver.Quit();
                driver = null;
            }
        }


        [Fact]
        public void CorrectHomePageTitle_On_NavigateToLibraryPage()
        {
            string strTitle = (driver != null) ? driver.Title: "";
            Assert.Equal("Library | SM Marketplace | CESMII", strTitle);
        }

        [Fact]
        public void FoundTextSearchBox_On_NavigateToLibraryPage()
        {
            // Make sure text search box can be found.
            IWebElement? iwe = TestUtils.TryFindElement(driver, ".with-append");
            Assert.True(iwe != null);

            // Make sure text search box can accept input.
            string strInput = "Here is a search string";
            iwe.SendKeys(strInput);

            string strOutput = iwe.GetAttribute("value");
            Assert.Equal(strInput, strOutput);
        }

        [Fact]
        public void FoundClearAllButton_On_NavigateToLibraryPage()
        {
            // Make sure text search box can be found.
            IWebElement? iwe = TestUtils.TryFindElement(driver, ".ml-sm-auto");
            Assert.True(iwe != null);

            string strValue = iwe.Text;
            Assert.True(!String.IsNullOrEmpty(strValue));

            string[] astrOutput = strValue.Split(new char[] { '\r', '\n' });
            string strLabel = astrOutput[0];
            Assert.Equal("Clear All", strLabel);
        }

        [Fact]
        public void FoundItemCount_On_NavigateToLibraryPage()
        {
            bool bSuccess = false;

            // First, look for centered text -- used when count == zero
            // There are multiple such items, so we have to dig deeper.
            var MyCollection = driver.FindElements(By.CssSelector(".text-center"));
            if (MyCollection.Count == 4)
            {
                try
                {
                    string strValue = MyCollection[3].Text;
                    if (strValue == "There are no matching marketplace item records.")
                        bSuccess = true;
                }
                catch
                {
                }
            }

            // Otherwise, look for left-justified text -- used when count > zero
            if (!bSuccess)
            {
                // Find item count control
                IWebElement? iweItemCount = TestUtils.TryFindElement(driver, ".text-left", 5, 50);
                if (iweItemCount != null)
                {
                    var strText = iweItemCount.Text;
                    var astr = strText.Split(new char[] { ' ' });
                    if (astr.Length == 2)
                    {
                        bSuccess = int.TryParse(astr[0], out int cItems);
                    }
                }
            }

            Assert.True(bSuccess);
        }

        //[Fact]
        //public void FoundSMAppButtonInExpectedStates_On_NavigateToLibraryPage_And_Click()
        //{
        //    // Click on "SM_App" button (should be unselected)
        //    // IWebElement iwe = driver.FindElement(By.CssSelector("#\\36 275769bb7e0831201e5c3e2 > .not-selected"));
        //    IWebElement? iwe = TestUtils.TryFindElement(driver, "#\\36 275769bb7e0831201e5c3e2 > .not-selected");
        //    Assert.True(iwe != null);

        //    // Set to selected state
        //    // iwe.Click();
        //    TestUtils.ClickWhenPageIsReady(driver, iwe);

        //    // Make sure item is selected.
        //    //IWebElement iwe2 = driver.FindElement(By.CssSelector(".selected"));
        //    IWebElement? iwe2 = TestUtils.TryFindElement(driver, ".selected");
        //    Assert.True(iwe2 != null);
        //    Assert.True(iwe2.Text == "SM App");

        //    // Make sure not selected item is not found           
        //    IWebElement? iwe3 = null;
        //    iwe3 = TestUtils.TryFindElement(driver, "#\\36 275769bb7e0831201e5c3e2 > .not-selected");
        //    Assert.True(iwe3 == null);
        //}

        //[Fact]
        //public void FoundSMHardwareButtonInExpectedStates_On_NavigateToLibraryPage_And_Click()
        //{
        //    // Find SM_Hardware button (it should be in unselected state).
        //    IWebElement? iwe = TestUtils.TryFindElement(driver, "#\\36 29763866827ef2028a17d61 > .not-selected");
        //    Assert.True(iwe != null);

        //    // iwe.Click();
        //    TestUtils.ClickWhenPageIsReady(driver, iwe);

        //    // Make sure item is selected.
        //    IWebElement? iwe2 = TestUtils.TryFindElement(driver, ".selected");
        //    Assert.True(iwe2 != null);
        //    Assert.True(iwe2 != null);
        //    Assert.True(iwe2.Text == "SM Hardware");

        //    // Make sure not selected item is not found           
        //    IWebElement? iwe3 = null;
        //    iwe3 = TestUtils.TryFindElement(driver, "#\\36 29763866827ef2028a17d61 > .not-selected");
        //    Assert.True(iwe3 == null);
        //}

        //[Fact]
        //public void FoundSMProfileButtonInExpectedStates_On_NavigateToLibraryPage_And_Click()
        //{
        //    // Find "SM_Profile" button (it needs to be in unselected state)
        //    IWebElement? iwe = TestUtils.TryFindElement(driver, "#\\36 275769bb7e0831201e5c3e3 > .not-selected");
        //    Assert.True(iwe != null);

        //    // iwe.Click();
        //    TestUtils.ClickWhenPageIsReady(driver, iwe);
        //    System.Threading.Thread.Sleep(50);

        //    // Make sure item is selected.
        //    IWebElement? iwe2 = TestUtils.TryFindElement(driver, ".selected");
        //    Assert.True(iwe2 != null);
        //    Assert.True(iwe2 != null);
        //    Assert.True(iwe2.Text == "SM Profile");

        //    // Make sure not selected item is not found           
        //    IWebElement? iwe3 = TestUtils.TryFindElement(driver, "#\\36 275769bb7e0831201e5c3e3 > .not-selected");
        //    Assert.True(iwe3 == null);
        //}


        //[Fact]
        //public void FoundIndustryVerticalSeeAll_On_NavigateToLibraryPage()
        //{
        //    // Make sure See All can be found for first group.
        //    IWebElement? iwe = TestUtils.TryFindElement(driver, ".info-section:nth-child(1) > .btn");
        //    Assert.True(iwe != null);

        //    string strValue1 = iwe.Text;
        //    Assert.Equal("+ See all", strValue1);

        //    // iwe.Click();
        //    TestUtils.ClickWhenPageIsReady(driver, iwe);

        //    string strValue2 = iwe.Text;
        //    Assert.Equal("- See less", strValue2);
        //}

        //[Fact]
        //public void FoundProcessesSeeAll_On_NavigateToLibraryPage()
        //{
        //    // Make sure See All can be found for second group.
        //    IWebElement? iwe = TestUtils.TryFindElement(driver, ".info-section:nth-child(2) > .btn");
        //    Assert.True(iwe != null);

        //    string strValue = iwe.Text;
        //    Assert.Equal("+ See all", strValue);
        //}

        //[Fact]
        //public void FoundPublishersSeeAll_On_NavigateToLibraryPage()
        //{
        //    // Make sure See All can be found for third group.
        //    IWebElement? iwe = TestUtils.TryFindElement(driver, ".info-section:nth-child(3) > .btn");
        //    Assert.True(iwe != null);

        //    string strValue = iwe.Text;
        //    Assert.Equal("+ See all", strValue);
        //}

    }
}
