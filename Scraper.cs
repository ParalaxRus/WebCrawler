using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace WebCrawler
{
    /// <summary>Web scraper downloads html files from the site using provided sitemap.</summary>
    internal class Scraper
    {
        string rootPath = null;

        Sitemap sitemap = null;

        Random random = new Random();

        public bool SaveHtmlFiles { get; set; }

        private Dictionary<int, Uri> GetHtmlUrls()
        {
            // Dictionary is needed:
            // 1) To avoid dplicates between htmlResources and (root + index.html)
            // 2) Simplifies and increases performance of Take() method
            var htmls = new Dictionary<int, Uri>();

            var htmlResources = this.sitemap.HtmlResources;
            for (int i = 0; i < htmlResources.Count; ++i)
            {
                htmls.Add(i, htmlResources[i]);
            }

            // Roots might also be html pages with index.html as default
            var roots = this.sitemap.Roots;
            for (int i = 0; i < roots.Count; ++i)
            {
                var indexHtml = new Uri(roots[i] + "index.html");
                htmls.Add(i + htmlResources.Count, indexHtml);
            }
            
            return htmls;
        }

        /// <summary>Takes pseudo-random (bucketized) 'count of elements' from urls.</summary>
        private List<Uri> Take(Dictionary<int, Uri> urls, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException("count");
            }

            int urlsPerBucket = urls.Count / count;

            // Pseudo-random = random inside buckets
            var buckets = new int[count];
            for (int i = 0; i < buckets.Length; ++i)
            {
                int start = i * urlsPerBucket;
                buckets[i] = this.random.Next(start, start + urlsPerBucket);
            }
            
            var sample = new List<Uri>();
            foreach (var index in buckets)
            {
                sample.Add(urls[index]);
                urls.Remove(index);
            }

            return sample;
        }

        private void SlowSequentialDownload(
            Dictionary<int, Uri> htmls, int[] delayInterval, BlockingCollection<string> queue)
        {
            var random = new Random();

            var samples = this.Take(htmls, 10);

            foreach (var uri in samples)
            {
                var delay = random.Next(delayInterval[0], delayInterval[1] + 1);
                Thread.Sleep(delay * 1000);

                var file = Path.Join(this.rootPath, uri.LocalPath);
                Directory.CreateDirectory(Path.GetDirectoryName(file));

                if (UriDonwload.Download(uri, file))
                {
                    // Producer: adding downloaded file
                    queue.Add(file);
                }

                if (!this.SaveHtmlFiles && File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        public Scraper(Sitemap sitemap, string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                throw new ArgumentException(rootPath);  
            }

            if (sitemap == null)
            {
                throw new NullReferenceException("sitemap");
            }

            this.rootPath = rootPath;
            this.sitemap = sitemap;
            this.SaveHtmlFiles = true;
        }

        public void DownloadHtmls(BlockingCollection<string> queue)
        {
            // Generating collection of possible html links
            var htmls = this.GetHtmlUrls();

            // Slowly and randomly donwloading html pages in order not to be banned by the site
            this.SlowSequentialDownload(htmls, new int[] { 4, 8 }, queue);

            // No more html links left for the currrent site
            queue.CompleteAdding();
        }
    }
}