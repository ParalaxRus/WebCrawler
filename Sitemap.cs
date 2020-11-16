using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;

namespace WebCrawler
{
    internal class Sitemap
    {
        private Uri rootUrl = null;
        private Uri sitemap = null;
        private string rootPath = null;
        private bool saveSitemapFiles = true;
        private bool saveUrls = true;
        private HashSet<Uri> urls = null;
        private HashSet<string> disallow = null;

        private HashSet<Uri> CreateVisited(Uri parent, HashSet<string> filterOut)
        {
            var visited = new HashSet<Uri>();

            foreach (var relative in filterOut)
            {
                var url = new Uri(parent + relative);

                visited.Add(url);
            }

            return visited;
        }

        private void Parse(string file, HashSet<Uri> indexUrls, HashSet<Uri> urls)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(file);

                // Index urls
                foreach (XmlNode node in doc.GetElementsByTagName("sitemap"))
                {
                    indexUrls.Add(new Uri(node["loc"].InnerText));
                }

                // Urls
                foreach (XmlNode node in doc.GetElementsByTagName("url"))
                {
                    urls.Add(new Uri(node["loc"].InnerText));
                }
            }
            catch(Exception exception)
            {
                Trace.TraceError(exception.Message);
            }
        }

        private HashSet<Uri> RetrieveUrls(Uri sitemap)
        {
            var visisted = this.CreateVisited(this.rootUrl, this.disallow);

            var urls = new HashSet<Uri>();

            var queue = new Queue<Uri>();
            queue.Enqueue(sitemap);

            while (queue.Count != 0)
            {
                var nextUri = queue.Dequeue();
                if (visisted.Contains(nextUri))
                {
                    continue;
                }

                visisted.Add(nextUri);

                var file = Path.Join(this.rootPath, nextUri.LocalPath);
                Directory.CreateDirectory(Path.GetDirectoryName(file));

                try
                {
                    var res = new AsyncDonwload().DownloadAsync(nextUri, file);
                    res.Wait();
                    if (!res.Result)
                    {
                        Trace.TraceError(string.Format("Failed to download {0} to {1}", nextUri.LocalPath, file));
                        continue;
                    }

                    var indexUrls = new HashSet<Uri>();
                    Parse(file, indexUrls, urls);
                    
                    foreach (var index in indexUrls)
                    {
                        queue.Enqueue(index);
                    }
                }
                finally
                {
                    if ( !this.saveSitemapFiles && File.Exists(file) )
                    {
                        File.Delete(file);
                    }
                }
            }

            return urls;
        }

        private void CreateFromStaticMap()
        {
            if (this.sitemap == null)
            {
                // Not provided
                return;
            }

            this.urls = RetrieveUrls(this.sitemap);
        }

        private void WriteToFile()
        {
            if ( !this.saveUrls || (this.urls == null) )
            {
                return;
            }

            var urlsPath = Path.Combine(Directory.GetParent(this.rootPath).FullName, "sitemap.txt");
            using (var file = File.CreateText(urlsPath))
            foreach (var url in this.urls)
            {
                file.WriteLine(url.LocalPath);
            }
        }

        public Sitemap(Uri rootUrl, Uri sitemap, string rootPath, bool saveSitemapFiles = true, bool saveUrls = true)
        {
            if (rootUrl == null)
            {
                throw new ArgumentNullException();
            }

            if (rootPath == null)
            {
                throw new ArgumentNullException();
            }

            this.rootUrl = rootUrl;
            this.sitemap = sitemap;
            this.rootPath = Path.Combine(rootPath, "Sitemap");
            this.saveSitemapFiles = saveSitemapFiles;
        }

        public void Build(HashSet<string> disallow)
        {
            this.disallow = disallow;

            this.CreateFromStaticMap();

            if (!this.saveSitemapFiles)
            {
                Directory.Delete(this.rootPath, true);
            }

            this.WriteToFile();
        }
    }
}