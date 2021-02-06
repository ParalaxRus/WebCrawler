using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebCrawler
{
    [TestClass]
    public class GZipTests
    {
        private static byte[] GetFileHash(string file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        private static void CreateCompressedFile(string file, int len = 100)
        {
            using (var writer = File.CreateText(file))
            {
                for (int i = 0; i < len; ++i)
                {
                    writer.Write(i);
                }
            }

            string tmp = file + "." + Guid.NewGuid().ToString();
            GZip.Compress(file, tmp);
            File.Move(tmp, file, true);
        }

        [TestMethod]
        public void IsGZipShouldReturnTrueForZipArchive()
        {
            var file = Path.Join(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".xml.gz");

            try
            {
                GZipTests.CreateCompressedFile(file);

                Assert.IsTrue(GZip.IsGZip(file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void DecompressAndCompressFileShouldReturnIdenticalFile()
        {
            var compressedFile = Path.Join(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".xml.gz");
            var decompressedFile = Path.Join(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".xml");
            var compressedFile1 = Path.Join(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".xml.gz");

            try
            {
                GZipTests.CreateCompressedFile(compressedFile);

                GZip.Decompress(compressedFile, decompressedFile);
                GZip.Compress(decompressedFile, compressedFile1);

                var hash1 = GZipTests.GetFileHash(compressedFile);
                var hash2 = GZipTests.GetFileHash(compressedFile1);
                Assert.IsTrue(hash1.SequenceEqual(hash2));
            }
            finally
            {
                File.Delete(compressedFile);
                File.Delete(decompressedFile);
                File.Delete(compressedFile1);
            }
        }
    }
}