using System;
using System.IO;
using System.Diagnostics;

namespace WebCrawler
{
    class Program
    {
        [System.Runtime.InteropServices.DllImport("Kernel32")]
        public static extern void AllocConsole();

        private static readonly Uri rootUrl = new Uri("https://www.google.com/");

        private static readonly string rootPath = Path.Join(Directory.GetCurrentDirectory(), Program.rootUrl.Host);

        private static readonly string logPath = Path.Join(Program.rootPath, "crawler.log");

        private static void Initialize()
        {
            AllocConsole();

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

            Console.WriteLine("Running");

            var policy = new CrawlPolicy();
            var task = policy.DownloadPolicyAsync(Program.rootUrl, rootPath);
            task.Wait();
            if (!task.Result)
            {
                Trace.TraceError("Failed to obtain policy");
            }

            var agentPolicy = policy.GetAgentPolicy();
            
            var sitemap = new Sitemap(Program.rootUrl, policy.SitemapUri, rootPath, false);
            sitemap.Build(agentPolicy.Disallow, agentPolicy.Allow);

            var crawler = new Crawler(sitemap, Program.rootPath);
            crawler.Start();
        }
    }
}
