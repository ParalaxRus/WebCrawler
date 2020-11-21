using System;
using System.IO;
using System.Diagnostics;

namespace WebCrawler
{
    class Program
    {
        private static readonly Uri rootUrl = new Uri("https://www.google.com/");
        //private static readonly Uri rootUrl = new Uri("https://www.mallenom.ru/");

        /*private static HashSet<string> ParseUrls(string file)
        {
            var set = new HashSet<string>();

            using (var reader = new StreamReader(file))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    try
                    {
                        var hrefLink = XElement.Parse(line)
                                           .Descendants("a")
                                           .Select(x => x.Attribute("href").Value)
                                           .FirstOrDefault();

                        Uri uriResult;
                        bool isValidUrl = Uri.TryCreate(hrefLink, UriKind.Absolute, out uriResult) && 
                                                   ( (uriResult.Scheme == Uri.UriSchemeHttp) || 
                                                     (uriResult.Scheme == Uri.UriSchemeHttps) );

                        if (isValidUrl)
                        {
                            set.Add(hrefLink);
                        }
                    }
                    catch(Exception)
                    {

                    }
                }
            }

            return set;
        }

        private static void SaveUrls(HashSet<string> set, string file)
        {
            using (var stream = File.CreateText(file))
            {
                foreach (var record in set)
                {
                    stream.WriteLine(record);
                }
            }
        }*/

        static void Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener("crawler.log", "CrawlerListener"));
            Trace.AutoFlush = true;

            var rootPath = Path.Join(Directory.GetCurrentDirectory(), Program.rootUrl.Host);
            Directory.CreateDirectory(rootPath);

            var policy = new CrawlPolicy();
            var task = policy.DownloadPolicyAsync(Program.rootUrl, rootPath);
            task.Wait();
            if (!task.Result)
            {
                Trace.TraceError("Failed to obtain policy");
            }

            var agentPolicy = policy.GetAgentPolicy();
            
            var sitemap = new Sitemap(Program.rootUrl, policy.SitemapUri, rootPath, false);
            sitemap.Build(agentPolicy.Disallow);

            /*var robotsFile = Path.Join(projectFolder, "robots");
            Program.DownloadToFile(Program.url + "robots.txt", robotsFile);

            var sitemapFile = Path.Join(projectFolder, "sitemap");
            Program.DownloadToFile(Program.url + "sitemap.php", sitemapFile);

            var htmlFile = Path.Join(projectFolder, "mallenom.html");
            Program.DownloadToFile(Program.url, htmlFile);

            var urls = Program.ParseUrls(htmlFile);

            var urlsFile = Path.Join(Directory.GetCurrentDirectory(), "urls.txt");
            Program.SaveUrls(urls, urlsFile);*/
        }
    }
}
