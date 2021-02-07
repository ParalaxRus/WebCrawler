using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebCrawler
{
    class Program
    {
        private static void WaitUntilCompletedOrKeyPressed(CancellationTokenSource token, Task task)
        {
            Console.WriteLine("Press 'q' to stop");

            while (true)
            {
                Thread.Sleep(500);

                if (task.IsCompleted)
                {
                    break;
                }

                if (Console.KeyAvailable) 
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.Q)
                    {
                        token.Cancel();
                        break;
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            var configuration = new Configuration()
            {
                EnableLog = true,
                OutputPath = Path.Join(Directory.GetCurrentDirectory(), "output"),
                LogFilePath = Path.Join(Directory.GetCurrentDirectory(), "output/crawler.log"),
                GraphFilePath = Path.Join(Directory.GetCurrentDirectory(), "output/graph.json"),
                SaveRobotsFile = true,
                SaveSitemapFiles = false,
                SaveUrls = true,
                DeleteHtmlAfterScrape = true,
                SerializeSite = true,
                SerializeGraph = true,
                HostUrlsLimit = 1000,
                SitemapIndexLimit = 1000
            };

            var token = new CancellationTokenSource();

            var seedUrls = new Uri[]
            {
                new Uri("https://www.google.com")
            };
            var crawler = new Crawler(configuration, seedUrls, token.Token);

            var task = Task.Run(() =>
            {
                crawler.Crawl();
            });

            Program.WaitUntilCompletedOrKeyPressed(token, task);

            task.Wait();
            token.Dispose();

            Console.WriteLine("Completed");
        }
    }
}
