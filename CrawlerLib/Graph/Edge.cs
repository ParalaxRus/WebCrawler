using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebCrawler
{

/// <summary>Edge class.</summary>
internal class Edge
{
    /// <summary>Gets child.</summary>
    public Uri Child { get; private set; }

    /// <summary>Gets or sets edge weight.</summary>
    public int Weight { get; set; }

    public Edge(Uri Child, int Weight = 1)
    {
        if (Child == null)
        {
            throw new ArgumentNullException();
        }

        this.Child = Child;
        this.Weight = Weight;
    }

    /// <summary>Checks equality including weight.</summary>
    public bool IsEqual(Edge edge)
    {
        if (edge == null)
        {
            return false;
        }

        return ((edge.Child == this.Child) && (edge.Weight == this.Weight));
    }

    /// <summary>Checks equality without checking weight (to be used with hashtables).</summary>
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

    public static Edge Deserialize(string value)
    {
        return JsonSerializer.Deserialize<Edge>(value);
    }
}

}
