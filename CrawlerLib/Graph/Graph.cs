using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace WebCrawler
{

/// <summary>Connected directed graph with weights. At any point graph will have parent vertex (hosts/seeds) 
/// and outgoing edges (connections) to the children leaves. However leaves might not exist until 
/// they've been discovered by crawler.</summary>
public partial class Graph
{
    /// <summary>Graph table (vertex key - host, vertex value).</summary>
    private Dictionary<string, Vertex> graph = new Dictionary<string, Vertex>();

    /// <summary>Gets vertex value by key.</summary>
    /// <remarks>Throws exception if vertex not found.</remarks>
    private Vertex GetVertex(Uri key)
    {
        if (key == null)
        {
            throw new ArgumentNullException();
        }

        if (!this.graph.ContainsKey(key.Host))
        {
            throw new ApplicationException(string.Format("Vertex {} does not exist", key.Host));
        }

        return this.graph[key.Host];
    }

    /// <summary>Checks whether vertex with the specified key exists or not.</summary>
    public bool IsVertex(Uri key)
    {
        return ( (key != null) && this.graph.ContainsKey(key.Host) );
    }

    /// <summary>Adds parent if it does not exist.</summary>
    public bool AddVertex(Uri key, Dictionary<string, string> attributes = null)
    {
        if (this.IsVertex(key))
        {
            return false;
        }

        this.graph.Add(key.Host, new Vertex(attributes));

        this.RaiseHostDiscoveredEvent(key, this.graph[key.Host].DiscoveryTime, this.graph[key.Host].Attributes);
        
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

        var value = this.graph[source.Host];

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
    public Dictionary<string, string> GetAttributes(Uri key)
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

    /// <summary>Serializes graph to the specified file.</summary>
    public void Serialize(string file)
    {
        if (file == null)
        {
            throw new ArgumentNullException();
        }

        File.Delete(file);

        using (var stream = File.OpenWrite(file))
        {
            var options = new JsonWriterOptions { Indented = true };
            using (var writer = new Utf8JsonWriter(stream,  options))
            {
                JsonSerializer.Serialize<Dictionary<string, Vertex>>(writer, this.graph);
            }
        }
    }

    /// <summary>Deserializes graph from the specified file.</summary>
    public static Graph Deserialize(string file)
    {
        if (!File.Exists(file))
        {
            throw new ArgumentException(string.Format("File {0} does not exist", file));
        }

        var graph = new Graph();

        using (var stream = File.OpenRead(file))
        {
            var task = JsonSerializer.DeserializeAsync<Dictionary<string, Vertex>>(stream).AsTask();
            task.Wait();

            graph.graph = task.Result;
        }

        return graph;
    }

    #region Equality overrides

    public override bool Equals(object obj)
    {
        var other = obj as Graph;
        if (other == null)
        {
            return false;
        }

        return this.graph.SequenceEqual(other.graph);
    }

    public override int GetHashCode()
    {
       return this.graph.GetHashCode();
    }

    #endregion
}

}