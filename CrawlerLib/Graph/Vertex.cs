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

    /// <summary>Gets attributes.</summary>
    public Dictionary<string, string> Attributes { get; private set; }

    /// <summary>Gets edges.</summary>
    public HashSet<Edge> Edges { get; private set; }

    /// <summary>Gets edge count.</summary>
    [JsonIgnore]
    public int EdgeCount { get { return this.Edges.Count; } }

    [JsonConstructor]
    public Vertex(DateTime DiscoveryTime, bool Completed, Dictionary<string, string> Attributes, HashSet<Edge> Edges)
    {
        this.DiscoveryTime = DiscoveryTime;
        this.Completed = Completed;
        this.Attributes = Attributes;
        this.Edges = Edges;
    }

    public Vertex(Dictionary<string, string> attributes) 
        : this(DateTime.UtcNow, false, attributes, new HashSet<Edge>())
    {
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

        var res = vertex.Attributes.Except(this.Attributes).ToList();
        if (vertex.Attributes.Except(this.Attributes).Any())
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
