namespace WebCrawler
{

/// <summary>Crawler configuration class.</summary>
public class Configuration
{
    /// <summary>Needed for serialization.</summary>
    public Configuration() { }

    /// <summary>A value indicating whether to logging to text file enabled or not.</summary>
    public bool EnableLog { get; set; }

    /// <summary>Gets or sets full path to the output folder.</summary>
    public string OutputPath { get; set; }

    /// <summary>Gets or sets full path to the log file.</summary>
    public string LogFilePath { get; set; }

    /// <summary>A value indicating whether to persist robots file or not.</summary>
    public bool SaveRobotsFile { get; set; }

    /// <summary>A value indicating whether to persist sitemap files or not.</summary>
    public bool SaveSitemapFiles { get; set; }
    
    /// <summary>A value indicating whether to persist urls or not.</summary>
    public bool SaveUrls { get; set; }

    /// <summary>A value indicating whether to delete every html page 
    /// after scraping and parsing is done. Site might have a lot of html pages and saving
    /// them locally on disk might be problematic.</summary>
    public bool DeleteHtmlAfterScrape { get; set; }

    /// <summary>A value indicating whether to serialize site object or not.</summary>
    public bool SerializeSite { get; set; }

    /// <summary>A value indicating whether to serialize graph object or not.</summary>
    public bool SerializeGraph { get; set; }

    /// <summary>Gets or sets maximum urls per host to be scraped.</summary>
    /// <remarks>Some hosts can have huge amount of html pages so its needed to 
    /// set the limit otherwise crawler can get stuck on those sites.</remarks>
    public int HostUrlsLimit {  get; set; }

    /// <summary>Gets or sets maximum sitemap index files per host to be analyzed.</summary>
    /// <remarks>Some hosts can have huge amount of index files so its needed to 
    /// set the limit otherwise crawler can get stuck on those sites.</remarks>
    public int SitemapIndexLimit {  get; set; }
}

}
