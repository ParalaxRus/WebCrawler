using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace WebCrawler
{

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

        public override int GetHashCode()
        {
            return this.Child.GetHashCode();
        }
    }

    private Dictionary<Uri, HashSet<Edge>> graph = new Dictionary<Uri, HashSet<Edge>>();

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
            this.dataBase.AddConnection(parent.Host, child.Host);
        }

        var edge = new Edge(child);

        var edges = this.graph[parent];

        Edge value = null;
        if (edges.TryGetValue(edge, out value))
        {
            // Increasing child's weight
            ++value.Weight;

            return false;
        }

        // Adding new child
        edges.Add(edge);

        return true;    
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