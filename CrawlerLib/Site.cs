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

        /// <summary>A value indicating whether to save retrieved hosts to a 
        /// file on disk or not.</summary>
        public bool SaveHosts { get; set; }

        /// <summary>A value indicating whether to serialize site object or not.</summary>
        public bool DoSerialize { get; set; }
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
        public string HostsFile { get; set; }

        /// <summary>Gets or sets full path on disk where to store serialized site object.</summary>
        public string SerializedSitePath { get; set; }

        /// <summary>Gets or sets a full path on disk where to download html files during scrape.</summary>
        public string HtmlDownloadPath {get; set; }

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
            this.HostsFile = System.IO.Path.Combine(this.Path, "hosts.txt");
            this.SerializedSitePath = System.IO.Path.Combine(this.Path, url.Host + ".site.json");
            this.HtmlDownloadPath = System.IO.Path.Combine(this.Path, "Html");
        }

        public void Serialize()
        {
            if (!this.Configuration.DoSerialize)
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