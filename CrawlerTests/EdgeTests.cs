using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebCrawler
{
    [TestClass]
    public class EdgeTests
    {
        [TestMethod]
        public void DefaultCtorShouldSetDefaultValues()
        {
            var uri = new Uri("http://www.example.com");
            var edge = new Edge(uri);

            Assert.AreEqual(edge.Child, uri);
            Assert.AreEqual(edge.Weight, 1);
        }

        [TestMethod]
        public void SerializeAndDeserializeShouldProduceEqualObjects()
        {
            var uri = new Uri("http://www.example.com");
            var edge = new Edge(uri);
            edge.Weight = 10;

            var serializedEdge = edge.Serialize();
            Assert.IsFalse(string.IsNullOrWhiteSpace(serializedEdge));

            Edge deserializedEdge = Edge.Deserialize(serializedEdge);

            Assert.AreEqual(edge, deserializedEdge);
        }
    }
}