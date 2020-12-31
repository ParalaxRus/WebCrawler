using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace WebCrawler
{

public class CrawlGraphEdgeArgs
{
    public string Parent { get; }

    public string Child { get; }

    public int Weight { get; }

    public CrawlGraphEdgeArgs(string parent, string child, int weight) 
    { 
        this.Parent = parent;
        this.Child = child;
        this.Weight = weight;
    }
}

/// <summary>Connected directed graph with weights.</summary>
public class Graph
{
    private class Edge
    {
        public Uri Child { get;set; }

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

    private Dictionary<Uri, HashSet<Edge>> graph = new Dictionary<Uri, HashSet<Edge>>();

    private DataBase dataBase = null;

    #region Graph events

    protected virtual void RaiseGraphEvent(string parent, string child, int weight)
    {
        if (this.GraphEvent != null)
        {
            this.GraphEvent.Invoke(this, new CrawlGraphEdgeArgs(parent, child, weight));
        }
    }

    // Declare the delegate (if using non-generic pattern).
    public delegate void GraphEventHandler(object sender, CrawlGraphEdgeArgs e);

    // Declare the event.
    public event GraphEventHandler GraphEvent;

    #endregion

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

    /// <summary>Adds parent if does not exist.</summary>
    public bool AddParent(Uri parent)
    {
        if (parent == null)
        {
            throw new ArgumentNullException();
        }

        if (this.graph.ContainsKey(parent))
        {
            // Already exists
            return false;
        }

        this.graph.Add(parent, new HashSet<Edge>());

        if (this.dataBase != null)
        {
            this.dataBase.AddHost(parent.Host, false, false);
        }
        
        return true;
    }

    /// <summary>Adds child.</summary>
    /// <returns>True if new child has been successfully added otherwise 
    /// increases existing child weight and return false.</returns>
    public bool AddChild(Uri parent, Uri child)
    {
        if (!this.graph.ContainsKey(parent))
        {
            throw new ArithmeticException();
        }

        if (this.dataBase != null)
        {
            this.dataBase.AddHost(child.Host, false, false);
            this.dataBase.AddConnection(parent.Host, child.Host);
        }

        var edge = new Edge(child);

        var edges = this.graph[parent];

        Edge value = null;
        int weight = 1;
        if (edges.TryGetValue(edge, out value))
        {
            // Increasing child's weight
            ++value.Weight;
            
            weight = value.Weight;
        }
        else
        {
            // Adding new child
            edges.Add(edge);
        }

        this.RaiseGraphEvent(parent.Host, child.Host, weight);

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

        var edges = this.graph[parent];

        return edges.Select(edge => edge.Child).ToArray();
    }

    public int GetConnectionWeight(Uri parent, Uri child)
    {
        if (!this.graph.ContainsKey(parent))
        {
            throw new ArgumentException("parent");
        }

        var edges = this.graph[parent];

        Edge res = null;
        var edge = new Edge(child);

        if (!edges.TryGetValue(edge, out res))
        {
            throw new ArgumentException("child");
        }

        return res.Weight;
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

        File.Delete(graphFile);
        using (var writer = new StreamWriter(graphFile))
        {
            foreach (var kvp in this.graph)
            {
                foreach (var child in kvp.Value)
                {
                    writer.WriteLine("{0}: {1}, {2}", kvp.Key, child.Child, child.Weight);
                }
            }
        }

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