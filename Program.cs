using System;
using System.IO;
using System.Diagnostics;

namespace WebCrawler
{
    class Program
    {
        private static readonly Uri rootUrl = new Uri("https://www.google.com/");

        private static readonly string rootPath = Path.Join(Directory.GetCurrentDirectory(), Program.rootUrl.Host);

        private static readonly string logPath = Path.Join(Program.rootPath, "crawler.log");

        private static void Initialize()
        {
            if (Directory.Exists(Program.rootPath))
            {
                Directory.Delete(Program.rootPath, true);
            }
            
            Directory.CreateDirectory(Program.rootPath);

            Trace.Listeners.Add(new TextWriterTraceListener(Program.logPath, "CrawlerListener"));
            Trace.AutoFlush = true;
        }
        
        public static void Main(string[] args)
        {
            Program.Initialize();

            var settings = new Site.SiteSettings()
            {
                SaveRobotsFile = true,
                SaveSitemapFiles = false,
                SaveUrls = true,
                DeleteHtmlAfterScrape = true,
                SaveHosts = true,
                DoSerialize = true
            };
            var site = new Site(Program.rootUrl, Program.rootPath, settings);
            
            var policyCrawler = new CrawlPolicy(site);
            var task = policyCrawler.DownloadPolicyAsync();
            task.Wait();
            if (!task.Result)
            {
                Trace.TraceError("Failed to obtain policy");
            }

            var agentPolicy = policyCrawler.GetAgentPolicy();
            if (policyCrawler.SitemapFound)
            {
                // Static sitemap found
                site.Map = new Sitemap(site.Url, 
                                       policyCrawler.SitemapUrl,
                                       site.Path, 
                                       site.Settings.SaveSitemapFiles,
                                       site.Settings.SaveUrls);

                site.Map.Build(agentPolicy.Disallow, agentPolicy.Allow);
            }
            else
            {
                // Need to dynamically obtain sitemap graph
                throw new NotImplementedException();
            }
            
            var crawler = new Crawler(site);
            crawler.Start();

            site.HostsUrls = crawler.Hosts;
            site.Serialize();
        }
    }
}
