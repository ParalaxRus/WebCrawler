using System;
using System.Collections.Generic;

namespace WebCrawler
{
    internal class Site
    {
        public Uri Location { get; set; }

        public string RobotsFile { get; set; }

        public string SitemapFile { get; set; }

        public List<Uri> Urls { get; set; }
        
        public List<Uri> DisallowedUrls { get; set; }

        public Site(Uri location)
        {
            this.Location = location;
        }
    }
}