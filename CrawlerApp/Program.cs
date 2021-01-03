using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WebCrawler
{
    class Program
    {
        /// <summary>Full path to crawl product files.</summary>
        private static readonly string outputPath = Path.Join(Directory.GetCurrentDirectory(), "output");

        /// <summary>Full path to the log file.</summary>
        private static readonly string logPath = Path.Join(Program.outputPath, "crawler.log");

        public static void Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Program.logPath, "CrawlerListener"));
            Trace.AutoFlush = true;

            // Recreating for now
            if (Directory.Exists(Program.outputPath))
            {
                Directory.Delete(Program.outputPath, true);
            }
            Directory.CreateDirectory(Program.outputPath);

            var configuration = new CrawlerConfiguration()
            {
                SaveRobotsFile = true,
                SaveSitemapFiles = false,
                SaveUrls = true,
                DeleteHtmlAfterScrape = true,
                SerializeSite = true,
                SerializeGraph = true
            };

            var token = new CancellationTokenSource();

            var seedUrls = new Uri[]
            {
                new Uri("https://www.google.com/")
            };
            var crawler = new Crawler(configuration, seedUrls, Program.outputPath, token.Token);

            var task = Task.Run(() =>
            {
                crawler.Crawl();
            });

            task.Wait();
            token.Dispose();

            Console.WriteLine("Completed");
        }
    }
}
