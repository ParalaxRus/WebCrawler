using System;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace WebCrawler
{
    internal class UrlHelper
    {
        public static bool Download(Uri url, string file)
        {
            try
            {
                var request = WebRequest.Create(url);
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return false;
                    }

                    using (var stream = response.GetResponseStream())
                    {
                        using (var dataStream = response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(dataStream))
                            {
                                using (var writer = File.Create(file))
                                {
                                    reader.BaseStream.CopyTo(writer);
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.Message);
                
                return false;
            }
        }

        public static bool IsResource(Uri uri)
        {
            if (uri.LocalPath.Length == 0)
            {
                return false;
            }

            const char trailingSlash = '/'; // Resource does not end with /
            return (uri.LocalPath[uri.LocalPath.Length - 1] != trailingSlash);
        }
    }
}