using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace WebCrawler
{

/// <summary>Connected directed graph with weights. At any point graph will have parent vertex (hosts/seeds) 
/// and outgoing edges (connections) to the children leaves. However leaves might not exist until 
/// they've been discovered by crawler.</summary>
public partial class Graph
{
    private class Edge
    {
        public Uri Child { get; private set; }

        public int Weight { get; set; }

        public Edge(Uri child)
        {
            if (child == null)
            {
                throw new ArgumentNullException();
            }

            this.Child = child;
            this.Weight = 1;
        }

        public override bool Equals(Object other)
        {
            var edge = other as Edge;
            if (edge == null)
            {
                return false;
            }
            
            return (this.Child == edge.Child);
        }

        public override int GetHashCode()
        {
            return this.Child.GetHashCode();
        }
    }

    private class GraphValue
    {
        /// <summary>Gets vertex discovery time.</summary>
        public DateTime DiscoveryTime { get; private set; }

        /// <summary>Gets edges.</summary>
        public HashSet<Edge> Edges { get; private set; }

        public GraphValue()
        {
            this.DiscoveryTime = DateTime.UtcNow;
            this.Edges = new HashSet<Edge>();
        }
    }

    /// <summary>Graph object (vertex, edges).</summary>
    private Dictionary<Uri, GraphValue> graph = new Dictionary<Uri, GraphValue>();

    /// <summary>Vertex attributes.</summary>
    private Dictionary<Uri, object[]> attributes = new Dictionary<Uri, object[]>();

    // Maybe its not such a good idea to connect DB and graph ...
    private DataBase dataBase = null;

    public bool IsPersistent {get { return (this.dataBase != null); } }

    public DataBase CrawlDataBase 
    { 
        get
        {
            if (this.dataBase == null)
            {
                throw new ApplicationException();
            }

            return this.dataBase;
        }
    }

    public Graph(bool persistent)
    {
        if (persistent)
        {
            this.dataBase = new DataBase();
        }
    }

    /// <summary>Checks whether graph contains specified parent vertex or not.</summary>
    public bool IsParent(Uri parent)
    {
        return this.graph.ContainsKey(parent);
    }

    /// <summary>Adds parent if does not exist.</summary>
    public bool AddParent(Uri parent, bool isRobots = false, bool isSitemap = false)
    {
        if (parent == null)
        {
            throw new ArgumentNullException();
        }

        if (this.IsParent(parent))
        {
            return false;
        }

        this.graph.Add(parent, new GraphValue());
        this.attributes.Add(parent, new object[] { isRobots, isSitemap });

        if (this.IsPersistent)
        {
            this.dataBase.AddHost(parent.Host, isRobots, isSitemap);
        }

        this.RaiseHostDiscoveredEvent(parent, this.graph[parent].DiscoveryTime, this.attributes[parent]);
        
        return true;
    }

    /// <summary>Adds child.</summary>
    /// <returns>True if new child has been successfully added otherwise 
    /// increases existing child weight and return false.</returns>
    public bool AddChild(Uri parent, Uri child)
    {
        if (!this.IsParent(parent))
        {
            throw new ArgumentException();
        }

        if (this.IsPersistent)
        {
            if (this.dataBase.GetHostRecord(child.Host) == null)
            {
                this.dataBase.AddHost(child.Host, false, false);
            }
            
            this.dataBase.AddConnection(parent.Host, child.Host);
        }
        
        var value = this.graph[parent];

        var newEdge = new Edge(child);

        Edge existingEdge = null;
        int weight = 1;
        if (value.Edges.TryGetValue(newEdge, out existingEdge))
        {
            // Increasing child's weight
            ++existingEdge.Weight;

            weight = existingEdge.Weight;
        }
        else
        {
            // Adding new child
            value.Edges.Add(newEdge);
        }

        this.RaiseConnectionDiscoveredEvent(parent, child, weight);

        return (value == null);
    }

    public Uri[] GetChildren(Uri parent)
    {
        if (parent == null)
        {
            throw new ArgumentNullException();
        }

        if (!this.graph.ContainsKey(parent))
        {
            throw new ApplicationException(string.Format("Parent {} does not exist", parent.Host));
        }

        var value = this.graph[parent];

        return value.Edges.Select(edge => edge.Child).ToArray();
    }

    public object[] GetParentAttributes(Uri parent)
    {
        if (parent == null)
        {
            throw new ArgumentNullException();
        }

        if (!this.graph.ContainsKey(parent))
        {
            throw new ApplicationException(string.Format("Parent {} does not exist", parent.Host));
        }

        return this.attributes[parent];
    }

    public int GetConnectionWeight(Uri parent, Uri child)
    {
        if (!this.graph.ContainsKey(parent))
        {
            throw new ArgumentException("parent");
        }

        var value = this.graph[parent];

        var lookup = new Edge(child);

        Edge existingEdge = null;
        if (!value.Edges.TryGetValue(lookup, out existingEdge))
        {
            throw new ArgumentException("child");
        }

        return existingEdge.Weight;
    }

    /// <summary>Saves graph and its database representation (if enabled) to the disk.</summary>
    public void Serialize(string graphFile, string databaseFile)
    {
        if (graphFile == null)
        {
            throw new ArgumentNullException();
        }

        if (databaseFile == null)
        {
            throw new ArgumentNullException();
        }

        // 1) Graph
        File.Delete(graphFile);
        using (var writer = new StreamWriter(graphFile))
        {
            foreach (var kvp in this.graph)
            {
                writer.WriteLine("Host={0} Time={1}", kvp.Key, kvp.Value.DiscoveryTime);
                writer.WriteLine("Links:");

                foreach (var child in kvp.Value.Edges)
                {
                    writer.WriteLine("  {0} {1}", child.Child, child.Weight);
                }
            }
        }

        // 2) Database
        if (!this.IsPersistent)
        {
            // Database disabled
            return;
        }

        File.Delete(databaseFile);
        this.dataBase.Serialize(databaseFile);
    }
}

}