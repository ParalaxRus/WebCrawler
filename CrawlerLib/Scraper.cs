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
        /// <summary>ScrapeSettings class.</summary>
        public class Settings
        {
            /// <summary>Default number of pages to scrape from each site.</summary>
            public const int DefaultCount = 6;

            /// <summary>Minimum delay interval in seconds between pages download.</summary>
            public const int DefaultMinDelay = 3;

            /// <summary>Maximum delay interval in seconds between pages download.</summary>
            public const int DefaultMaxDelay = 6;

            /// <summary>Gets number of pages to scrape from each site.</summary>
            public int Count { get; private set; }

            /// <summary>Gets minimum delay interval in seconds between pages download.</summary>
            public int MinDelay { get; private set; }

            /// <summary>Gets maximum delay interval in seconds between pages download.</summary>
            public int MaxDelay { get; private set; }

            public Settings(int count = Scraper.Settings.DefaultCount, 
                            int minDelay = Scraper.Settings.DefaultMinDelay, 
                            int maxDelay = Scraper.Settings.DefaultMaxDelay)
            {
                this.Count = count;
                this.MinDelay = minDelay;
                this.MaxDelay = maxDelay;
            }
        }

        private Sitemap sitemap = null;

        private string htmlDownloadPath = null;

        private Random random = new Random();

        /// <summary>Report progress optional callback.</summary>
        private Action<double> reportProgress = null;

        private CancellationToken cancellationToken;

        Dictionary<int, Uri> pagesToScrape = null;

        /// <summary>Gets links to html resources only.</summary>
        private Dictionary<int, Uri> GetHtmlLinks()
        {
            // Dictionary is needed:
            // 1) To avoid duplicates between htmlResources and (root + index.html)
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

        private void SlowSequentialDownload(Dictionary<int, Uri>       htmlMap, 
                                            Settings                   settings, 
                                            BlockingCollection<string> queue)
        {
            var random = new Random();

            var samples = this.Take(htmlMap, settings.Count);

            for (int i = 0; (i < samples.Count) && !this.cancellationToken.IsCancellationRequested; ++i)
            {
                var uri = samples[i];

                // Sleeping until delay happens or user cancellation
                var delay = random.Next(settings.MinDelay, settings.MaxDelay + 1);
                bool isCancelled = this.cancellationToken.WaitHandle.WaitOne(delay * 1000);
                if (isCancelled)
                {
                    break;
                }

                var file = Path.Join(this.htmlDownloadPath, uri.LocalPath);
                Directory.CreateDirectory(Path.GetDirectoryName(file));

                if (UriDownload.Download(uri, file))
                {
                    // Producer: adding downloaded file
                    queue.Add(file);
                }

                if (this.reportProgress != null)
                {
                    this.reportProgress((i + 1) / (double)settings.Count);
                }
            }
        }

        public Scraper(Sitemap sitemap, string htmlDownloadPath, Action<double> reportProgress, CancellationToken token)
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
            this.reportProgress = reportProgress;
            this.cancellationToken = token;
        }

        public Tuple<int, int> DiscoverLinks()
        {
            // Generating collection of possible html links
            this.pagesToScrape = this.GetHtmlLinks();

            return new Tuple<int, int>(this.pagesToScrape.Count, this.sitemap.DiscoveredUrls);
        }

        /// <summary>Download and parse urls.</summary>
        public void Scrape(BlockingCollection<string> queue, Settings settings)
        {
            if (this.pagesToScrape == null)
            {
                throw new ApplicationException("Call GeneratePagesToScrape() first");
            }

            if (this.pagesToScrape.Count == 0)
            {
                // Host does not refer to other sites
                return;
            }

            // Slowly and randomly downloading html pages in order not to be banned by the site
            this.SlowSequentialDownload(this.pagesToScrape, settings, queue);

            // No more html links left for the current site
            queue.CompleteAdding();
        }
    }
}