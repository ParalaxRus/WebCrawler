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

        /// <summary>Gets collection of site urls from sitemap file.</summary>
        /// <param name="sitemapFile">Sitemap file (can be index or sitemap file).</param>
        private HashSet<Uri>[] GetSitemapUrls(Uri sitemapFile)
        {
            // Sitemap file can be index file ir sitemap file. Index file contains uri of another
            // sitemap files and sitemap file contains site structure urls
            // However following logic allows to account for hybrid/mixed files

            var urls = new HashSet<Uri>[]
            {
                new HashSet<Uri>(), // Urls to more index files
                new HashSet<Uri>(), // Concrete urls
            };

            // File on disk to download sitemap file to
            var fileOnDisk = Path.Join(this.rootPath, sitemapFile.LocalPath);

            Directory.CreateDirectory(Path.GetDirectoryName(fileOnDisk));

            try
            {
                var res = new AsyncDonwload().DownloadAsync(sitemapFile, fileOnDisk);
                res.Wait();
                if (!res.Result)
                {
                    Trace.TraceError(string.Format("Failed to download sitemap file from {0} to {1}", 
                                                   sitemapFile.LocalPath, 
                                                   fileOnDisk));
                    
                    // Nothing could be retrieved
                    return urls;
                }

                this.Parse(fileOnDisk, urls[0], urls[1]);
            }
            finally
            {
                if (!this.saveSitemapFiles && File.Exists(fileOnDisk))
                {
                    // No need to keep sitemap file on disk after processing
                    File.Delete(fileOnDisk);
                }
            }

            return urls;
        }

        private HashSet<Uri> GetSiteUrls(Uri sitemap)
        {
            var visisted = this.CreateVisited(this.rootUrl, this.disallow);

            var urls = new HashSet<Uri>();

            var queue = new Queue<Uri>();
            queue.Enqueue(sitemap);

            while (queue.Count != 0)
            {
                var url = queue.Dequeue();
                if (visisted.Contains(url))
                {
                    continue;
                }

                visisted.Add(url);

                var nextUrls = this.GetSitemapUrls(url);
                var indexUrls = nextUrls[0];
                var concreteUrls = nextUrls[1];

                foreach (var index in indexUrls)
                {
                    queue.Enqueue(index);
                }
                urls.UnionWith(concreteUrls);
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

            this.urls = GetSiteUrls(this.sitemap);
        }

        private void WriteToFile()
        {
            if ( !this.saveUrls || (this.urls == null) )
            {
                return;
            }

            // Will reside in the same folder with robots.txt
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