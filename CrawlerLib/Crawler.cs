using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("CrawlerTests")]

namespace WebCrawler
{
    /// <summary>Web crawler class.</summary>
    public partial class Crawler
    {
        /// <summary>Crawler settings.</summary>
        private CrawlerConfiguration configuration = null;

        /// <summary>Seed hosts.</summary>
        private Uri[] seeds = null;

        /// <summary>Hosts graph.</summary>
        private Graph graph = new Graph();

        private BlockingCollection<string> blockingQueue = new BlockingCollection<string>();

        private Regex hrefPattern = new Regex("href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))",
                                              RegexOptions.IgnoreCase);

        private CancellationToken cancellationToken;

        private void Init()
        {
            // Creating output path if it does not exist
            Directory.CreateDirectory(this.configuration.OutputPath);

            if (!this.configuration.EnableLog)
            {
                return;
            }

            // Recreating log file on every run
            if (File.Exists(this.configuration.LogFilePath))
            {
                File.Delete(this.configuration.LogFilePath);
            }

            Trace.Listeners.Add(new TextWriterTraceListener(this.configuration.LogFilePath, "CrawlerListener"));
            Trace.AutoFlush = true;
            Trace.TraceInformation(DateTime.UtcNow.ToString());
        }

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
            this.graph.AddVertex(parent);

            // Updating database info
            foreach(var child in children)
            {
                this.graph.AddEdge(parent, child);
            }
        }

        private void ScraperCallback(double progress)
        {
            this.RaiseProgressEvent(progress);
        }

        /// <summary>Scrapes seed info and builds graph of the hosts with which 
        /// current seed url is connected.</summary>
        private void Start(Uri host, Site site)
        {
            // Dowloading html pages in parallel and adding them to the blocking queue
            var scraper = new Scraper(site.Map, site.HtmlDownloadPath, this.ScraperCallback, this.cancellationToken);
            var counts = scraper.DiscoverLinks();
            site.UrlsToScrape = counts.Item1;
            site.DiscoveredUrls = counts.Item2;

            var task = Task.Run(() => scraper.Scrape(this.blockingQueue));

            // scraper.Scrape() completes this loop
            while (!this.blockingQueue.IsCompleted)
            {
                var htmlFile = this.blockingQueue.Take();

                var nextHosts = this.ParseHosts(htmlFile, site.Configuration.DeleteHtmlAfterScrape);

                this.UpdateGraph(site.Url, nextHosts);
            }

            // Task should be done by now because blocking queue loop is over
            task.Wait();
        }

        private Policy RetrievePolicy(Site site)
        {
            this.RaiseStatusEvent(string.Format("{0} retrieving policy", site.Url.Host));

            var sitePolicy = new PolicyManager(site.Url, site.RobotsUrl, site.RobotsPath);
            var task = sitePolicy.RetrieveAsync();
            task.Wait();
            if (!task.Result)
            {
                var error = string.Format("{0} failed to obtain policy", site.Url.Host);

                this.RaiseStatusEvent(error);
                Trace.TraceError(error);
            }

            var policy = sitePolicy.GetPolicy(); // Empty policy if none detected

            this.RaiseStatusEvent(string.Format("{0} policy obtained", site.Url.Host));

            return policy;
        }

        private void RetrieveSitemap(Site site, Policy policy)
        {
            this.RaiseStatusEvent(string.Format("{0} retrieving sitemap", site.Url.Host));

            if (policy.IsSitemap)
            {
                // Static sitemap found
                site.Map = new Sitemap(site.Url, 
                                       policy.Sitemap,
                                       site.Path, 
                                       site.Configuration.SaveSitemapFiles,
                                       site.Configuration.SaveUrls);

                site.Map.Build(policy);
            }
            else
            {
                // Need to dynamically obtain sitemap graph
                throw new NotImplementedException();
            }

            this.RaiseStatusEvent(string.Format("{0} sitemap obtained", site.Url.Host));
        }

        /// <summary>Gets sites graph.</summary>
        public Graph CrawlerGraph { get {return this.graph; } }

        public Crawler(CrawlerConfiguration configuration, Uri[] seeds, CancellationToken token)
        {
            if (configuration == null)
            {
                throw new ArgumentException("configuration");
            }

            if ((seeds == null) || (seeds.Length == 0))
            {
                throw new ArgumentException("seeds");
            }

            this.configuration = configuration;
            this.seeds = seeds;
            this.cancellationToken = token;

            this.Init();
        }

        public void Crawl()
        {
            this.RaiseStatusEvent("Crawling started");
            this.RaiseProgressEvent(0.0);

            foreach (var seed in this.seeds)
            {
                if (this.cancellationToken.IsCancellationRequested)
                {
                    Trace.TraceInformation("Crawl cancel requested");
                    break;
                }

                var site = new Site(seed, this.configuration);

                try
                {
                    string info = string.Format("Crawling {0}", seed.Host);
                    Trace.TraceInformation(info);
                    this.RaiseStatusEvent(info);

                    var policy = this.RetrievePolicy(site);

                    this.RetrieveSitemap(site, policy);

                    if (this.graph.IsVertex(seed))
                    {
                        Trace.TraceInformation(string.Format("Seed {0} has already been discovered", seed.Host));

                        // TO-DO: Rediscover in case if seed info is outdated

                        continue;
                    }

                    var attributes = new Dictionary<string, object>() 
                    {
                        { "robots", policy.IsRobots },
                        { "sitemap", policy.IsSitemap }
                    };

                    this.graph.AddVertex(seed, attributes);
                
                    this.Start(seed, site);

                    info = string.Format("Crawling {0} completed", seed.Host);
                    Trace.TraceInformation(info);
                    this.RaiseStatusEvent(info);
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

            this.RaiseStatusEvent("Crawling completed");
            this.RaiseProgressEvent(1.0);
        }

        /// <summary>Gets collection of sites connected to the specified host.</summary>
        public Uri[] GetConnections(Uri host)
        {
            return this.graph.GetEdges(host);
        }
    }
}