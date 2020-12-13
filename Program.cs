﻿using System;
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
                DeleteHtmlsAfterScrape = true,
                SaveHosts = true
            };
            var site = new Site(Program.rootUrl, Program.rootPath, settings);
            
            var policy = new CrawlPolicy(site);
            var task = policy.DownloadPolicyAsync();
            task.Wait();
            if (!task.Result)
            {
                Trace.TraceError("Failed to obtain policy");
            }

            var agentPolicy = policy.GetAgentPolicy();
            if (policy.SitemapFound)
            {
                // Static sitemap found
                site.Map = new Sitemap(site.Location, 
                                       policy.SitemapUrl,
                                       site.PathOnDisk, 
                                       site.Settings.SaveSitemapFiles,
                                       site.Settings.SaveUrls);

                site.Map.Build(agentPolicy.Disallow, agentPolicy.Allow);
            }
            else
            {
                // Need to build sitemap grpah by scraping site pages
                throw new NotImplementedException();
            }
            
            var crawler = new Crawler(site);
            crawler.Start();

            site.HostsUrls = crawler.Hosts;
        }
    }
}
