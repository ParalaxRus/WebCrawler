using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Linq;
using NetCollections;

[assembly:InternalsVisibleTo("CrawlerTests")]

namespace WebCrawler
{
    /// <summary>Web crawler class.</summary>
    public partial class Crawler
    {
        /// <summary>Crawler settings.</summary>
        private Configuration configuration = null;

        /// <summary>Seed hosts.</summary>
        private Uri[] seeds = null;

        /// <summary>Hosts graph.</summary>
        private Graph graph = new Graph();

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
                        if ((host != null) && !hosts.Contains(host) && (host.Scheme == "https"))
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
        private void Start(Uri host, Site site, Policy policy)
        {
            // Dowloading html pages in parallel and adding them to the blocking queue
            var scraper = new Scraper(site.Map, site.HtmlDownloadPath, this.ScraperCallback, this.cancellationToken);
            var counts = scraper.DiscoverLinks();
            site.UrlsToScrape = counts.Item1;
            site.DiscoveredUrls = counts.Item2;

            // Making sure that host contains links to other sites
            if (site.UrlsToScrape == 0)
            {
                Trace.TraceInformation(string.Format("Host {0} does not have external url links", host.Host));

                return;
            }

            var blockingQueue = new BlockingCollection<string>();

            var scrapeSettings = new Scraper.Settings(Scraper.Settings.DefaultCount, 
                                                      policy.CrawlDelayInSecs, 
                                                      Scraper.Settings.DefaultMaxDelay);
            var task = Task.Run(() => scraper.Scrape(blockingQueue, scrapeSettings));

            // scraper.Scrape() completes this loop
            while (!blockingQueue.IsCompleted)
            {
                var htmlFile = blockingQueue.Take();

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
                                       policy.Sitemaps,
                                       site.Path, 
                                       site.Configuration.SaveSitemapFiles,
                                       site.Configuration.SaveUrls,
                                       site.Configuration.HostUrlsLimit,
                                       site.Configuration.SitemapIndexLimit);

                site.Map.Build(policy);
            }
            else
            {
                // Need to dynamically obtain sitemap graph
                throw new NotImplementedException(
                    "Sitemap not found. Dynamic sitemap retrieval is not supported yet");
            }

            this.RaiseStatusEvent(string.Format("{0} sitemap obtained", site.Url.Host));
        }

        /// <summary>Creates queue from user provided seeds.</summary>
        private static PriorityLookupQueue<int, Uri> CreateQueue(Graph graph, Uri[] seeds)
        {
            var queue = new PriorityLookupQueue<int, Uri>();

            // Adding user provided seeds if not discovered yet
            foreach (var seed in seeds)
            {
                if (graph.Discovered(seed))
                {
                    // TO-DO: add support to discovery expiration
                    continue;
                }

                // User seed has a max priority
                queue.Enqueue(int.MaxValue, seed);
            }

            // Updating with hosts from just deserialized graph 
            Crawler.UpdateQueue(graph, queue);

            return queue;
        }

        /// <summary>Selecting seeds from graph and queue seeds.</summary>
        private static void UpdateQueue(Graph graph, PriorityLookupQueue<int, Uri> queue)
        {
            // Graph is being updated with the hosts children
            // They have to be included into the queue

            // Graph seeds
            var hosts = graph.GetVertices();
            foreach (var host in hosts)
            {
                var key = new Uri("https://" + host);
                if (!graph.Discovered(key))
                {
                    // Parent host is not fully discovered yet
                    // TO-DO: add support to discovery expirationS

                    // Hosts added with a max priority for now
                    queue.Enqueue(int.MaxValue, key);
                }

                // Adding children (connected sites)
                var edges = graph.GetEdges(key);
                foreach (var edge in edges)
                {
                    // Child might be fully discovered already
                    // TO-DO: add support to discovery expiration
                    if (!graph.Discovered(edge.Child))
                    {
                        queue.Enqueue(edge.Weight, edge.Child);
                    }
                }
            }
        }

        public Crawler(Configuration configuration, Uri[] seeds, CancellationToken token)
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
            this.seeds = seeds.Where(url => url.Scheme == "https").ToArray();
            this.cancellationToken = token;

            this.Init();
        }

        public void Crawl()
        {
            this.RaiseStatusEvent("Crawling started");
            this.RaiseProgressEvent(0.0);

            this.graph = Graph.Reconstruct(this.configuration.OutputPath);

            var queue = Crawler.CreateQueue(this.graph, this.seeds);
            while (queue.Count != 0)
            {
                var seed = queue.Dequeue().Value;

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

                    if (this.graph.Exists(seed))
                    {
                        Trace.TraceInformation(string.Format("Seed {0} exists in the graph", seed.Host));

                        // TO-DO: Rediscover in case if seed info is outdated

                        continue;
                    }

                    var attributes = new Dictionary<string, string>() 
                    {
                        { "robots", policy.IsRobots.ToString() },
                        { "sitemap", policy.IsSitemap.ToString() }
                    };

                    this.graph.AddVertex(seed, attributes);
                
                    this.Start(seed, site, policy);

                    this.graph.MarkAsDiscovered(seed);

                    info = string.Format("Crawling {0} completed", seed.Host);
                    Trace.TraceInformation(info);
                    this.RaiseStatusEvent(info);

                    Crawler.UpdateQueue(this.graph, queue);
                }
                catch (Exception exception)
                {
                    Trace.TraceError(string.Format("Failed to crawl {0}. {1}", seed.Host, exception.Message));

                    // Avoid crawling this host until a bug fix or 
                    // functionality implemented (should throw NotImplementedException)
                    this.graph.MarkAsDoNotProcess(seed);
                }
                finally
                {
                    if (!this.configuration.SaveRobotsFile)
                    {
                        File.Delete(site.RobotsPath);
                    }

                    if (this.configuration.SerializeGraph)
                    {
                        // Saving vertices under separate paths
                        this.graph.Persist(this.configuration.OutputPath, true);
                    }

                    // Deleting empty paths for downloaded html files
                    if ( site.Configuration.DeleteHtmlAfterScrape && Directory.Exists(site.HtmlDownloadPath) )
                    {
                        Directory.Delete(site.HtmlDownloadPath, true);
                    }

                    site.Serialize();
                }   
            }

            if (this.configuration.SerializeGraph)
            {
                // Persisting graph as a whole
                this.graph.Serialize(this.configuration.GraphFilePath);
            }

            this.RaiseStatusEvent("Crawling completed");
            this.RaiseProgressEvent(1.0);
        }
    }
}