using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using Ivony.Html;
using Ivony.Html.Parser;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("开始下载图片" + DateTime.Now.ToString() );
            DownLoad();
            Console.WriteLine("下载完成:" + DateTime.Now.ToString());

            
            //如果用多线程，那就注释掉Save函数的webClient.DownloadFile方法
            //Parallel.ForEach(imgUrlList, r =>
            //{
            //    WebClient wb = InitWebClient();
            //    wb.DownloadFile(r.Url, r.Path);
            //});
            
            Console.ReadKey();
        }
        static List<UrlObj> imgUrlList = new List<UrlObj>();

        static WebClient InitWebClient()
        {
            System.Net.WebClient webClient = new System.Net.WebClient();
            webClient.Headers.Add("Host", "i1.umei.cc");
            webClient.Headers.Add("Pragma", "no-cache");
            webClient.Headers.Add("Cache-Control", "no-cache");
            webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36");
            webClient.Headers.Add("Accept", "image/webp,image/apng,image/*,*/*;q=0.8");
            webClient.Headers.Add("DNT", "1");
            webClient.Headers.Add("Referer", "http://www.umei.cc/meinvtupian/meinvmote/71726.htm");
            webClient.Headers.Add("Accept-Encoding", "gzip, deflate");
            webClient.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");

            return webClient;
        }

        static void DownLoad()
        {
            var mainUrl = "http://www.umei.cc/meinvtupian/siwameinv/";
            IHtmlDocument source = new JumonyParser().LoadDocument(mainUrl);

            //获取套图分页列表中最大页码
            var lastPageUrl = source.Find(".NewPages").First().Find("a").Last().Attribute("href").Value();
            var outPageCount =Convert.ToInt32(lastPageUrl.Split('/').Last().Split('.')[0]);

            List<string> urlList = new List<string>();

            //DownLoad(source, mainUrl, 1);

            //for (int outIndex = 6; outIndex <= 10; outIndex++)
            //{
            //    DownLoad(source, mainUrl, outIndex);
            //}
            Parallel.For(20, outPageCount, outIndex =>
            {
                DownLoad(source, mainUrl, outIndex);
            });
        }

        static void DownLoad(IHtmlDocument source,string mainUrl,int outIndex)
        {
            var item= mainUrl + outIndex + ".htm";
            
            source = new JumonyParser().LoadDocument(item);
            //第一页中套图列表
            var photoPage = source.Find(".TypeBigPics");
            var listTit = source.Find(".ListTit");

            //循环遍历分页页面
            for (int i = 0; i < photoPage.Count(); i++)
            {
                var pPath = AppDomain.CurrentDomain.BaseDirectory;
                var path = pPath + "/美女图片/" + listTit.ToList()[i].InnerText();

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }


                Console.WriteLine("开始下载:{0},开始时间:{1}", listTit.ToList()[i].InnerText(), DateTime.Now.ToString());
                //打开套图
                var pUrl = photoPage.ToList()[i].Attribute("href").Value();
                IHtmlDocument pHtml;
                try
                {
                    pHtml = new JumonyParser().LoadDocument(pUrl);
                }
                catch (Exception)
                {
                    Console.WriteLine(pUrl);
                    continue;
                }

                //保存第一页的图片
                OpenPage(pHtml, path);

                //获取分页页码
                var pages = pHtml.Find(".NewPages");
                if (pages.Count() > 0)
                {
                    var page = pages.First();
                    //获取当前套图总共多少页
                    var pageCount = Convert.ToInt32(Regex.Match(Regex.Match(page.InnerText(), "共\\d+页").Value, "\\d+").Value);
                    var links = page.Find("a");
                    var nextPage = links.Where(r => r.InnerText().Contains("下一页")).First();
                    var url = nextPage.Attribute("href").Value();

                    var headerHtml = url.Substring(0, url.IndexOf(url.Split('/').Last()));
                    var htmlCode = url.Split('/').Last().Split('_')[0];

                    //for (int index = 2; index < pageCount + 1; index++)
                    //{
                    //    var targeHtmlUrl = "http://www.umei.cc" + headerHtml + htmlCode + "_" + index + ".htm";
                    //    try
                    //    {
                    //        var newPage = new JumonyParser().LoadDocument(targeHtmlUrl);

                    //        OpenPage(newPage, path);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine(ex);
                    //        Console.WriteLine("错误链接:{0}", targeHtmlUrl);
                    //    }
                    //}
                    Parallel.For(2, pageCount + 1, index =>
                    {
                        var targeHtmlUrl = "http://www.umei.cc" + headerHtml + htmlCode + "_" + index + ".htm";
                        try
                        {
                            var newPage = new JumonyParser().LoadDocument(targeHtmlUrl);

                            OpenPage(newPage, path);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            Console.WriteLine("错误链接:{0}", targeHtmlUrl);
                        }
                    });
                }

                Console.WriteLine("结束下载:{0},结束时间:{1}", listTit.ToList()[i].InnerText(), DateTime.Now.ToString());
            }
        }

        static void OpenPage(IHtmlDocument source,string path)
        {
            var imageBody = source.Find(".ImageBody");
            if (imageBody.Count() > 0)
            {
                Save(imageBody, path);
            }
        }
        static void Save(IEnumerable<IHtmlElement> doc,string path)
        {
            foreach (var item in doc)
            {
                WebClient webClient= InitWebClient();
                var imgs = item.Find("img");
                foreach (var img in imgs)
                {
                    var src = img.Attribute("src").Value();
                    var filename = src.Split('/').Last();

                    //lock (imgUrlList)
                    //{
                    //    imgUrlList.Add(new UrlObj
                    //    {
                    //        Path = path + "/" + filename,
                    //        Url = src
                    //    });
                    //    //Console.WriteLine(path + "/" + filename);
                    //}

                    try
                    {
                        webClient.DownloadFile(src, path + "/" + filename);
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex);
                        continue;
                    }
                }
            }
        }

        class UrlObj
        {
            public string Url { get; set; }

            public string Path { get; set; }
        }

        static void DownLoad2()
        {
            IHtmlDocument source = new JumonyParser().LoadDocument("http://www.tuimo5.com/6778.html");
            var aLinks = source.Find(".img_jz");
            System.Net.WebClient webClient = new System.Net.WebClient();

            foreach (var item in aLinks)
            {
               var imgs= aLinks.Find("img");
                foreach (var img in imgs)
                {
                   var src= img.Attribute("src").Value();
                    var filename = src.Split('/').Last();
                    webClient.DownloadFile(src, filename);
                }
            }
            Console.ReadKey();

        }

    }
}
