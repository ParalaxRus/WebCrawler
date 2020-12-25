using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace WebCrawler
{
    public class Crawler
    {
        /// <summary>Crawler settings.</summary>
        private CrawlerConfiguration configuration = null;

        /// <summary>Seed hosts.</summary>
        private Uri[] seeds = null;

        /// <summary>Full path on disk where to save crawl products.</summary>
        private string outputPath = null;


        private HashSet<Uri> hostUrls = new HashSet<Uri>();

        private BlockingCollection<string> blockingQueue = new BlockingCollection<string>();

        private Regex hrefPattern = new Regex("href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))",
                                              RegexOptions.IgnoreCase);

        private SiteDataBase siteDataBase = new SiteDataBase();

        private Uri GetHostUri(string href)
        {
            Uri uri = null;
            if (!Uri.TryCreate(href, UriKind.Absolute, out uri))
            {
                return null;
            }

            if ((uri.Scheme != Uri.UriSchemeHttp) && (uri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            return new UriBuilder(uri.Scheme, uri.Host).Uri;
        }

        /// <summary>Gets http/https hosts.</summary>
        private HashSet<Uri> GetHosts(string line)
        {
            var hosts = new HashSet<Uri>();

            Match match;
            for (match = this.hrefPattern.Match(line); match.Success; match = match.NextMatch())
            {
                foreach (var group in match.Groups)
                {
                    var href = group.ToString();

                    try
                    {
                        Uri host = this.GetHostUri(href);
                        if ((host != null) && !hosts.Contains(host))
                        {
                            hosts.Add(host);
                        }
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError(string.Format("Skipping {0}. Exception: {1}", href, exception.Message));
                    }
                }
            }

            return hosts;
        }

        private HashSet<Uri> ParseHosts(string file, bool deleteHtml)
        {
            var hosts = new HashSet<Uri>();

            try
            {
                using (var reader = File.OpenText(file))
                {
                    while (true)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        var hostsFromLine = this.GetHosts(line);
                        hosts.UnionWith(hostsFromLine);
                    }
                }
            }
            finally
            {
                if (deleteHtml && File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            return hosts;
        }

        private void Save(string file, bool saveHosts)
        {
            if (!saveHosts)
            {
                return;
            }

            using (var writer = new StreamWriter(file))
            {
                foreach (var host in this.Hosts)
                {
                    writer.WriteLine(host);
                }
            }
        }

        private void UpdateHosts(Uri parent, HashSet<Uri> children)
        {
            // Children of the current parent
            this.hostUrls.UnionWith(children);
            
            // Updating database info
            foreach(var child in children)
            {
                this.siteDataBase.AddHost(child.Host, false, false);
                this.siteDataBase.AddConnection(parent.Host, child.Host);
            }
        }

        /// <summary>Gets hosts urls.</summary>
        public HashSet<Uri> Hosts { get { return this.hostUrls; } }

        public Crawler(CrawlerConfiguration configuration, Uri[] seeds, string outputPath)
        {
            if (configuration == null)
            {
                throw new ArgumentException("configuration");
            }

            if ((seeds == null) || (seeds.Length == 0))
            {
                throw new ArgumentException("seeds");
            }

            if (!Directory.Exists(outputPath))
            {
                throw new ArgumentException("outputPath");
            }

            this.configuration = configuration;
            this.seeds = seeds;
            this.outputPath = outputPath;
        }

        public void Crawl()
        {
            foreach (var seed in this.seeds)
            {
                var site = new Site(seed, this.outputPath, this.configuration);

                try
                {
                    Trace.TraceInformation("Crawling: " + seed.Host);

                    // Determining site policy if any
                    var sitePolicy = new SitePolicy(site.RobotsUrl, site.RobotsPath);
                    var task = sitePolicy.DetectAsync();
                    task.Wait();
                    if (!task.Result)
                    {
                        Trace.TraceError("Failed to obtain policy");
                    }
                    var policy = sitePolicy.GetPolicy(); // Empty policy if none detected

                    if (policy.IsSitemap)
                    {
                        // Static sitemap found
                        site.Map = new Sitemap(site.Url, 
                                               policy.Sitemap,
                                               site.Path, 
                                               site.Configuration.SaveSitemapFiles,
                                               site.Configuration.SaveUrls);

                        site.Map.Build(policy.Disallow, policy.Allow);
                    }
                    else
                    {
                        // Need to dynamically obtain sitemap graph
                        throw new NotImplementedException();
                    }
                
                    // Might exist as a child of another host already. However does not have
                    // policy information yet because its crawling has not began
                    // so need to update table anyways
                    this.siteDataBase.AddHost(seed.Host, policy.IsRobots, policy.IsSitemap);

                    this.Start(seed, site);

                    Trace.TraceInformation(string.Format("Crawling {0} completed successfully", seed.Host));
                }
                catch (Exception exception)
                {
                    Trace.TraceError(string.Format("Failed to crawl {0}. {1}", seed.Host, exception.Message));
                }
                finally
                {
                    if (!this.configuration.SaveRobotsFile)
                    {
                        File.Delete(site.RobotsPath);
                    }

                    this.Save(site.HostsFile, site.Configuration.SaveHosts);

                    // Deleting empty paths for downloaded html files
                    if (site.Configuration.DeleteHtmlAfterScrape)
                    {
                        Directory.Delete(site.HtmlDownloadPath, true);
                    }

                    site.Serialize();
                }   
            }
        }

        /// <summary>Scrapes seed info and builds graph of the hosts with which 
        /// current seed url is connected.</summary>
        private void Start(Uri host, Site site)
        {
            // Dowloading html pages in parallel and adding them to the blocking queue
            var scraper = new Scraper(site.Map, site.HtmlDownloadPath);
            var task = Task.Run(() => scraper.DownloadHtmls(this.blockingQueue));

            // scraper.DownloadHtmls() completes this loop
            while (!this.blockingQueue.IsCompleted)
            {
                var htmlFile = this.blockingQueue.Take();

                var nextHosts = this.ParseHosts(htmlFile, site.Configuration.DeleteHtmlAfterScrape);

                this.UpdateHosts(site.Url, nextHosts);
            }

            // Task should be done by now because blocking queue loop is over
            task.Wait();
        }
    }
}