using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace WebCrawler
{
    internal class Crawler
    {
        private Scraper scraper = null;

        private HashSet<Uri> hostUrls = new HashSet<Uri>();

        private BlockingCollection<string> blockingQueue = new BlockingCollection<string>();

        private Regex hrefPattern = new Regex("href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", 
                                              RegexOptions.IgnoreCase);

        /// <summary>A value indicating whether to delete html file after scraping is done or not.</summary>
        public bool DeleteAfterScrape { get; set; }

        /// <summary>Gets hosts urls.</summary>
        public HashSet<Uri> Hosts { get { return this.hostUrls; } }

        private bool IsAbsoluteHttps(string href, out Uri uri)
        {
            bool result = Uri.TryCreate(href, UriKind.Absolute, out uri);
            
            return result && ( (uri.Scheme == Uri.UriSchemeHttp) || (uri.Scheme == Uri.UriSchemeHttps) );
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

                    There is a bug here: Produces 0 hosts ...

                    try
                    {
                        Uri uri = null;
                        if (this.IsAbsoluteHttps(href, out uri))
                        {
                            var host = new Uri(uri.Host);

                            if (!hosts.Contains(host))
                            {
                                hosts.Add(host);
                            }
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

        private HashSet<Uri> ParseHosts(string file)
        {
            var hosts = new HashSet<Uri>();

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

            return hosts;
        }

        /// <summary>Adding url hosts while avoiding duplicates.</summary>
        private void Add(HashSet<Uri> hosts)
        {
            lock (this.hostUrls)
            {
                this.hostUrls.UnionWith(hosts);
            }
        }

        public Crawler(Sitemap sitemap, string path)
        {
            this.scraper = new Scraper(sitemap, path);
            this.DeleteAfterScrape = false;
        }

        public void Start()
        {
            // Dowloading htmls in parallel and adding them to the blocking queue
            var task = Task.Run(() => this.scraper.DownloadHtmls(this.blockingQueue) );

            while (!this.blockingQueue.IsCompleted)
            {
                var htmlFile = this.blockingQueue.Take();
                var nextHosts = this.ParseHosts(htmlFile);

                this.Add(nextHosts);
            }

            // Task should complete by now
        }
    }
}