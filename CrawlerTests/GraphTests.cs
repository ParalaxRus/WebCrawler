using System;
using System.Linq;
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
        public void AddParentSecondTimeShouldNotModifyItAndReturnFalse()
        {
            var graph = new Graph(true);

            var parent1 = new Uri("http://www.parent.com");
            Assert.IsTrue(graph.AddParent(parent1, true, true));

            var parent2 = new Uri("http://www.parent.com");
            Assert.IsFalse(graph.AddParent(parent2, false, false));

            var attributes = graph.GetParentAttributes(parent2);
            Assert.IsTrue(attributes.SequenceEqual(new object[] {true, true}));
        }

        [TestMethod]
        public void AddSameChildMultipleTimesShouldUpateEdgeWeight()
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

        [TestMethod]
        public void AddChildShouldNotModifyParents()
        {
            var graph = new Graph(true);

            var parent = new Uri("http://www.parent.com");
            graph.AddParent(parent);

            var child = new Uri("http://www.child.com");
            graph.AddChild(parent, child);

            Assert.IsFalse(graph.IsParent(child));
        }

        [TestMethod]
        public void AddParentAsAChildShouldSuccessfullyAddIt()
        {
            var graph = new Graph(true);

            var parent = new Uri("http://www.parent.com");
            graph.AddParent(parent);

            var child = new Uri("http://www.child.com");
            graph.AddChild(parent, child);

            Assert.IsFalse(graph.IsParent(child));
        }
    }
}