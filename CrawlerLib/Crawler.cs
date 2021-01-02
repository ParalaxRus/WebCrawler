using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace WebCrawler
{
    /// <summary>Web crawler class.</summary>
    public partial class Crawler
    {
        /// <summary>Crawler settings.</summary>
        private CrawlerConfiguration configuration = null;

        /// <summary>Seed hosts.</summary>
        private Uri[] seeds = null;

        /// <summary>Full path on disk where to save crawl products.</summary>
        private string outputPath = null;

        /// <summary>Crawler graph discovered so far.</summary>
        private Graph graph = new Graph(true);

        private BlockingCollection<string> blockingQueue = new BlockingCollection<string>();

        private Regex hrefPattern = new Regex("href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))",
                                              RegexOptions.IgnoreCase);

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

        private void UpdateGraph(Uri parent, HashSet<Uri> children)
        {
            this.graph.AddParent(parent);

            // Updating database info
            foreach(var child in children)
            {
                this.graph.AddChild(parent, child);
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

                this.UpdateGraph(site.Url, nextHosts);
            }

            // Task should be done by now because blocking queue loop is over
            task.Wait();
        }

        private Agent RetrievePolicy(Site site)
        {
            this.RaiseStatusEvent("Retrieving policy", 0.0);

            var sitePolicy = new SitePolicy(site.RobotsUrl, site.RobotsPath);
            var task = sitePolicy.DetectAsync();
            task.Wait();
            if (!task.Result)
            {
                Trace.TraceError("Failed to obtain policy");
            }

            var policy = sitePolicy.GetPolicy(); // Empty policy if none detected

            this.RaiseStatusEvent("Policy obtained", 0.0);

            return policy;
        }

        private void RetrieveSitemap(Site site, Agent policy)
        {
            this.RaiseStatusEvent("Retrieving sitemap", 0.0);

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

            this.RaiseStatusEvent("Sitemap obtained", 0.0);
        }

        /// <summary>Gets crawler data base object.</summary>
        public DataBase CrawlerDataBase { get {return this.graph.CrawlDataBase; } }

        /// <summary>Gets sites graph.</summary>
        public Graph CrawlerGraph { get {return this.graph; } }

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
            this.RaiseStatusEvent("Crawling started", 0.0);

            foreach (var seed in this.seeds)
            {
                var site = new Site(seed, this.outputPath, this.configuration);

                try
                {
                    Trace.TraceInformation("Crawling: " + seed.Host);
                    this.RaiseStatusEvent(string.Format("Crawling {0}", seed.Host), 0.0);

                    var policy = this.RetrievePolicy(site);

                    RetrieveSitemap(site, policy);

                    if (this.graph.IsParent(seed))
                    {
                        Trace.TraceInformation(string.Format("Seed {0} has already been discovered", seed.Host));

                        // TO-DO: Rediscover in case if seed info is outdated

                        continue;
                    }

                    this.graph.AddParent(seed, policy.IsRobots, policy.IsSitemap);
                
                    this.Start(seed, site);

                    Trace.TraceInformation(string.Format("Crawling {0} completed successfully", seed.Host));
                    this.RaiseStatusEvent(string.Format("{0} completed", seed.Host), 1.0);
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

                    if (site.Configuration.SerializeGraph)
                    {
                        this.graph.Serialize(site.GraphFile, site.SiteDbFile);
                    }

                    // Deleting empty paths for downloaded html files
                    if (site.Configuration.DeleteHtmlAfterScrape)
                    {
                        Directory.Delete(site.HtmlDownloadPath, true);
                    }

                    site.Serialize();
                }   
            }

            this.RaiseStatusEvent("Crawling completed", 1.0);
        }

        /// <summary>Gets collection of sites connected to the specified host.</summary>
        public Uri[] GetConnections(Uri host)
        {
            return this.graph.GetChildren(host);
        }
    }
}