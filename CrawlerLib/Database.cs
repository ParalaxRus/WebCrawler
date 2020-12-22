using System;
using System.Data;
using System.Collections.Generic;

namespace WebCrawler
{

/// <summary>Sites data base.</summary>
public class SiteDataBase
{
    private const string sitesName = "SitesTable";
    private const string connectionsName = "ConnectionsTable";
    private const string idName = "Id";
    private const string parentIdName = "ParentId";
    private const string childIdName = "ChildId";

    private DataSet set = new DataSet("SitesDataSet");

    private DataTable CreateSites()
    {
        var table = new DataTable(SiteDataBase.sitesName);

        var columns = new List<DataColumn>()
        {
            new DataColumn(SiteDataBase.idName, typeof(Int32))
            {
                ReadOnly = true,
                Unique = true,
                AutoIncrement = true
            },
            new DataColumn("Host", typeof(string))
            {
                Unique = true,
                AutoIncrement = false
            },
            new DataColumn("Robots", typeof(bool)),
            new DataColumn("Sitemap", typeof(bool))
        };

        table.Columns.AddRange(columns.ToArray());

        table.PrimaryKey = new DataColumn[1]
        {
            table.Columns[SiteDataBase.idName]
        };

        return table;
    }

    private DataTable CreateConnections()
    {
        var table = new DataTable(SiteDataBase.connectionsName);

        var columns = new List<DataColumn>()
        {
            new DataColumn(SiteDataBase.idName, typeof(Int32))
            {
                ReadOnly = true,
                Unique = true
            },
            new DataColumn(SiteDataBase.parentIdName, typeof(Int32)),
            new DataColumn(SiteDataBase.childIdName, typeof(Int32)),
        };

        table.Columns.AddRange(columns.ToArray());

        table.PrimaryKey = new DataColumn[1]
        {
            table.Columns[SiteDataBase.idName]
        };

        return table;
    }

    private void InsertHost(string host, bool isRobotsFile, bool isSitemap)
    {
        var table = this.set.Tables[SiteDataBase.sitesName];

        var row = table.NewRow();
        row["Host"] = host;
        row["Robots"] = isRobotsFile;
        row["Sitemap"] = isSitemap;

        table.Rows.Add(row);
    }

    private static void UpdateHost(DataRow row, bool isRobotsFile, bool isSitemap)
    {
        row["Robots"] = isRobotsFile;
        row["Sitemap"] = isSitemap;
    }

    private void InsertConnection(int parentId, int childId)
    {
        var table = this.set.Tables[SiteDataBase.connectionsName];

        var row = table.NewRow();
        row[SiteDataBase.parentIdName] = parentId;
        row[SiteDataBase.childIdName] = childId;

        table.Rows.Add(row);
    }

    private DataRow GetHost(string host)
    {
        var table = this.set.Tables[SiteDataBase.sitesName];

        string hostLookup = string.Format("Host='{0}'", host);
        DataRow[] rows = table.Select(hostLookup);

        if (rows.Length == 0)
        {
            return null;
        }
        else if (rows.Length == 1)
        {
            return rows[0];
        }
        else
        {
            throw new ArgumentException(string.Format("Duplicate host '{}' found", host));
        }
    }

    private DataRow GetConnection(int parentId, int childId)
    {
        var table = this.set.Tables[SiteDataBase.connectionsName];

        string lookUp = string.Format("{0}={1} {2}={3}", SiteDataBase.parentIdName, parentId, SiteDataBase.childIdName, childId);
        var rows = table.Select(lookUp);

        if (rows.Length == 0)
        {
            return null;
        }
        else if (rows.Length == 1)
        {
            return rows[0];
        }
        else
        {
            throw new ArgumentException(string.Format("Duplicate edge '{0} - {1}' found", parentId, childId));
        }
    }

    private static void UpdateConnection(DataRow row, int parentId, int childId)
    {
        row[SiteDataBase.parentIdName] = parentId;
        row[SiteDataBase.childIdName] = childId;
    }

    public SiteDataBase()
    {
        var sites = this.CreateSites();
        var connections = this.CreateConnections();

        this.set.Tables.Add(sites);
        this.set.Tables.Add(connections);

        var parent = this.set.Tables[SiteDataBase.sitesName].Columns[SiteDataBase.idName];
        var child = this.set.Tables[SiteDataBase.connectionsName].Columns[SiteDataBase.parentIdName];
        var relation = new DataRelation("SitesToConnections", parent, child);
        this.set.Tables[SiteDataBase.connectionsName].ParentRelations.Add(relation);
    }

    public void AddHost(string host, bool isRobotsFile, bool isSitemap)
    {
        var hostRow = this.GetHost(host);
        if (hostRow == null)
        {
            this.InsertHost(host, isRobotsFile, isSitemap);
        }
        else
        {
            SiteDataBase.UpdateHost(hostRow, isRobotsFile, isSitemap);
        }
    }

    public void AddConnection(string parent, string child)
    {
        var parentRow = this.GetHost(parent);
        if (parentRow == null)
        {
            throw new ArgumentException(string.Format("Parent {0} does not exist", parent));
        }

        var childRow = this.GetHost(child);
        if (childRow == null)
        {
            throw new ArgumentException(string.Format("Child {0} does not exist", child));
        }

        int parentId = (int)parentRow[SiteDataBase.idName];
        int childId = (int)childRow[SiteDataBase.idName];
        var connectionRow = this.GetConnection(parentId, childId);
        if (connectionRow == null)
        {
            this.InsertConnection(parentId, childId);
        }
        else
        {
            SiteDataBase.UpdateConnection(connectionRow, parentId, childId);
        }
    }

    public object[] GetHostRecord(string host)
    {
        var row = this.GetHost(host);
        return row != null ? row.ItemArray : null;
    }

    public int GetHostCount()
    {
        var sites = this.set.Tables[SiteDataBase.sitesName];

        return sites.Rows.Count;
    }
}

}