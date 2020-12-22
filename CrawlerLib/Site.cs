using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace WebCrawler
{
    [Serializable]
    public class Site
    {
        /// <summary>Needed for serialization.</summary>
        private Site() { }

        private string GetUrlStr(Uri url)
        {
            return url != null ? url.AbsoluteUri : null;
        }

        private Uri SetUrlStr(string value)
        {
            return value != null ? new Uri(value) : null;
        }

        [Serializable]
        public class SiteSettings
        {
            /// <summary>Needed for serialization.</summary>
            public SiteSettings() { }

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

        /// <summary>Gets or sets url of the site to be crawled.</summary>
        internal Uri Url { get; set; }

        /// <summary>Gets or sets url of the site's robots file if any.</summary>
        internal Uri RobotsUrl { get; set; }

        /// <summary>Gets or sets sitemap object.</summary>
        internal Sitemap Map { get; set; }

        /// <summary>Gets or sets url of the site's sitemap file if any.</summary>
        internal Uri SitemapUrl { get; set; }

        internal HashSet<Uri> HostsUrls { get; set; }

        #region Serializable properties

        [XmlElement("Url")]
        public string UrlStr
        {
            get { return this.GetUrlStr(this.Url); }
            set { this.Url = this.SetUrlStr(value); }
        }

        /// <summary>Gets or sets full path where to store site artifacts.</summary>
        public string Path { get; set; }

        /// <summary>Gets or sets configuration.</summary>
        public SiteSettings Settings { get; set; }

        /// <summary>Gets or sets full path on disk where to store robots file if any.</summary>
        public string RobotsPath { get; set; }

        /// <summary>Gets or sets url of the site's robots file if any.</summary>
        [XmlElement("RobotsUrl")]
        public string RobotsUrlStr
        {
            get { return this.GetUrlStr(this.RobotsUrl); }
            set { this.RobotsUrl = this.SetUrlStr(value); }
        }

        /// <summary>Gets or sets full path on disk where to store sitemap file if any.</summary>
        public string SitemapPath { get; set; }

        /// <summary>Gets or sets url of the site's robots file if any.</summary>
        [XmlElement("SitemapUrl")]
        public string SitemapUrlStr
        {
            get { return this.GetUrlStr(this.SitemapUrl); }
            set { this.SitemapUrl = this.SetUrlStr(value); }
        }

        /// <summary>Gets or sets full path on disk where to store hosts file.</summary>
        public string HostsFile { get; set; }

        /// <summary>Gets or sets full path on disk where to store serialized site object.</summary>
        public string SerializedSitePath { get; set; }

        /// <summary>Gets or sets a full path on disk where to download html files during scrape.</summary>
        public string HtmlDownloadPath {get; set; }

        #endregion

        public Site(Uri siteUrl, string path, SiteSettings settings)
        {
            if (siteUrl == null)
            {
                throw new ArgumentException("siteUrl");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (settings == null)
            {
                throw new ArgumentException("settings");
            }

            this.Url = siteUrl;
            this.Path = path;
            this.Settings = settings;
            this.RobotsPath = System.IO.Path.Combine(this.Path, "robots.txt");
            this.RobotsUrl = new Uri(this.Url + "robots.txt");
            this.HostsFile = System.IO.Path.Combine(this.Path, "hosts.txt");
            this.SerializedSitePath = System.IO.Path.Combine(this.Path, "site.xml");
            this.HtmlDownloadPath = System.IO.Path.Combine(this.Path, "Htmls");
        }

        public void Serialize()
        {
            if (!this.Settings.DoSerialize)
            {
                return;
            }

            var serializer = new XmlSerializer(typeof(Site));

            using (var writer = new StreamWriter(this.SerializedSitePath))
            {
                serializer.Serialize(writer, this);
            }
        }
    }   
}