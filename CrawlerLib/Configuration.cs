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

    public bool SaveRobotsFile { get; set; }

    public bool SaveSitemapFiles { get; set; }
    
    public bool SaveUrls { get; set; }

    /// <summary>A value indicating whether to delete every html page 
    /// after scraping and parsing is done. Site might have a lot of html pages and saving
    /// them locally on disk might be problematic.</summary>
    public bool DeleteHtmlAfterScrape { get; set; }

    /// <summary>A value indicating whether to serialize site object or not.</summary>
    public bool SerializeSite { get; set; }

    /// <summary>A value indicating whether to serialize graph object or not.</summary>
    public bool SerializeGraph { get; set; }
}

}
