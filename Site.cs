using System;
using System.IO;
using System.Collections.Generic;

namespace WebCrawler
{
    [Serializable]
    internal class Site
    {
        internal class SiteSettings
        {
            public bool SaveRobotsFile { get; set; }
            public bool SaveSitemapFiles { get; set; }
            public bool SaveUrls { get; set; }

            /// <summary>A value indicating whether to delete every html page 
            /// after scraping and parsing is done. Site might have a lot of html pages and saving
            /// them locally on disk might be problematic.</summary>
            public bool DeleteHtmlsAfterScrape { get; set; }

            /// <summary>A value indicating whether to save retrieved hosts to a 
            /// file on disk or not.</summary>
            public bool SaveHosts { get; set; }
        }

        public Uri Location { get; set; }

        public string PathOnDisk { get; set; }

        public SiteSettings Settings { get; private set; }

        public string RobotsFile { get; set; }

        public Uri RobotsFileUrl { get; set; }

        public Sitemap Map { get; set; }

        public string SitemapFile { get; set; }

        public Uri SitemapUrl { get; set; }

        public string HostsFile { get; set; }

        public HashSet<Uri> HostsUrls { get; set; }

        public Site(Uri location, string path, SiteSettings settings)
        {
            if (location == null)
            {
                throw new ArgumentException("location");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (settings == null)
            {
                throw new ArgumentException("settings");
            }

            this.Location = location;
            this.PathOnDisk = path;
            this.Settings = settings;

            this.RobotsFile = Path.Combine(this.PathOnDisk, "robots.txt");
            this.RobotsFileUrl = new Uri(this.Location + "robots.txt");
            this.HostsFile = Path.Combine(this.PathOnDisk, "hosts.txt");
        }
    }   
}