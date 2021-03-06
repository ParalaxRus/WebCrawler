using System;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace WebCrawler
{
    internal class UriDownload
    {
        private async Task<HttpWebResponse> IssueGetAsync(Uri uri)
        {
            var task = await Task.Run(() => 
            {
                HttpWebResponse response = null;
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(uri);

                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException exception)
                {
                    Trace.TraceError(string.Format("Failed to get {0}. Exception: {1}", 
                                                   uri.LocalPath, 
                                                   exception.Message));

                    response = (HttpWebResponse)exception.Response;
                }

                return response;
            });

            return task;
        }

        private async Task<bool> WriteToFile(HttpWebResponse response, string file)
        {   
            return await Task.Run(() => 
            {
                using (var reader = response.GetResponseStream())
                {
                    using (var writer = File.Create(file))
                    {
                        var buffer = new byte[64 * 1024];
                        while (true)
                        {
                            int read = reader.Read(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                break;
                            }

                            writer.Write(buffer, 0, read);
                        }
                    }
                }

                return true;
            });
        }

        public async Task<bool> DownloadAsync(Uri uri, string file)
        {
            try
            {
                var response = await this.IssueGetAsync(uri);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException("Response failed. " + response.ToString());
                }

                return await this.WriteToFile(response, file);
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.Message);

                return false;
            }
        }

        public static bool Download(Uri uri, string file)
        {
            var downloader = new UriDownload();

            var task = downloader.DownloadAsync(uri, file);
            task.Wait();

            if (!task.Result)
            {
                Trace.TraceError(string.Format("Failed to download {0} to {1}", 
                                               uri.LocalPath, 
                                               file));
            }

            return task.Result;
        }
    }
}