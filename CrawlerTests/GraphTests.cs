using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebCrawler
{
    [TestClass]
    public class GraphTests
    {
        [TestMethod]
        public void CreateNonPersistentGraphShouldReturnIsPersistentFalseAndThrowApplicationException()
        {
            var graph = new Graph(false);

            Assert.IsFalse(graph.IsPersistent);

            Action action = () => 
            {
                var db = graph.CrawlDataBase;
            };

            Assert.ThrowsException<ApplicationException>(action);
        }

        [TestMethod]
        public void CreatePersistentGraphShouldReturnIsPersistentTrueAndNotNullDatabase()
        {
            var graph = new Graph(true);

            Assert.IsNotNull(graph.CrawlDataBase);
            Assert.IsTrue(graph.IsPersistent);
        }

        [TestMethod]
        public void AddingSameChildMultipleTimesShouldUpateEdgeWeight()
        {
            var graph = new Graph(true);

            var parent = new Uri("http://www.parent.com");
            var child = new Uri("http://www.child.com");
            graph.AddParent(parent);
            graph.AddChild(parent, child);

            for (int i = 0; i < 3; ++i)
            {
                graph.AddChild(new Uri("http://www.parent.com"), new Uri("http://www.child.com"));
            }
            
            int weight = graph.GetConnectionWeight(parent, child);
            Assert.AreEqual(weight, 4);
        }
    }
}