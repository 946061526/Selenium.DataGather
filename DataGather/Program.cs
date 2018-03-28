using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace DataGather
{
    class Program
    {
        static string url = ConfigurationManager.AppSettings["url"].ToString();
        static string proxyServerIP = ConfigurationManager.AppSettings["proxyServerIP"].ToString();
        static string proxyServerPort = ConfigurationManager.AppSettings["proxyServerPort"].ToString();

        static void Main(string[] args)
        {
            //Task t1 = new Task(GatherTmallTask);
            //Console.WriteLine(t1.Status);
            //t1.Start();
            //t1.Wait();

            GatherJD();
        }

        static public void GatherTmallTask()
        {
            int i = 0;
            IWebDriver driver = new PhantomJSDriver();
            //IWebDriver driver = new FirefoxDriver();
            //IWebDriver driver = new ChromeDriver();
            try
            {
                if (string.IsNullOrEmpty(url))
                    url = "https://list.tmall.com/search_product.htm?q=%B5%E7%C4%D4&type=p&vmarket=&spm=875.7931836%2FB.a2227oh.d100&from=mallfp..pc_1_searchbutton";

                driver.Navigate().GoToUrl(url);
                Thread.Sleep(500);
                //var divContent = driver.FindElement(By.Id("content"));
                var pageCount = Convert.ToInt16(driver.FindElement(By.XPath("//input[@name='totalPage']")).GetAttribute("value"));
                IWebElement nextBtn;
                for (var p = 1; p <= pageCount; p++)
                {
                    Console.WriteLine("第 " + p + "页：");
                    //每次点击页码之后，都要重新找元素
                    driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 1));

                    IList<IWebElement> proList = driver.FindElements(By.XPath("//div[@id='J_ItemList']/div[@class='product  ']/div"));
                    foreach (var item in proList)
                    {
                        i++;
                        if (item.FindElements(By.ClassName("productTitle")).Count == 0)
                            continue;
                        IWebElement titleDiv = item.FindElement(By.ClassName("productTitle"));
                        IWebElement priceDiv = item.FindElement(By.TagName("em"));
                        string title = titleDiv != null ? titleDiv.Text : "";
                        string detailUrl = titleDiv != null ? titleDiv.FindElement(By.TagName("a")).GetAttribute("href") : "";
                        string price = priceDiv != null ? priceDiv.Text : "";
                        Console.WriteLine(string.Format("{0}--{1}，  {2}", i, title, price));
                    }

                    Thread.Sleep(5000);
                    nextBtn = driver.FindElement(By.XPath("//div[@id='content']/div/div[@class='ui-page']/div/b[@class='ui-page-num']/a[@class='ui-page-next']"));
                    nextBtn.Click();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                driver.Quit();
                Console.WriteLine("------程序已终止------");
            }
            Console.Read();
        }

        private static void GatherJD()
        {
            int i = 0;
            IWebDriver driver = new PhantomJSDriver();
            try
            {
                //var url = "https://search.jd.com/Search?keyword=%E6%89%8B%E6%9C%BA&enc=utf-8&pvid=c000d06501554fab9eb2017619927347";
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(100);
                var pageCount = Convert.ToInt16(driver.FindElement(By.XPath("//div[@id='J_bottomPage']/span[@class='p-skip']/em[1]/b")).Text);
                IWebElement nextBtn;
                for (var p = 1; p <= pageCount; p++)
                {
                    Console.WriteLine("第 " + p + "页：");
                    //每次点击页码之后，都要重新找元素
                    driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 1));
                    IList<IWebElement> proList = driver.FindElements(By.XPath("//div[@id='J_goodsList']/ul/li"));
                    foreach (var item in proList)
                    {
                        if (item.FindElements(By.ClassName("p-name")).Count == 0)
                            continue;
                        i++;
                        IWebElement titleDiv = item.FindElement(By.ClassName("p-name"));
                        IWebElement priceDiv = item.FindElement(By.ClassName("p-price")).FindElement(By.TagName("i"));
                        string title = titleDiv != null ? titleDiv.Text : "";
                        string price = priceDiv != null ? priceDiv.Text : "";
                        Console.WriteLine(string.Format("{0}--{1}，  {2}", i, title, price));
                    }

                    Thread.Sleep(5000);
                    nextBtn = driver.FindElement(By.XPath("//div[@id='J_bottomPage']/span[@class='p-num']/a[@class='pn-next']"));
                    nextBtn.Click();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                driver.Quit();
                Console.WriteLine("------程序已终止------");
            }
            Console.Read();
        }

        private static PhantomJSDriverService GetPhantomJSDriverService()
        {
            PhantomJSDriverService pds = PhantomJSDriverService.CreateDefaultService();
            ////设置代理服务器地址
            //pds.Proxy = string.Format("${0}:{1}",proxyServerIP,proxyServerPort); //$"{ip}:{port}";
            ////设置代理服务器认证信息
            //pds.ProxyAuthentication = GetProxyAuthorization();
            return pds;
        }
    }
}
