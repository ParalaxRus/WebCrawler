using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebCrawler
{

/// <summary>Graph vertex value.</summary>
internal class Vertex
{
    /// <summary>Gets discovery time.</summary>
    public DateTime DiscoveryTime { get; private set; }

    /// <summary>Gets or sets vertex discovery completion value.</summary>
    public bool Completed { get; set; }

    /// <summary>Gets edge count.</summary>
    public int EdgeCount { get { return this.Edges.Count; } }

    /// <summary>Gets attributes.</summary>
    public Dictionary<string, object> Attributes { get; private set; }    

    /// <summary>Gets edges.</summary>
    public HashSet<Edge> Edges { get; private set; }

    public Vertex(Dictionary<string, object> attributes)
    {
        this.DiscoveryTime = DateTime.UtcNow;
        this.Completed = false;
        this.Attributes = attributes;
        this.Edges = new HashSet<Edge>();
    }

    [JsonConstructor]
    public Vertex(Dictionary<string, object> attributes, HashSet<Edge> edges) : this(attributes)
    {
        this.Edges = edges;
    }

    public bool IsEqual(Vertex vertex)
    {
        if (vertex == null)
        {
            return false;
        }

        if ( (vertex.DiscoveryTime != this.DiscoveryTime) || 
             (vertex.EdgeCount != this.EdgeCount) || 
             (vertex.Completed != this.Completed) )
        {
            return false;
        }

        if (!vertex.Attributes.SequenceEqual(this.Attributes))
        {
            return false;
        }

        foreach (var edge in vertex.Edges)
        {
            Edge actualValue = null;
            if (!this.Edges.TryGetValue(edge, out actualValue))
            {
                return false;
            }

            if (!actualValue.IsEqual(edge))
            {
                return false;
            }
        }

        return true;
    }
    
    public string Serialize(bool pretty = false)
    {
        JsonSerializerOptions options = null;
        if (pretty)
        {
            options = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };
        }

        return JsonSerializer.Serialize(this, options);
    }

    public static Vertex Deserialize(string value)
    {
        return JsonSerializer.Deserialize<Vertex>(value);
    }
}

}
