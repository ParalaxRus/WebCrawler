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
        Sitemap sitemap = null;

        string htmlDownloadPath = null;

        Random random = new Random();

        private Dictionary<int, Uri> GetHtmlUrls()
        {
            // Dictionary is needed:
            // 1) To avoid delicates between htmlResources and (root + index.html)
            // 2) Simplifies and increases performance of Take() method
            var htmlMap = new Dictionary<int, Uri>();

            var htmlResources = this.sitemap.HtmlResources;
            for (int i = 0; i < htmlResources.Count; ++i)
            {
                htmlMap.Add(i, htmlResources[i]);
            }

            // Roots might also be html pages with index.html as default
            var roots = this.sitemap.Roots;
            for (int i = 0; i < roots.Count; ++i)
            {
                var indexHtml = new Uri(roots[i] + "index.html");
                htmlMap.Add(i + htmlResources.Count, indexHtml);
            }
            
            return htmlMap;
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
            Dictionary<int, Uri> htmlMap, int[] delayInterval, BlockingCollection<string> queue)
        {
            var random = new Random();

            var samples = this.Take(htmlMap, 10);

            foreach (var uri in samples)
            {
                var delay = random.Next(delayInterval[0], delayInterval[1] + 1);
                Thread.Sleep(delay * 1000);

                var file = Path.Join(this.htmlDownloadPath, uri.LocalPath);
                Directory.CreateDirectory(Path.GetDirectoryName(file));

                if (UriDownload.Download(uri, file))
                {
                    // Producer: adding downloaded file
                    queue.Add(file);
                }
            }
        }

        public Scraper(Sitemap sitemap, string htmlDownloadPath)
        {
            if (sitemap == null)
            {
                throw new NullReferenceException("sitemap");
            }

            if (htmlDownloadPath == null)
            {
                throw new NullReferenceException("htmlDownloadPath");
            }

            if (Directory.Exists(htmlDownloadPath))
            {
                Directory.Delete(htmlDownloadPath, true);
            }
            Directory.CreateDirectory(htmlDownloadPath);

            this.sitemap = sitemap;
            this.htmlDownloadPath = htmlDownloadPath;
        }

        public void DownloadHtmls(BlockingCollection<string> queue)
        {
            // Generating collection of possible html links
            var htmlMap = this.GetHtmlUrls();

            // Slowly and randomly downloading html pages in order not to be banned by the site
            this.SlowSequentialDownload(htmlMap, new int[] { 3, 6 }, queue);

            // No more html links left for the current site
            queue.CompleteAdding();
        }
    }
}