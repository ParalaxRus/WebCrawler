using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;

namespace WebCrawler
{
    /// <summary>Sitemap class helps to gather host web pages which could be scraped.</summary>
    public class Sitemap
    {
        private Uri host = null;
        private Uri sitemap = null;
        private string rootPath = null;
        private bool saveSitemapFiles = true;
        private bool saveUrls = true;
        private Policy policy = null;

        /// <summary>Host links which are allowed to be scraped.</summary>
        private HashSet<Uri> urls = null;

        /// <summary>Total number discovered links at this host (including disallowed).</summary>
        private int discoveredUrls = 0;

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
                bool isResource = UrlHelper.IsResource(uri);
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
                bool isResource = UrlHelper.IsResource(uri);
                if (!isResource)
                {
                    res.Add(uri);
                }
            }

            return res;
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
                    if ((uri.Scheme == "https") && (uri.LocalPath != "/")) // https and not root url
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

        /// <summary>Download sitemap file, parses it and extracts https links 
        /// to scrape and/or another sitemap files.</summary>
        private HashSet<Uri>[] DownloadAndParse(Uri sitemap)
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
            var fileOnDisk = Path.Join(this.rootPath, sitemap.LocalPath);

            Directory.CreateDirectory(Path.GetDirectoryName(fileOnDisk));

            try
            {
                if (!UriDownload.Download(sitemap, fileOnDisk))
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

        /// <summary>Refactor - looks ugly !</summary>
        private void AddNext(Queue<Uri> indices, HashSet<Uri> urls, HashSet<Uri>[] next, bool isThreadSafe)
        {
            var indexUrls = next[0];
            var regularUrls = next[1];

            // Sitemap index files
            foreach (var index in indexUrls)
            {
                var localPath = index.LocalPath;

                // Unlikely that sitemap index is dissalowed but still worth checking
                if (!this.policy.IsAllowed(localPath))
                {
                    Trace.TraceWarning(string.Format("Policy disallows index file {0} to be crawled", 
                                                     index.AbsolutePath));

                    continue;
                }

                if (isThreadSafe)
                {
                    indices.Enqueue(index);
                }
                else
                {
                    lock (indices)
                    {
                        indices.Enqueue(index);
                    }
                }
            }

            // Regular urls
            foreach (var regular in regularUrls)
            {
                var localPath = regular.LocalPath;

                if (!this.policy.IsAllowed(localPath))
                {
                    Trace.TraceWarning(string.Format("Policy disallows {0} to be crawled", 
                                                     regular.AbsolutePath));

                    continue;
                }

                if (isThreadSafe)
                {
                    urls.Add(regular);
                }
                else
                {
                    lock (urls)
                    {
                        urls.Add(regular);
                    }
                }
            }

            if (isThreadSafe)
            {
                this.discoveredUrls += next[1].Count;
            }
            else
            {
                Interlocked.Add(ref this.discoveredUrls, next[1].Count);
            }
        }

        private HashSet<Uri> RetrieveUsingSitemap(Uri sitemapIndex)
        {
            var urls = new HashSet<Uri>();

            var visited = new HashSet<string>();

            var sitemapIndices = new Queue<Uri>();
            sitemapIndices.Enqueue(sitemapIndex);

            while (sitemapIndices.Count != 0)
            {
                var currentIndex = sitemapIndices.Dequeue();

                var indexLocalPath = currentIndex.LocalPath;
                if (visited.Contains(indexLocalPath))
                {
                    continue;
                }
                visited.Add(indexLocalPath);

                var next = this.DownloadAndParse(currentIndex);

                this.AddNext(sitemapIndices, urls, next, true);
            }

            return urls;
        }

        private HashSet<Uri> RetrieveUsingSitemapParallel(Uri sitemapindex)
        {
            var urls = new HashSet<Uri>();

            var visited = new HashSet<string>();

            // Dump BFS parallelism processing each level in parallel but syncing between levels
            // Refactor some sort of a blocking queue is preferrable but need to figure out 
            // proper producer consumer and parallel bfs with the blocking queue in this case ...

            var sitemapIndices = new Queue<Uri>();
            sitemapIndices.Enqueue(sitemapindex);

            while (sitemapIndices.Count != 0)
            {
                var nextLevelIndices = new Queue<Uri>();

                Parallel.ForEach(sitemapIndices, (Uri currentIndex) => 
                {
                    var indexLocalPath = currentIndex.LocalPath;
                    
                    lock (visited)
                    {
                        if (visited.Contains(indexLocalPath))
                        {
                            return;
                        }

                        visited.Add(indexLocalPath);
                    }
                    
                    var next = this.DownloadAndParse(currentIndex);

                    this.AddNext(nextLevelIndices, urls, next, false);

                }); // Completes when all urls from the same levels are processed

                sitemapIndices = nextLevelIndices;
            }

            return urls;
        }

        /// <summary>Collects links to host pages which could be scraped.</summary>
        private HashSet<Uri> RetrieveUrls()
        {
            if (this.sitemap == null)
            {
                // Not provided
                return new HashSet<Uri>();
            }

            var watch = new Stopwatch();
            watch.Start();

            var urls = this.RetrieveUsingSitemapParallel(this.sitemap);

            watch.Stop();

            Trace.TraceInformation(string.Format("{0}: {1}", MethodBase.GetCurrentMethod(), watch.Elapsed));

            return urls;
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

            this.host = rootUrl;
            this.sitemap = sitemap;
            this.rootPath = Path.Combine(rootPath, "Sitemap");
            this.saveSitemapFiles = saveSitemapFiles;
            this.saveUrls = saveUrls;
        }

        public void Build(Policy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException();
            }

            this.policy = policy;

            this.urls = this.RetrieveUrls();

            if (!this.saveSitemapFiles)
            {
                Directory.Delete(this.rootPath, true);
            }

            this.WriteToFile();
        }

        public int DiscoveredUrls {get { return this.discoveredUrls; }}

        public HashSet<Uri> Urls { get { return this.urls; } }

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