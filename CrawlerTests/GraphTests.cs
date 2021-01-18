using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebCrawler
{
    [TestClass]
    public class GraphTests
    {
        [TestMethod]
        public void AddParentSecondTimeShouldNotModifyItAndReturnFalse()
        {
            var graph = new Graph();

            var parent1 = new Uri("http://www.parent.com");
            var attributes1 = new Dictionary<string, object>() 
            {
                { "robots", true},
                { "sitemap", true}
            };
            Assert.IsTrue(graph.AddVertex(parent1, attributes1));

            var parent2 = new Uri("http://www.parent.com");
            var attributes2 = new Dictionary<string, object>() 
            {
                { "robots", false },
                { "sitemap", false }
            };
            Assert.IsFalse(graph.AddVertex(parent2, attributes2));

            var actualAttributes = graph.GetAttributes(parent2);
            Assert.IsTrue(actualAttributes.SequenceEqual(attributes1));
        }

        [TestMethod]
        public void AddSameChildMultipleTimesShouldUpateEdgeWeight()
        {
            var graph = new Graph();

            var parent = new Uri("http://www.parent.com");
            var child = new Uri("http://www.child.com");
            graph.AddVertex(parent);
            graph.AddEdge(parent, child);

            for (int i = 0; i < 3; ++i)
            {
                graph.AddEdge(new Uri("http://www.parent.com"), new Uri("http://www.child.com"));
            }
            
            int weight = graph.GetEdgeWeight(parent, child);
            Assert.AreEqual(weight, 4);
        }

        [TestMethod]
        public void AddChildShouldNotModifyParents()
        {
            var graph = new Graph();

            var parent = new Uri("http://www.parent.com");
            graph.AddVertex(parent);

            var child = new Uri("http://www.child.com");
            graph.AddEdge(parent, child);

            Assert.IsFalse(graph.IsVertex(child));
        }

        [TestMethod]
        public void AddParentAsAChildShouldSuccessfullyAddIt()
        {
            var graph = new Graph();

            var parent = new Uri("http://www.parent.com");
            graph.AddVertex(parent);

            var child = new Uri("http://www.child.com");
            graph.AddEdge(parent, child);

            Assert.IsFalse(graph.IsVertex(child));
        }
    }
}