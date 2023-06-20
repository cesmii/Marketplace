using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.Marketplace.UI.Tests
{
    internal class utils
    {
        public static void PublisherListShowAll(IWebDriver driver)
        {
            try
            {
                var eleMoreLess = driver.FindElement(By.CssSelector(".info-section:nth-child(3) > .btn"));
                if (eleMoreLess != null)
                {
                    string strValue = eleMoreLess.Text;
                    if (strValue == "+ See all")
                        eleMoreLess.Click();
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
