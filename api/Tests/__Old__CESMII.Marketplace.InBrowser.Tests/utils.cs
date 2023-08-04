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
        // *** Data Definition Start 
        // Lookup tables of all available publishers, verticals, and processes (categories)
        private static string[] astrCategories =
        {
        "Air Compressing",
        "Analytics",
        "Analytics Lifecycle",
        "APS",
        "Asset Health",
        "Asset Monitoring",
        "Asset Optimization",
        "Asset Performance Management",
        "Augmented Reality",
        "Blowing",
        "Chilling",
        "CNC Monitoring",
        "Communications",
        "Continuous Improvement",
        "Continuous Manufacturing Intelligence",
        "Continuous Monitoring",
        "Continuous Supply Chain Intelligence",
        "Cost of Quality",
        "Data Analytics",
        "Data Collection",
        "Data Management",
        "Digital Manufacturing",
        "Digitized Quality Checks",
        "Edge Computing",
        "Energy Efficiency",
        "Energy Monitoring",
        "Enterprise Data Management",
        "Enterprise Integration",
        "Heating",
        "Historian",
        "IIoT",
        "In-House Quality Inspections",
        "Industrial Software",
        "Industry 4.0",
        "Inventory Management",
        "Inventory Optimization",
        "Logistics and Yard Solutions",
        "Machine Connectivity",
        "Machine Learning",
        "Machine Monitoring",
        "Machine Vision",
        "Machining",
        "Maintenance",
        "Manufacturing Operations Solutions",
        "Material Analysis Solutions",
        "Material Traceability",
        "MES",
        "Molding",
        "OEE",
        "OT Data Lake",
        "Palletizing",
        "Planning and Scheduling",
        "Plant Reliability",
        "Power Factor",
        "Power Quality",
        "Predictive Maintenance",
        "Process Optimization",
        "Product Quality",
        "Production Quality",
        "Quality Assessment",
        "Quality Assurance",
        "Quality Traceability",
        "Remote Monitoring",
        "Renewables Monitoring",
        "Short Interval Control",
        "Smart Manufacturing",
        "Supplier Inventory Visibility",
        "Supplier Quality",
        "Supply Chain",
        "Supply Chain Automation",
        "Supply Chain Management",
        "Supply Chain Optimization",
        "Warranty Management",
        "Workflow Mapping",
        };

        private static string[] astrVerticals =
        {
        "Academia",
        "Aerospace",
        "Agriculture",
        "Assembly",
        "Automotive",
        "Biotechnology",
        "Building Materials and Supplies",
        "Cement",
        "Chemical",
        "CNC",
        "Consumer and Paper Goods",
        "Consumer Products",
        "Defense",
        "Discrete Manufacturing",
        "Distribution",
        "Electronics",
        "Energy",
        "Energy (Oil & Gas)",
        "Food & Beverage",
        "Forestry",
        "Foundry",
        "Government",
        "Greenhouses",
        "Heavy Industry",
        "Hospitality",
        "Industrial Gas",
        "Intelligence",
        "Life Sciences",
        "Lumber",
        "Machine Shops",
        "Manufacturing",
        "Medical Devices",
        "Metals",
        "Mining",
        "Packaging",
        "Personal Care",
        "Petrochemical",
        "Pharmaceuticals",
        "Plastics",
        "Plastics and Rubber",
        "Power",
        "Process Manufacturing",
        "Pulp & Paper",
        "Refining",
        "Remanufacturing",
        "Retailers",
        "Sawmills",
        "Seafood",
        "Semiconductor",
        "Small-Medium Enterprises",
        "Steel",
        "Telecom",
        "Textile",
        "Utilities",
        };

        private static string[] astrPublishers =
        {
        "5G Technologies USA Ltd.",
        "Adapdix",
        "Amatrol",
        "Augury",
        "Beeond, Inc.",
        "C5MI Insight LLC",
        "CESMII - The Smart Manufacturing Institute",
        "CoreWatts",
        "Emerson",
        "Falkonry Inc.",
        "Litmus Automation",
        "SAS",
        "ShelfAware",
        "SymphonyAI Industrial",
        "ThinkIQ",
        "Toward Zero",
        };


        // *** Data Definition End


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
                var eleMoreLess = driver.FindElement(By.CssSelector($".info-section:nth-child({iGroup}) > .btn"));
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
                utils.Console_WriteLine($"MarketplaceSeeAllSeeLess: Exception: {ex.Message}");
            }
        }

        public static bool SendKeysToQueryBox(IWebDriver driver, string strValue, int cRetry)
        {
            Console_WriteLine($"SendKeysToQueryBox: Enter function");
            bool bSuccess = false;

            for (int iRetry = 0; iRetry < cRetry && !bSuccess; iRetry++)
            {
                try
                {
                    IWebElement iweQueryTextbox = GetQueryBox(driver, true, cRetry);
                    if (driver != null)
                    {
                        driver.FindElement(By.CssSelector(".with-append")).Click();
                        driver.FindElement(By.CssSelector(".with-append")).SendKeys(strValue);
                        driver.FindElement(By.CssSelector(".with-append")).SendKeys(Keys.Enter);
                        bSuccess = true;
                    }
                }
                catch (Exception ex)
                {
                    Console_WriteLine($"SendKeysToQueryBox: Exception - {ex.Message}");
                    bSuccess = false;
                    System.Threading.Thread.Sleep(10 * iRetry);
                }
            }

            return bSuccess;
        }

        public static IWebElement GetQueryBox(IWebDriver driver, bool bClear, int cRetry)
        {
            Console_WriteLine($"GetQueryBox: Enter function");
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
                        Console_WriteLine($"GetQueryBox: Exception - {ex.Message}");
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

        public static int QueryItemCount(IWebDriver driver, int cMaxItems, string strContext)
        {
            // System.Threading.Thread.Sleep(2000);

            int cItems = -1;

            // One loop for all cases.
            // 1st time, cMaxItems == -1, which means "error reading value"
            // 2nd and subsequent times, we use cMaxItems as an invalid value.
            for (int iRetry = 0; iRetry < 20; iRetry++)
            {
                int cTemp = ReadCount(driver);
                if (cTemp != cMaxItems && cTemp != -1)
                {
                    cItems = cTemp;
                    utils.Console_WriteLine($"QueryItemCount:[{strContext}]  iRetry:{iRetry}, cTemp:{cTemp}, cMaxItems:{cMaxItems}");
                    break;
                }
                System.Threading.Thread.Sleep(25);
            }

            return cItems;
        }

        public static int ReadCount(IWebDriver driver)
        {
            int cItems = -1;

            try
            {
                var eleItemCounter = driver.FindElement(By.CssSelector(".text-left"));
                if (eleItemCounter == null)
                {
                    utils.Console_WriteLine($"ReadCount: null value returned");
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
            {
                utils.Console_WriteLine($"ReadCount: Exception: {ex.Message}");
            }

            return cItems;
        }

        public static void Console_WriteLine(string strOutput)
        {
            DateTime dt = DateTime.Now;
            string strOutput2 = $"{dt.Year:0000}-{dt.Month:00}-{dt.Day:00}T{dt.Hour:00}:{dt.Minute:00}:{dt.Second:00}.{dt.Millisecond:000} {strOutput}";
            Console.WriteLine(strOutput2);
            System.Diagnostics.Debug.WriteLine(strOutput2);
        }

        public static string GetItemName(int iGroup, int iItem, bool bNoSpaces)
        {
            string strReturn = "";
            if (iGroup == 1)
            {
                if (iItem > -1 && iItem < astrVerticals.Length)
                    strReturn = "v_" + astrVerticals[iItem];
            }
            else if (iGroup == 2)
            {
                if (iItem > -1 && iItem < astrCategories.Length)
                    strReturn = "c_" + astrCategories[iItem];

            }
            else if (iGroup == 3)
            {
                if (iItem > -1 && iItem < astrPublishers.Length)
                    strReturn = "p_" + astrPublishers[iItem];

            }

            if  (bNoSpaces)
                strReturn = strReturn.Replace(" ", "_");
            return strReturn;
        }

        public static int FindItemInList(bool bStaging, int iGroup, string strItem)
        {
            string[] astrSearch = (bStaging && iGroup == 1) ? astrVerticals :
                                  (bStaging && iGroup == 2) ? astrCategories :
                                  (bStaging && iGroup == 3) ? astrPublishers :
                                  new string[0];

            string strItemLower = strItem.ToLower();
            if (astrSearch.Length > 0)
            {
                for (int iItem = 0; iItem < astrSearch.Length; iItem++)
                {
                    if (strItemLower == astrSearch[iItem].ToLower())
                        return iItem;
                }
            }

            return -1;
        }

    } // class utils
} // namespace CESMII.Marketplace.UI.Tests