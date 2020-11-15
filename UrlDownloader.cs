using System;
using System.Collections.Generic;

namespace WebCrawler
{
    internal class UrlDonwloader
    {
        public string Site { get; }

        public string RobotsUrl { get { return this.Site + "robots.txt"; } }

        public bool IsRobots { get; private set; }

        public string SitemapUrl { get; private set; }

        public bool IsSitemap { get { return (this.SitemapUrl != null); } }

        public string ProjectPath { get; }

       

        private List<Uri> Bfs(Uri parent, List<Uri> staticUrls, List<Uri> disallowed)
        {
            var urls = new List<Uri>();

            var visited = new HashSet<Uri>();
            visited.Add(parent);

            var queue = new Queue<Uri>();
            queue.Enqueue(parent);
            foreach (var uri in disallowed)
            {
                queue.Enqueue(uri);
                visited.Add(uri);
            }

            while (queue.Count != 0)
            {
                var next = queue.Dequeue();
                if (visited.Contains(next))
                {
                    continue;
                }

                urls.Add(next);

                //var children = this.ParseUri(next);
                //foreach (var child in children)
                //{
                    //queue.Enqueue(child);
                //}
            }

            return urls;
        }

        public UrlDonwloader(string url, string projectPath)
        {
            this.Site = url;
            this.ProjectPath = projectPath;
        }

        public Site Start()
        {
            var site = new Site(new Uri(this.Site));

            // Robots file if any
            /*var robotsFile = Path.Join(this.ProjectPath, "robots");
            if (UrlDonwloader.Download(this.RobotsUrl, robotsFile))
            {
                site.RobotsFile = robotsFile;
            }

            // Retrieving urls from sitemap if any
            var staticUrls = this.ParseSitemapFromRobots(robotsFile);
            site.DisallowedUrls = staticUrls[1];
            
            // Discovering url by BFS traversal from url
            site.Urls = this.Bfs(site.Location, staticUrls[0], staticUrls[1]);*/

            return site;
        }
    }
}