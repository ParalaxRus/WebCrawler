using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebCrawler
{
    [TestClass]
    public class VertexTests
    {
        [TestMethod]
        public void DefaultCtorShouldSetDefaultValues()
        {
            var vertex = new Vertex(null);

            TimeSpan diff = DateTime.UtcNow.Subtract(vertex.DiscoveryTime);
            Assert.IsTrue(diff < new TimeSpan(1, 0, 0));
            Assert.IsNull(vertex.Attributes);
            Assert.IsFalse(vertex.Discovered);
            Assert.AreEqual(vertex.EdgeCount, 0);
            Assert.AreEqual(vertex.Edges.Count, 0);
        }

        [TestMethod]
        public void CtorWithAttributesShouldSetAttributes()
        {
            var attributes = new Dictionary<string, string>() 
            {
                { "robots", true.ToString()},
                { "sitemap", true.ToString()}
            };
            var vertex = new Vertex(attributes);

            Assert.IsTrue(attributes.SequenceEqual(vertex.Attributes));
            Assert.AreEqual(attributes["robots"], "True");
            Assert.AreEqual(attributes["sitemap"], "True");
        }

        [TestMethod]
        public void SerializeAndDeserializeShouldProduceEqualObject()
        {
            var attributes = new Dictionary<string, string>() 
            {
                { "robots", true.ToString() },
                { "sitemap", false.ToString() }
            };
            var vertex = new Vertex(attributes);
            vertex.Discovered = true;
            var edges = new Edge[]
            {
                new Edge(new Uri("http://www.example.com"), 10),
                new Edge(new Uri("http://www.example1.com"), 7)
            };

            foreach (var edge in edges)
            {
                vertex.Edges.Add(edge);
            }

            var serializedVertex = vertex.Serialize();

            Assert.IsFalse(string.IsNullOrWhiteSpace(serializedVertex));

            var deserializedVertex = Vertex.Deserialize(serializedVertex);

            Assert.AreEqual(vertex, deserializedVertex);
        }

        [TestMethod]
        public void SerializeToAndFromFileShouldProduceEqualObject()
        {
            var attributes = new Dictionary<string, string>() 
            {
                { "robots", true.ToString() },
                { "sitemap", false.ToString() }
            };
            var vertex = new Vertex(attributes);
            vertex.Discovered = true;
            var edges = new Edge[]
            {
                new Edge(new Uri("http://www.example.com"), 10),
                new Edge(new Uri("http://www.example1.com"), 7)
            };

            foreach (var edge in edges)
            {
                vertex.Edges.Add(edge);
            }

            string file = Path.Join(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());
            try
            {
                vertex.ToFile(file, true);

                Assert.IsTrue(File.Exists(file));

                var deserializedVertex = Vertex.FromFile(file);

                Assert.AreEqual(vertex, deserializedVertex);
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}