using System;

namespace WebCrawler
{

public class ConnectionDiscoveredArgs
{
    public Uri Parent { get; }

    public Uri Child { get; }

    public int Weight { get; }

    public ConnectionDiscoveredArgs(Uri parent, Uri child, int weight) 
    { 
        if (parent == null)
        {
            throw new ArgumentNullException("parent");
        }

        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        if (weight <= 0)
        {
            throw new ArgumentException("weight");
        }

        this.Parent = parent;
        this.Child = child;
        this.Weight = weight;
    }
}

public class HostDiscoveredArgs
{
    public Uri Host { get; }

    public DateTime DiscoveryTime { get; }

    public object[] Attributes { get; }

    public HostDiscoveredArgs(Uri host, DateTime time, object[] attributes) 
    { 
        if (host == null)
        {
            throw new ArgumentNullException("host");
        }

        if (time == null)
        {
            throw new ArgumentNullException("time");
        }

        if (attributes == null)
        {
            throw new ArgumentNullException("attributes");
        }

        this.Host = host;
        this.DiscoveryTime = time;
        this.Attributes = attributes;
    }
}

//devide into status and progress events ...
public class StatusArgs
{
    public string Status { get; }

    public double Progress { get; }

    public StatusArgs(string status, double progress) 
    { 
        if (status == null)
        {
            throw new ArgumentNullException("status");
        }

        if ((progress < 0.0) || (progress > 1.0))
        {
            throw new ArgumentNullException("progress");
        }

        this.Status = status;
        this.Progress = progress;
    }
}

/// <summary>Graph events logic.</summary>
public partial class Graph
{
    protected virtual void RaiseConnectionDiscoveredEvent(Uri parent, Uri child, int weight)
    {
        if (this.ConnectionDiscoveredEvent != null)
        {
            this.ConnectionDiscoveredEvent.Invoke(this, new ConnectionDiscoveredArgs(parent, child, weight));
        }
    }

    protected virtual void RaiseHostDiscoveredEvent(Uri host, DateTime time, object[] attributes)
    {
        if (this.HostDiscoveredEvent != null)
        {
            this.HostDiscoveredEvent.Invoke(this, new HostDiscoveredArgs(host, time, attributes));
        }
    }


    public delegate void HostDiscoveredEventHandler(object sender, HostDiscoveredArgs e);

    public delegate void ConnectionDiscoveredEventHandler(object sender, ConnectionDiscoveredArgs e);


    public event ConnectionDiscoveredEventHandler ConnectionDiscoveredEvent;

    public event HostDiscoveredEventHandler HostDiscoveredEvent;
}

/// <summary>Crawler events logic.</summary>
public partial class Crawler
{
    protected virtual void RaiseStatusEvent(string status, double progress)
    {
        if (this.StatusEvent != null)
        {
            this.StatusEvent.Invoke(this, new StatusArgs(status, progress));
        }
    }

    public delegate void StatusEventHandler(object sender, StatusArgs e);

    public event StatusEventHandler StatusEvent;
}

}