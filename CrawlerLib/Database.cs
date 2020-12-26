using System;
using System.Data;
using System.IO;
using System.Collections.Generic;

namespace WebCrawler
{

/// <summary>Crawler persistent data base.</summary>
public class DataBase
{
    private const string sitesName = "SitesTable";
    private const string connectionsName = "ConnectionsTable";
    private const string idName = "Id";
    private const string parentIdName = "ParentId";
    private const string childIdName = "ChildId";
    private const string hostName = "Host";

    private DataSet set = new DataSet("CrawlerDataSet");

    private DataTable CreateSites()
    {
        var table = new DataTable(DataBase.sitesName);

        var columns = new List<DataColumn>()
        {
            new DataColumn(DataBase.idName, typeof(Int32))
            {
                ReadOnly = true,
                Unique = true,
                AutoIncrement = true
            },
            new DataColumn(DataBase.hostName, typeof(string))
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
            table.Columns[DataBase.idName]
        };

        return table;
    }

    private DataTable CreateConnections()
    {
        var table = new DataTable(DataBase.connectionsName);

        var columns = new List<DataColumn>()
        {
            new DataColumn(DataBase.idName, typeof(Int32))
            {
                ReadOnly = true,
                Unique = true,
                AutoIncrement = true
            },
            new DataColumn(DataBase.parentIdName, typeof(Int32)),
            new DataColumn(DataBase.childIdName, typeof(Int32)),
        };

        table.Columns.AddRange(columns.ToArray());

        table.PrimaryKey = new DataColumn[1]
        {
            table.Columns[DataBase.idName]
        };

        return table;
    }

    private void InsertHost(string host, bool isRobotsFile, bool isSitemap)
    {
        var table = this.set.Tables[DataBase.sitesName];

        var row = table.NewRow();
        row[DataBase.hostName] = host;
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
        var table = this.set.Tables[DataBase.connectionsName];

        var row = table.NewRow();
        row[DataBase.parentIdName] = parentId;
        row[DataBase.childIdName] = childId;

        table.Rows.Add(row);
    }

    private DataRow GetHostByName(string host)
    {
        var table = this.set.Tables[DataBase.sitesName];

        string hostLookup = string.Format("{0}='{1}'", hostName, host);
        var rows = table.Select(hostLookup);

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
            throw new ArgumentException(string.Format("Duplicate host {0} found", host));
        }
    }

    private DataRow GetHostById(int id)
    {
        var table = this.set.Tables[DataBase.sitesName];

        string hostLookup = string.Format("{0}={1}", DataBase.idName, id);
        var rows = table.Select(hostLookup);

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
            throw new ArgumentException(string.Format("Duplicate ID {0} found", id));
        }
    }

    private DataRow GetConnection(int parentId, int childId)
    {
        var table = this.set.Tables[DataBase.connectionsName];

        string lookUp = string.Format("{0}={1} and {2}={3}", DataBase.parentIdName, parentId, DataBase.childIdName, childId);
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
        row[DataBase.parentIdName] = parentId;
        row[DataBase.childIdName] = childId;
    }

    public DataBase()
    {
        var sites = this.CreateSites();
        var connections = this.CreateConnections();

        this.set.Tables.Add(sites);
        this.set.Tables.Add(connections);

        var parent = this.set.Tables[DataBase.sitesName].Columns[DataBase.idName];
        var child = this.set.Tables[DataBase.connectionsName].Columns[DataBase.parentIdName];
        var relation = new DataRelation("SitesToConnections", parent, child);
        this.set.Tables[DataBase.connectionsName].ParentRelations.Add(relation);
    }

    public void AddHost(string host, bool isRobotsFile, bool isSitemap)
    {
        var hostRow = this.GetHostByName(host);
        if (hostRow == null)
        {
            this.InsertHost(host, isRobotsFile, isSitemap);
        }
        else
        {
            DataBase.UpdateHost(hostRow, isRobotsFile, isSitemap);
        }
    }

    public void AddConnection(string parent, string child)
    {
        var parentRow = this.GetHostByName(parent);
        if (parentRow == null)
        {
            throw new ArgumentException(string.Format("Parent {0} does not exist", parent));
        }

        var childRow = this.GetHostByName(child);
        if (childRow == null)
        {
            throw new ArgumentException(string.Format("Child {0} does not exist", child));
        }

        int parentId = (int)parentRow[DataBase.idName];
        int childId = (int)childRow[DataBase.idName];
        var connectionRow = this.GetConnection(parentId, childId);
        if (connectionRow == null)
        {
            this.InsertConnection(parentId, childId);
        }
        else
        {
            // This should not change row because parentId and childId are the same
            // However this might be handy in future in case if new columns/properties added 
            // to the connections table
            DataBase.UpdateConnection(connectionRow, parentId, childId);
        }
    }

    public List<string> GetChildren(string parent)
    {
        var parentRow = this.GetHostByName(parent);
        if (parentRow == null)
        {
            throw new ArgumentException(string.Format("Parent {0} does not exist", parent));
        }

        int parentId = (int)parentRow[DataBase.idName];
        string childrenLookup = string.Format("{0}={1}", DataBase.parentIdName, parentId);

        var table = this.set.Tables[DataBase.connectionsName];
        var rows = table.Select(childrenLookup);

        var children = new List<string>(rows.Length);

        foreach (var row in rows)
        {
            int childId = (int)row[DataBase.childIdName];

            var childRow = this.GetHostById(childId);
            if (childRow == null)
            {
                throw new ApplicationException(string.Format("Failed to find child row by id={0}", childId));
            }

            children.Add((string)childRow[DataBase.hostName]);
        }

        return children;
    }

    public object[] GetHostRecord(string host)
    {
        var row = this.GetHostByName(host);
        return row != null ? row.ItemArray : null;
    }

    public int GetHostCount()
    {
        var sites = this.set.Tables[DataBase.sitesName];

        return sites.Rows.Count;
    }

    public void Serialize(string file)
    {
        if (file == null)
        {
            throw new ArgumentException("file");
        }

        File.Delete(file);

        this.set.WriteXml(file, XmlWriteMode.WriteSchema);
    }

    public void Deserialize(string file)
    {
        if (!File.Exists(file))
        {
            throw new ArgumentException("file");
        }

        this.set.ReadXml(file, XmlReadMode.ReadSchema);
    }
}

}