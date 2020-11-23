using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WebCrawler
{
    internal class WebScraper
    {
        string rootPath = null;

        Sitemap sitemap = null;

        Random random = new Random();

        public bool SaveHtmlFiles { get; set; }

        private Dictionary<int, Uri> GetHtmlUrls()
        {
            // Dictionary is neded because:
            // 1) To avoid dplicates between htmlResources and (root + index.html)
            // 2) Performance in Take() method
            var htmls = new Dictionary<int, Uri>();

            var htmlResources = this.sitemap.HtmlResources;
            for (int i = 0; i < htmlResources.Count; ++i)
            {
                htmls.Add(i, htmlResources[i]);
            }

            // Root might contain index.html
            var roots = this.sitemap.Roots;
            for (int i = 0; i < roots.Count; ++i)
            {
                var indexHtml = new Uri(roots[i] + "index.html");
                htmls.Add(i + htmlResources.Count, indexHtml);
            }
            
            return htmls;
        }

        /// <summary>Takes pseudo-random (bicketized) 'count of elements' from urls.</summary>
        private List<Uri> Take(Dictionary<int, Uri> urls, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException("count");
            }

            // Pseudo-random = random inside buckets
            var buckets = new int[urls.Count / count];
            for (int i = 0; i < buckets.Length; ++i)
            {
                int start = i * count;
                buckets[i] = this.random.Next(start, start + count);
            }
            
            var sample = new List<Uri>();
            foreach (var index in buckets)
            {
                sample.Add(urls[index]);
                urls.Remove(index);
            }

            return sample;
        }

        public WebScraper(string rootPath, Sitemap sitemap)
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

        public void DownloadHtmls()
        {
            var htmls = this.GetHtmlUrls();

            var sample = this.Take(htmls, 10);

            Parallel.ForEach(sample, (Uri uri) => 
            {
                var file = Path.Join(this.rootPath, uri.LocalPath);
                Directory.CreateDirectory(Path.GetDirectoryName(file));

                UriDonwload.Download(uri, file);

                if (!this.SaveHtmlFiles && File.Exists(file))
                {
                    File.Delete(file);
                }
            });
        }
    }
}