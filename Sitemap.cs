using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Reflection;

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
        private HashSet<string> allow = null;

        private static bool EndsWith(string value, string[] endsWith)
        {
            if (endsWith == null)
            {
                return false;
            }

            foreach (var ends in endsWith)
            {
                if (value.EndsWith(ends))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Gets resource urls.</summary>
        /// <param name="endsWith">Resource pattern if any.</param>
        private List<Uri> GetResources(string[] endsWith = null)
        {
            var res = new List<Uri>();

            foreach (var uri in this.urls)
            {
                bool isResource = UrlHelper.IsResourse(uri);
                if (!isResource)
                {
                    continue;
                }

                if (Sitemap.EndsWith(uri.LocalPath, endsWith))
                {
                    res.Add(uri);
                }
            }

            return res;
        }

        /// <summary>Gets root urls.</summary>
        private List<Uri> GetRoots()
        {
            var res = new List<Uri>();

            foreach (var uri in this.urls)
            {
                bool isResource = UrlHelper.IsResourse(uri);
                if (!isResource)
                {
                    res.Add(uri);
                }
            }

            return res;
        }

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
                    var uri = new Uri(node["loc"].InnerText);
                    if (uri.LocalPath != "/") // root url
                    {
                        indexUrls.Add(uri);
                    }
                }

                // Urls
                foreach (XmlNode node in doc.GetElementsByTagName("url"))
                {
                    var uri = new Uri(node["loc"].InnerText);
                    if (uri.LocalPath != "/") // root url
                    {
                        urls.Add(uri);
                    }
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
                if (!UriDownload.Download(sitemapFile, fileOnDisk))
                {
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
                var uri = queue.Dequeue();
                if (visisted.Contains(uri))
                {
                    continue;
                }

                visisted.Add(uri);

                var nextUrls = this.GetSitemapUrls(uri);
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

        private HashSet<Uri> GetSiteUrlsParallel(Uri sitemap)
        {
            var visisted = this.CreateVisited(this.rootUrl, this.disallow);

            var urls = new HashSet<Uri>();

            // Dump BFS parallelism processing each level in parallel but syncing between levels
            // Refactor some sort of a blocking queue is preferrable but need to figure out 
            // proper producer consumer and parallel bfs with the blocking queue in this case ...

            var level = new List<Uri>();
            level.Add(sitemap);

            while (level.Count != 0)
            {
                var nextLevel = new ConcurrentBag<Uri>();

                Parallel.ForEach(level, (Uri url) => 
                {
                    lock (visisted)
                    {
                        if (visisted.Contains(url))
                        {
                            return;
                        }

                        visisted.Add(url);
                    }
                    
                    var nextUrls = this.GetSitemapUrls(url);
                    var indexUrls = nextUrls[0];
                    var concreteUrls = nextUrls[1];

                    foreach (var index in indexUrls)
                    {
                        nextLevel.Add(index);
                    }

                    lock (urls)
                    {
                        urls.UnionWith(concreteUrls);
                    }
                }); // Comlpetes when all urls from the same levels are processed

                level = nextLevel.ToList();
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

            var watch = new Stopwatch();
            watch.Start();

            this.urls = GetSiteUrlsParallel(this.sitemap);

            watch.Stop();

            Trace.TraceInformation(string.Format("{0}: {1}", MethodBase.GetCurrentMethod(), watch.Elapsed));
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
            this.saveUrls = saveUrls;
        }

        public void Build(HashSet<string> disallow, HashSet<string> allow)
        {
            this.disallow = disallow;
            this.allow = allow; // Need to take allow into account and unite with disallow !!!

            this.CreateFromStaticMap();

            if (!this.saveSitemapFiles)
            {
                Directory.Delete(this.rootPath, true);
            }

            this.WriteToFile();
        }

        public HashSet<Uri> RawUrls { get { return this.urls; } }

        public List<Uri> AllResources
        {
            get { return this.GetResources(); }
        }

        public List<Uri> HtmlResources
        {
            get 
            { 
                return this.GetResources(new string[] { ".htm", ".html" });
            }
        }

        public List<Uri> Roots
        {
            get { return this.GetRoots(); }
        }
    }
}