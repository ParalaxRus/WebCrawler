using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebCrawler;

namespace WinUiApp
{
    static class Program
    {
        private static Form1 form = null;

        private static Crawler crawler = null;

        private static void RegisterEvents()
        {
            Program.form.edgesView.Columns.Add("Parent", "Parent");
            Program.form.edgesView.Columns.Add("Child", "Child");
            Program.form.edgesView.Columns.Add("Weight", "Weight");
            Program.crawler.CrawlerGraph.ConnectionDiscoveredEvent += Program.Crawler_ConnectionDiscoveredEvent;

            Program.form.vertexView.Columns.Add("Parent", "Parent");
            Program.form.vertexView.Columns.Add("Time", "Time");
            Program.form.vertexView.Columns.Add("IsRobots", "IsRobots");
            Program.form.vertexView.Columns.Add("IsSitemap", "IsSitemap");
            Program.crawler.CrawlerGraph.HostDiscoveredEvent += Program.Crawler_HostDiscoveredEvent;

            Program.crawler.StatusEvent += Program.Crawler_StatusEvent;
            Program.crawler.ProgressEvent += Program.Crawler_ProgressEvent;
        }

        private static void Crawler_StatusEvent(object sender, StatusArgs e)
        {
            if (Program.form.IsClosed)
            {
                return;
            }

            Program.form.statusView.Invoke((MethodInvoker)delegate
            {
                Program.form.statusView.Text = e.Status;
            });
        }

        private static void Crawler_ProgressEvent(object sender, ProgressArgs e)
        {
            if (Program.form.IsClosed)
            {
                return;
            }

            Program.form.progressView.Invoke((MethodInvoker)delegate
            {
                Program.form.progressView.Text = e.Progress.ToString("P2", CultureInfo.InvariantCulture);
            });
        }

        private static void Crawler_ConnectionDiscoveredEvent(object sender, ConnectionDiscoveredArgs e)
        {
            if (Program.form.IsClosed)
            {
                return;
            }

            Program.form.edgesView.Invoke((MethodInvoker)delegate
            {
                int i = 0;
                for (; i < Program.form.edgesView.Rows.Count; ++i)
                {
                    if (Program.form.edgesView.Rows[i].Cells[1].Value.ToString() == e.Child.Host)
                    {
                        break;
                    }
                }

                if (i == Program.form.edgesView.Rows.Count)
                {
                    // New row
                    Program.form.edgesView.Rows.Add(new object[] { e.Parent.Host, e.Child.Host, e.Weight });
                }
                else
                {
                    Program.form.edgesView.Rows[i].Cells[2].Value = e.Weight;
                }
            });
        }

        private static void Crawler_HostDiscoveredEvent(object sender, HostDiscoveredArgs e)
        {
            if (Program.form.IsClosed)
            {
                return;
            }

            Program.form.vertexView.Invoke((MethodInvoker)delegate
            {
                int i = 0;
                for (; i < Program.form.vertexView.Rows.Count; ++i)
                {
                    if (Program.form.vertexView.Rows[i].Cells[0].Value.ToString() == e.Host.Host)
                    {
                        break;
                    }
                }

                if (i == Program.form.vertexView.Rows.Count)
                {
                    // New row
                    Program.form.vertexView.Rows.Add(new object[] { e.Host.Host, e.DiscoveryTime, e.Attributes[0], e.Attributes[1] });
                }
                else
                {
                    Program.form.vertexView.Rows[i].Cells[1].Value = e.DiscoveryTime;
                    Program.form.vertexView.Rows[i].Cells[2].Value = e.Attributes[0];
                    Program.form.vertexView.Rows[i].Cells[3].Value = e.Attributes[1];
                }
            });
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Program.form = new Form1();
            
            string outputPath = Path.Join(Directory.GetCurrentDirectory(), "output");
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
            Directory.CreateDirectory(outputPath);

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
            Program.crawler = new Crawler(configuration, seedUrls, outputPath, token.Token);

            Program.RegisterEvents();

            var task = Task.Run(() =>
            {
                Program.crawler.Crawl();
            });

            Application.Run(Program.form);

            token.Cancel();
            task.Wait();

            token.Dispose();
        }
    }
}
