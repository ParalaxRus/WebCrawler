using System;
using System.Data;
using System.Collections.Generic;

namespace WebCrawler
{

/// <summary>Sites data base.</summary>
internal class SiteDataBase
{
    private DataSet set = new DataSet("SitesDataSet");
    private string sitesName = "SitesTable";
    private string connectionsName = "ConnectionsTable";
    private string idName = "Id";

    private DataTable CreateSites()
    {
        var table = new DataTable(this.sitesName);

        var columns = new List<DataColumn>()
        {
            new DataColumn(this.idName, Type.GetType("System.Int32"))
            {
                ReadOnly = true,
                Unique = true
            },
            new DataColumn("Host", Type.GetType("System.String"))
            {
                Unique = true,
                AutoIncrement = false
            },
            new DataColumn("Robots", Type.GetType("System.Bool")),
            new DataColumn("Sitemap", Type.GetType("System.Bool"))
        };

        table.Columns.AddRange(columns.ToArray());

        table.PrimaryKey = new DataColumn[1]
        {
            table.Columns[this.idName]
        };

        return table;
    }

    private DataTable CreateConnections()
    {
        var table = new DataTable(this.connectionsName);

        var columns = new List<DataColumn>()
        {
            new DataColumn(this.idName, Type.GetType("System.Int32"))
            {
                ReadOnly = true,
                Unique = true
            },
            new DataColumn("ParentId", Type.GetType("System.Int32")),
            new DataColumn("ChildId", Type.GetType("System.Int32")),
        };

        table.Columns.AddRange(columns.ToArray());

        table.PrimaryKey = new DataColumn[1]
        {
            table.Columns[this.idName]
        };

        return table;
    }

    private void Insert(string host, int parentId, bool isRobotsFile, bool isSitemap)
    {
        var sites = this.set.Tables[this.sitesName];
        var connections = this.set.Tables[this.connectionsName];

        var sitesRow = sites.NewRow();
        sitesRow["Host"] = host;
        sitesRow["Robots"] = isRobotsFile;
        sitesRow["Sitemap"] = isSitemap;

        var connectionsRow = connections.NewRow();
        connectionsRow["ParentId"] = parentId;
        connectionsRow["ChildId"] = sitesRow[this.idName];
    }

    private DataRow GetHost(string host)
    {
        var sites = this.set.Tables[this.sitesName];

        string hostLookup = "Host=" + host;
        var rows = sites.Select(hostLookup);

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

    public SiteDataBase() 
    {
        var sites = this.CreateSites();
        var connections = this.CreateConnections();

        this.set.Tables.Add(sites);
        this.set.Tables.Add(connections);

        var parent = this.set.Tables[this.sitesName].Columns[this.idName];
        var child = this.set.Tables[this.connectionsName].Columns["ParentId"];
        var relation = new DataRelation("SitesToConnections", parent, child);
        this.set.Tables[this.connectionsName].ParentRelations.Add(relation);
    }

    public void Add(string host, string parent, bool isRobotsFile, bool isSitemap)
    {
        var parentRow = this.GetHost(parent);
        if (parentRow == null)
        {
            throw new ArgumentException(string.Format("Parent host {0} does not exist", parent));
        }

        var hostRow = this.GetHost(host);
        if (hostRow == null)
        {
            this.Insert(host, (int)parentRow[this.idName], isRobotsFile, isSitemap);
        }
        else
        {
            //this.Update(host, parent, isRobotsFile, isSitemap);
        }
    }
}

}