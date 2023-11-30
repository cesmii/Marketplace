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

        public static int QueryItemCount(IWebDriver driver)
        {
            System.Threading.Thread.Sleep(2000);
            var eleItemCounter = driver.FindElement(By.CssSelector(".text-start"));
            if (eleItemCounter == null)
            {
                System.Diagnostics.Debug.WriteLine($"QueryItemCount: null value returned");
                return -1;
            }

            System.Threading.Thread.Sleep(2000);

            var str2 = eleItemCounter.Text;
            var ai = str2.Split(new char[] { ' ' });
            int cItems = -1;
            if (ai.Length == 2)
            {
                int.TryParse(ai[0], out cItems);
            }

            return cItems;
        }


    } // class utils
} // namespace CESMII.Marketplace.UI.Tests
