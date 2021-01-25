using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
            var attributes1 = new Dictionary<string, string>() 
            {
                { "robots", true.ToString()},
                { "sitemap", true.ToString()}
            };
            Assert.IsTrue(graph.AddVertex(parent1, attributes1));

            var parent2 = new Uri("http://www.parent.com");
            var attributes2 = new Dictionary<string, string>() 
            {
                { "robots", false.ToString() },
                { "sitemap", false.ToString() }
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

        [TestMethod]
        public void SerializeAndDeserializeShouldProduceEqualObject()
        {
            var graph = new Graph();

            // vertex1
            var p1 = new Uri("http://www.source.com");
            var attributes = new Dictionary<string, string>() 
            {
                { "robots", true.ToString() },
                { "sitemap", true.ToString() }
            };
            graph.AddVertex(p1, attributes);

            var edges = new Uri[]
            {
                new Uri("http://www.target1.com"),
                new Uri("http://www.target2.com"),
            };
            for (int i = 0; i < 2; ++i)
            {
                graph.AddEdge(p1, edges[0]);
            }
            for (int i = 0; i < 3; ++i)
            {
                graph.AddEdge(p1, edges[1]);
            }
            graph.MarkCompleted(p1);

            // vertex2
            var p2 = new Uri("http://www.source1.com");
            var attributes1 = new Dictionary<string, string>() 
            {
                { "robots", false.ToString() },
                { "sitemap", false.ToString() }
            };
            graph.AddVertex(p2, attributes1);

            var edges1 = new Uri[]
            {
                new Uri("http://www.target2.com"),
                new Uri("http://www.target3.com"),
                new Uri("http://www.target4.com"),
            };
            for (int i = 0; i < 3; ++i)
            {
                graph.AddEdge(p2, edges1[0]);
            }
            for (int i = 0; i < 2; ++i)
            {
                graph.AddEdge(p2, edges1[1]);
            }

            var file = Path.Join(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());
            try
            {
                graph.Serialize(file);

                var deserializedGraph = Graph.Deserialize(file);
                
                Assert.AreEqual(deserializedGraph, graph);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void PersistAndRecostructFromFileShouldProduceEqualObject()
        {
            var graph = new Graph();

            // vertex1
            var p1 = new Uri("http://www.source.com");
            var attributes = new Dictionary<string, string>() 
            {
                { "robots", true.ToString() },
                { "sitemap", true.ToString() }
            };
            graph.AddVertex(p1, attributes);

            var edges = new Uri[]
            {
                new Uri("http://www.target1.com"),
                new Uri("http://www.target2.com"),
            };
            for (int i = 0; i < 2; ++i)
            {
                graph.AddEdge(p1, edges[0]);
            }
            for (int i = 0; i < 3; ++i)
            {
                graph.AddEdge(p1, edges[1]);
            }
            graph.MarkCompleted(p1);

            // vertex2
            var p2 = new Uri("http://www.source1.com");
            var attributes1 = new Dictionary<string, string>() 
            {
                { "robots", false.ToString() },
                { "sitemap", false.ToString() }
            };
            graph.AddVertex(p2, attributes1);

            var edges1 = new Uri[]
            {
                new Uri("http://www.target2.com"),
                new Uri("http://www.target3.com"),
                new Uri("http://www.target4.com"),
            };
            for (int i = 0; i < 3; ++i)
            {
                graph.AddEdge(p2, edges1[0]);
            }
            for (int i = 0; i < 2; ++i)
            {
                graph.AddEdge(p2, edges1[1]);
            }

            var output = Path.Join(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());
            try
            {
                 graph.Persist(output);

                var reconstructedGraph = Graph.Reconstruct(output);
                
                Assert.AreEqual(reconstructedGraph, graph);
            }
            finally
            {
                Directory.Delete(output, true);
            }
        }
    }
}