using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebCrawler
{
    class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new Configuration()
            {
                EnableLog = true,
                OutputPath = Path.Join(Directory.GetCurrentDirectory(), "output"),
                LogFilePath = Path.Join(Directory.GetCurrentDirectory(), "output/crawler.log"),
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
            var crawler = new Crawler(configuration, seedUrls, token.Token);

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
