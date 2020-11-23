using System;
using System.IO;
using System.Diagnostics;

namespace WebCrawler
{
    class Program
    {
        private static readonly Uri rootUrl = new Uri("https://www.google.com/");
        
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener("crawler.log", "CrawlerListener"));
            Trace.AutoFlush = true;

            var rootPath = Path.Join(Directory.GetCurrentDirectory(), Program.rootUrl.Host);
            Directory.CreateDirectory(rootPath);

            var policy = new CrawlPolicy();
            var task = policy.DownloadPolicyAsync(Program.rootUrl, rootPath);
            task.Wait();
            if (!task.Result)
            {
                Trace.TraceError("Failed to obtain policy");
            }

            var agentPolicy = policy.GetAgentPolicy();
            
            // TO-DO:
            // 1) Filter languages in sitemap (lost of dups)
            // 2) Implement scraper to download index.html from roots
            // 3) What to do with resources ?!

            var sitemap = new Sitemap(Program.rootUrl, policy.SitemapUri, rootPath, false);
            sitemap.Build(agentPolicy.Disallow, agentPolicy.Allow);

            var scraper = new WebScraper(rootPath, sitemap);
            scraper.DownloadHtmls();
        }
    }
}
