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
    /// <summary>Graph table (vertex key - host, vertex value).</summary>
    private Dictionary<Uri, Vertex> graph = new Dictionary<Uri, Vertex>();

    /// <summary>Gets vertex value by key.</summary>
    /// <remarks>Throws exception if vertex not found.</remarks>
    private Vertex GetVertex(Uri key)
    {
        if (key == null)
        {
            throw new ArgumentNullException();
        }

        if (!this.graph.ContainsKey(key))
        {
            throw new ApplicationException(string.Format("Vertex {} does not exist", key.Host));
        }

        return this.graph[key];
    }

    /// <summary>Checks whether vertex with the specified key exists or not.</summary>
    public bool IsVertex(Uri key)
    {
        return ( (key != null) && this.graph.ContainsKey(key) );
    }

    /// <summary>Adds parent if it does not exist.</summary>
    public bool AddVertex(Uri key, Dictionary<string, object> attributes = null)
    {
        if (this.IsVertex(key))
        {
            return false;
        }

        this.graph.Add(key, new Vertex(attributes));

        this.RaiseHostDiscoveredEvent(key, this.graph[key].DiscoveryTime, this.graph[key].Attributes);
        
        return true;
    }

    /// <summary>Adds an edge between source and target nodes.</summary>
    /// <returns>True if new link has been successfully added otherwise 
    /// increases existing link weight and returns false.</returns>
    /// <remarks>Source node must exist but target does not have to.</remarks>
    public bool AddEdge(Uri source, Uri target)
    {
        if (!this.IsVertex(source))
        {
            throw new ArgumentException(string.Format("Source vertex {} does not exist", source.Host));
        }

        var value = this.graph[source];

        var newEdge = new Edge(target);

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

        this.RaiseConnectionDiscoveredEvent(source, target, weight);

        return (value == null);
    }

    /// <summary>Marks vertex discovery field as completed.</summary>
    public void MarkCompleted(Uri key)
    {
        var value = this.GetVertex(key);
        value.Completed = true;
    }

    /// <summary>Gets vertex edges.</summary>
    public Uri[] GetEdges(Uri key)
    {
        var value = this.GetVertex(key);

        return value.Edges.Select(edge => edge.Child).ToArray();
    }

    /// <summary>Gets vertex attributes.</summary>
    public Dictionary<string, object> GetAttributes(Uri key)
    {
        var value = this.GetVertex(key);

        return value.Attributes;
    }

    /// <summary>Gets vertex discovery completion flag.</summary>
    public bool GetCompleted(Uri key)
    {
        var value = this.GetVertex(key);

        return value.Completed;
    }

    /// <summary>Gets edge weight.</summary>
    /// <remarks>Source node must exist but target does not have to.</remarks>
    public int GetEdgeWeight(Uri source, Uri target)
    {
        var value = this.GetVertex(source);

        var lookup = new Edge(target);

        Edge existingEdge = null;
        if (!value.Edges.TryGetValue(lookup, out existingEdge))
        {
            throw new ArgumentException("edge");
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

        File.Delete(graphFile);
        using (var writer = new StreamWriter(graphFile))
        {
            foreach (var kvp in this.graph)
            {
                writer.WriteLine("Host={0} ", kvp.Key, kvp.Value.Serialize());
                writer.WriteLine("Links:");

                foreach (var child in kvp.Value.Edges)
                {
                    writer.WriteLine("  {0} {1}", child.Child, child.Weight);
                }
            }
        }
    }
}

}