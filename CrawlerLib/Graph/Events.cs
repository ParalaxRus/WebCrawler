using System;

namespace WebCrawler
{

public class ConnectionDiscoveredArgs : EventArgs
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

public class HostDiscoveredArgs : EventArgs
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

        if (attributes == null)
        {
            throw new ArgumentNullException("attributes");
        }

        this.Host = host;
        this.DiscoveryTime = time;
        this.Attributes = attributes;
    }
}

public class StatusArgs : EventArgs
{
    public string Status { get; }

    public StatusArgs(string status) 
    { 
        if (status == null)
        {
            throw new ArgumentNullException("status");
        }

        this.Status = status;
    }
}

public class ProgressArgs : EventArgs
{
    public double Progress { get; }

    public ProgressArgs(double progress) 
    { 
        if ((progress < 0.0) || (progress > 1.0))
        {
            throw new ArgumentNullException("progress");
        }

        this.Progress = progress;
    }
}

/// <summary>Graph events logic.</summary>
public partial class Graph
{
    #region Connection event

    protected virtual void RaiseConnectionDiscoveredEvent(Uri parent, Uri child, int weight)
    {
        if (this.ConnectionDiscoveredEvent != null)
        {
            this.ConnectionDiscoveredEvent.Invoke(this, new ConnectionDiscoveredArgs(parent, child, weight));
        }
    }

    public delegate void ConnectionDiscoveredEventHandler(object sender, ConnectionDiscoveredArgs e);

    public event ConnectionDiscoveredEventHandler ConnectionDiscoveredEvent;

    #endregion

    #region Host event

    protected virtual void RaiseHostDiscoveredEvent(Uri host, DateTime time, object[] attributes)
    {
        if (this.HostDiscoveredEvent != null)
        {
            this.HostDiscoveredEvent.Invoke(this, new HostDiscoveredArgs(host, time, attributes));
        }
    }

    public delegate void HostDiscoveredEventHandler(object sender, HostDiscoveredArgs e);

    public event HostDiscoveredEventHandler HostDiscoveredEvent;

    #endregion
}

/// <summary>Crawler events logic.</summary>
public partial class Crawler
{
    #region Status event

    protected virtual void RaiseStatusEvent(string status)
    {
        if (this.StatusEvent != null)
        {
            this.StatusEvent.Invoke(this, new StatusArgs(status));
        }
    }

    public delegate void StatusEventHandler(object sender, StatusArgs e);

    public event StatusEventHandler StatusEvent;

    #endregion

    #region Progress event

    protected virtual void RaiseProgressEvent(double progress)
    {
        if (this.ProgressEvent != null)
        {
            this.ProgressEvent.Invoke(this, new ProgressArgs(progress));
        }
    }

    public delegate void ProgressEventHandler(object sender, ProgressArgs e);

    public event ProgressEventHandler ProgressEvent;

    #endregion
}

}