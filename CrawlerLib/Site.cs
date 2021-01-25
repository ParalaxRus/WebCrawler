using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebCrawler
{
    public class Site
    {
        /// <summary>Gets or sets sitemap object.</summary>
        [JsonIgnore]
        public Sitemap Map { get; set; }

        /// <summary>Gets or sets configuration.</summary>
        public Configuration Configuration { get; set; }

        /// <summary>Gets or sets url of the site to be crawled.</summary>
        public Uri Url { get; set; }

        /// <summary>Gets or sets full path where to store site under crawl.</summary>
        public string Path { get; set; }

        /// <summary>Gets or sets url of the site's robots file if any.</summary>
        public Uri RobotsUrl { get; set; }

        /// <summary>Gets or sets full path on disk where to store robots file if any.</summary>
        public string RobotsPath { get; set; }

        /// <summary>Gets or sets url of the site's sitemap file if any.</summary>
        public Uri SitemapUrl { get; set; }

        /// <summary>Gets or sets full path on disk where to store sitemap file if any.</summary>
        public string SitemapPath { get; set; }

        [JsonIgnore]
        public HashSet<Uri> HostsUrls { get; set; }

        /// <summary>Gets or sets full path on disk where to store hosts file.</summary>
        public string GraphFile { get; set; }

        /// <summary>Gets or sets full path on disk where to store serialized site object.</summary>
        public string SerializedSitePath { get; set; }

        /// <summary>Gets or sets a full path on disk where to download html files during scrape.</summary>
        public string HtmlDownloadPath {get; set; }

        /// <summary>Gets or sets site database file.</summary>
        public string SiteDbFile { get; set; }

        /// <summary>Gets or sets total number of discovered pages.</summary>
        public int DiscoveredUrls { get; set; }

        /// <summary>Gets or sets number of pages to be scraped.</summary>
        public int UrlsToScrape { get; set; }

        public Site(Uri url, Configuration configuration)
        {
            if (url == null)
            {
                throw new ArgumentException("siteUrl");
            }

            if (configuration == null)
            {
                throw new ArgumentException("configuration");
            }

            this.Url = url;
            this.Configuration = configuration;

            this.Path = System.IO.Path.Combine(this.Configuration.OutputPath, this.Url.Host);
            if (Directory.Exists(this.Path))
            {
                Directory.Delete(this.Path, true);
            }
            Directory.CreateDirectory(this.Path);

            this.RobotsPath = System.IO.Path.Combine(this.Path, "robots.txt");
            this.RobotsUrl = new Uri(this.Url + "robots.txt");
            this.GraphFile = System.IO.Path.Combine(this.Path, Graph.GraphFileName);
            this.SerializedSitePath = System.IO.Path.Combine(this.Path, "site.json");
            this.HtmlDownloadPath = System.IO.Path.Combine(this.Path, "Html");
            this.SiteDbFile = System.IO.Path.Combine(this.Path, "database.xml");
            this.DiscoveredUrls = 0;
            this.UrlsToScrape = 0;
        }

        public void Serialize()
        {
            if (!this.Configuration.SerializeSite)
            {
                return;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            var jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(this.SerializedSitePath, jsonString);
        }
    }   
}