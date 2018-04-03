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
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace DataGather
{
    class Program
    {
        static string url = ConfigurationManager.AppSettings["url"].ToString();
        static string urlType = ConfigurationManager.AppSettings["urlType"].ToString();
        static string dataSavePath = ConfigurationManager.AppSettings["dataSavePath"].ToString();
        static string tmpDataPath = "tmpData.txt";

        //static string proxyServerIP = ConfigurationManager.AppSettings["proxyServerIP"].ToString();
        //static string proxyServerPort = ConfigurationManager.AppSettings["proxyServerPort"].ToString();


        static void Main(string[] args)
        {
            if (urlType == "1")
                GatherTmallProDetail();
            else if (urlType == "2")
                GatherTmall();
            else if (urlType == "3")
                GatherJD();

            //Task t1 = new Task(GatherTmallTask);
            //Console.WriteLine(t1.Status);
            //t1.Start();
            //t1.Wait();

            //GatherJD();
        }

        static string proTitle = string.Empty;//商品名称
        /// <summary>
        /// 天猫商品详情页数据
        /// </summary>
        static void GatherTmallProDetail()
        {
            if (string.IsNullOrEmpty(url))
                url = "https://detail.tmall.com/item.htm?spm=a220m.1000858.1000725.6.65ad5cb28figxk&id=560558686226&skuId=3503382888333&areaId=440300&standard=1&user_id=1776456424&cat_id=2&is_b=1&rn=bb45738939e159e7978ac0c15c8826e9";

            Console.WriteLine(" ");
            Console.WriteLine("正在解析url...");
            IWebDriver driver = new PhantomJSDriver();
            try
            {
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(500);
                Console.WriteLine(" ");
                Console.WriteLine("url解析成功，即将进行数据采集...");

                proTitle = driver.FindElement(By.XPath("//li[@class='tm-relate-current']/span")).GetAttribute("title");
                var html = driver.PageSource;

                string str = getRegStr(html);
                if (str.Length > 0)
                {
                    var s = str.IndexOf("api");
                    var e = str.LastIndexOf("})();");

                    str = str.Substring(s, e - s);
                    str = str.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(");", "").Trim();
                    str = "{\"" + str;
                    WriteToTxt(str, tmpDataPath, FileMode.Create);
                    Thread.Sleep(1000);

                    string jsonStr = ReadFromTxt(tmpDataPath);
                    GetData(jsonStr);
                }
                else
                    Console.WriteLine("------数据匹配失败------");
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


        #region 数据处理

        /// <summary>
        /// 正则匹配出相关数据
        /// </summary>
        /// <param name="oldStr"></param>
        /// <param name="reg"></param>
        /// <returns></returns>
        static string getRegStr(string oldStr, string reg = @"<script>[\s\S]*?</script>")
        {
            var matchVal = string.Empty;
            MatchCollection mc = Regex.Matches(oldStr, reg);
            for (int i = 0; i < mc.Count; i++)
            {
                Match m = mc[i];
                if (m.Value.IndexOf("TShop.poc(") >= 0)
                {
                    matchVal = m.Value;
                    break;
                }
            }
            return matchVal;
        }

        /// <summary>
        /// 将Json缓存至txt
        /// </summary>
        /// <param name="str"></param>
        static void WriteToTxt(string str, string filePath, FileMode mode = FileMode.Create, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.ReadWrite)
        {
            FileStream file = new FileStream(filePath, mode, fileAccess, FileShare.ReadWrite);
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.Write(str);
                writer.Flush();
                writer.Close();
            }
            //关闭文件
            file.Close();
            //释放对象
            file.Dispose();
        }

        static bool IsExistFile(string _filePath)
        {
            return File.Exists(_filePath);
        }

        /// <summary>
        /// 从txt读取要解析的json数据
        /// </summary>
        /// <returns></returns>
        static string ReadFromTxt(string filePath)
        {
            var strJson = "";
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    strJson = sr.ReadToEnd();
                }
            }
            return strJson;
        }

        /// <summary>
        /// 得到最后的数据，并保存至txt
        /// </summary>
        /// <param name="strJson"></param>
        static void GetData(string strJson)
        {
            var sb = new StringBuilder();
            try
            {
                Console.WriteLine(" ");
                Console.WriteLine("采到以下数据数据，将保存至" + dataSavePath);
                Console.WriteLine(" ");

                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(strJson);
                var skuMap = obj.valItemInfo.skuMap;
                var skuList = obj.valItemInfo.skuList;

                foreach (var item in skuMap)
                {
                    foreach (var childItem in item)
                    {
                        var skuId = childItem.skuId;
                        var price = childItem.price;
                        var name = "";
                        foreach (var skuItem in skuList)
                        {
                            if (skuItem.skuId == skuId)
                            {
                                name = skuItem.names;
                                Console.WriteLine(proTitle + "，" + name + "， " + price);

                                sb.AppendFormat("{2}，{0}，{1}\r\n", name, price, proTitle);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (sb.Length > 0)
                {
                    FileMode fmode = FileMode.OpenOrCreate;
                    FileAccess faccess = FileAccess.ReadWrite;
                    if (IsExistFile(dataSavePath))
                    {
                        fmode = FileMode.Append;
                        faccess = FileAccess.Write;
                    }
                    WriteToTxt(sb.ToString(), dataSavePath, fmode, faccess);
                }
                Console.WriteLine("  ");
                Console.WriteLine("本次执行结束，数据已保存至" + dataSavePath);
            }
            Console.ReadKey();
        }
        #endregion



        #region 搜索列表页

        /// <summary>
        /// 天猫搜索列页表数据
        /// </summary>
        static public void GatherTmall()
        {
            var i = 0;
            IWebDriver driver = new PhantomJSDriver();
            //IWebDriver driver = new FirefoxDriver();
            //IWebDriver driver = new ChromeDriver();
            try
            {
                if (string.IsNullOrEmpty(url))
                    url = "https://list.tmall.com/search_product.htm?q=%B5%E7%C4%D4&type=p&vmarket=&spm=875.7931836%2FB.a2227oh.d100&from=mallfp..pc_1_searchbutton";

                driver.Navigate().GoToUrl(url);
                Thread.Sleep(500);
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
                        if (item.FindElements(By.ClassName("productTitle")).Count == 0)
                            continue;

                        i++;
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

        /// <summary>
        /// 京东搜索列表页数据
        /// </summary>
        private static void GatherJD()
        {
            var i = 0;
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

        #endregion

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
