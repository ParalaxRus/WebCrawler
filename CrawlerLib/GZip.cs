using System.IO;
using System.IO.Compression;

namespace WebCrawler
{
    internal class GZip
    {
        private const string GzExtension = ".gz";

        public static void Compress(string source, string target)
        {
            var info = new FileInfo(source);

            using (var sourceStream = info.OpenRead())
            {
                using (var compressedStream = File.Create(target))
                {
                    using (var compressionStream = new GZipStream(compressedStream, CompressionMode.Compress))
                    {
                        sourceStream.CopyTo(compressionStream);
                    }
                }
            }
        }

        public static void Decompress(string source, string target)
        {
            var info = new FileInfo(source);

            using (FileStream sourceStream = info.OpenRead())
            {
                using (var stream = File.Create(target))
                {
                    using (var decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(stream);
                    }
                }
            }
        }

        public static bool IsGZip(string file)
        {
            var info = new FileInfo(file);

            return ((info.Extension == GZip.GzExtension) && info.Attributes.HasFlag(FileAttributes.Archive));
        }
    }
}