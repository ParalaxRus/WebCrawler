using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebCrawler
{
    public class CrawlerConfiguration
    {
        /// <summary>Needed for serialization.</summary>
        public CrawlerConfiguration() { }

        public bool SaveRobotsFile { get; set; }

        public bool SaveSitemapFiles { get; set; }
        
        public bool SaveUrls { get; set; }

        /// <summary>A value indicating whether to delete every html page 
        /// after scraping and parsing is done. Site might have a lot of html pages and saving
        /// them locally on disk might be problematic.</summary>
        public bool DeleteHtmlAfterScrape { get; set; }

        /// <summary>A value indicating whether to serialize site object or not.</summary>
        public bool SerializeSite { get; set; }

        /// <summary>A value indicating whether to serialize graph object or not.</summary>
        public bool SerializeGraph { get; set; }
    }

    public class Site
    {
        /// <summary>Gets or sets sitemap object.</summary>
        [JsonIgnore]
        public Sitemap Map { get; set; }

        /// <summary>Gets or sets configuration.</summary>
        public CrawlerConfiguration Configuration { get; set; }

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

        /// <summary>Gets or sets number of pages to be scraped for this site.</summary>
        public int PagesToScrape { get; set; }

        public Site(Uri url, string path, CrawlerConfiguration configuration)
        {
            if (url == null)
            {
                throw new ArgumentException("siteUrl");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (configuration == null)
            {
                throw new ArgumentException("configuration");
            }

            this.Url = url;

            this.Path = System.IO.Path.Combine(path, this.Url.Host);
            if (Directory.Exists(this.Path))
            {
                Directory.Delete(this.Path);
            }
            Directory.CreateDirectory(this.Path);

            this.Configuration = configuration;
            this.RobotsPath = System.IO.Path.Combine(this.Path, "robots.txt");
            this.RobotsUrl = new Uri(this.Url + "robots.txt");
            this.GraphFile = System.IO.Path.Combine(this.Path, "graph.txt");
            this.SerializedSitePath = System.IO.Path.Combine(this.Path, "site.json");
            this.HtmlDownloadPath = System.IO.Path.Combine(this.Path, "Html");
            this.SiteDbFile = System.IO.Path.Combine(this.Path, "database.xml");
            this.PagesToScrape = 0;
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