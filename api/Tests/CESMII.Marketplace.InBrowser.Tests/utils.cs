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
            catch (Exception)
            {

            }
        }

        public static bool MP_SendKeysToQueryBox(IWebDriver driver, string strValue, int cRetry)
        {
            Console_WriteLine($"MP_SendKeysToQueryBox: Enter function");
            bool bSuccess = false;

            for (int iRetry = 0; iRetry < cRetry && !bSuccess; iRetry++)
            {
                try
                {
                    IWebElement iweQueryTextbox = MP_GetQueryBox(driver, true, cRetry);
                    if (driver != null)
                    {
                        driver.FindElement(By.CssSelector(".with-append")).Click();
                        driver.FindElement(By.CssSelector(".with-append")).SendKeys(strValue);
                        driver.FindElement(By.CssSelector(".with-append")).SendKeys(Keys.Enter);
                        bSuccess = true;
                    }
                }
                catch
                {
                    Console_WriteLine($"MP_SendKeysToQueryBox: ***** exception ********");
                    bSuccess = false;
                    System.Threading.Thread.Sleep(10 * iRetry);
                }
            }

            return bSuccess;
        }

        public static IWebElement MP_GetQueryBox(IWebDriver driver, bool bClear, int cRetry)
        {
            Console_WriteLine($"MP_GetQueryBox: Enter function");
            IWebElement iwe = null;

            if (driver != null)
            {
                bool bFound = false;
                for (int iRetry = 0; iRetry < cRetry && bFound==false; iRetry++)
                {
                    try
                    {

                        iwe = driver.FindElement(By.CssSelector(".with-append"));
                        bFound = true;
                    }
                    catch (Exception ex) 
                    {
                        Console_WriteLine($"MP_GetQueryBox: ***** exception ********");
                    }
                }

                if (bClear && iwe != null)
                {
                    iwe.SendKeys(Keys.Control + "a");
                    iwe.SendKeys(Keys.Backspace);
                }
            }

            return iwe;
        }

        public static int MP_QueryPublisherItemCount(IWebDriver driver, int cMaxItems, string strContext)
        {
            // System.Threading.Thread.Sleep(2000);

            int cItems = -1;

            // One loop for all cases.
            // 1st time, cMaxItems == -1, which means "error reading value"
            // 2nd and subsequent times, we use cMaxItems as an invalid value.
            for (int iRetry = 0; iRetry < 20; iRetry++)
            {
                int cTemp = MP_ReadCount(driver);
                if (cTemp != cMaxItems && cTemp != -1)
                {
                    cItems = cTemp;
                    Console_WriteLine($"MP_QueryPublisherItemCount:[{strContext}]  iRetry:{iRetry}, cTemp:{cTemp}, cMaxItems:{cMaxItems}");
                    break;
                }
                System.Threading.Thread.Sleep(25);
            }

            return cItems;
        }

        public static int MP_ReadCount(IWebDriver driver)
        {
            int cItems = -1;

            try
            {
                var eleItemCounter = driver.FindElement(By.CssSelector(".text-left"));
                if (eleItemCounter == null)
                {
                    System.Diagnostics.Debug.WriteLine($"QueryItemCount: null value returned");
                    return -1;
                }

                var str2 = eleItemCounter.Text;
                var ai = str2.Split(new char[] { ' ' });
                if (ai.Length == 2)
                {
                    int.TryParse(ai[0], out cItems);
                }
            }
            catch { }

            return cItems;
        }

        public static void Console_WriteLine(string strOutput)
        {
            DateTime dt = DateTime.Now;
            string strOutput2 = $"{dt.Year:0000}-{dt.Month:00}-{dt.Day:00}T{dt.Hour:00}:{dt.Minute:00}{dt.Second:00}.{dt.Millisecond:000} {strOutput}";
            Console.WriteLine(strOutput2);
            System.Diagnostics.Debug.WriteLine(strOutput2);
        }


    } // class utils
} // namespace CESMII.Marketplace.UI.Tests