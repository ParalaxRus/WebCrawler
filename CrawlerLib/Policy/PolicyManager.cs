using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebCrawler
{
    /// <summary>Policy manager class helps to detect policies for concrete agents.</summary>
    public class PolicyManager
    {
        private Uri site = null;

        private Uri robotsUrl = null;

        private string robotsPath = null;

        private bool robotsDetected = false;

        private HashSet<Uri> sitemaps = null;

        /// <summary>Agents to policies table.</summary>
        private Dictionary<string, Policy> policies = new Dictionary<string, Policy>();

        private void Parse(string file)
        {
            using (var reader = new StreamReader(file))
            {
                string currentAgent = null;

                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        // EOF
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var parts = line.Split();
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    var key = parts[0].Trim().ToLower();
                    var value = parts[1].Trim().ToLower();

                    if (key == "user-agent:")
                    {
                        currentAgent = value;
                        if (!this.policies.ContainsKey(currentAgent))
                        {
                            this.policies.Add(currentAgent, new Policy(this.site, currentAgent));
                        }
                    }
                    else if (key == "allow:")
                    {
                        this.policies[currentAgent].AddAllowed(value);
                    }
                    else if (key == "disallow:")
                    {    
                        this.policies[currentAgent].AddDisallowed(value);
                    }
                    else if (key == "sitemap:")
                    {
                        // Site contains sitemap file with static site structure
                        this.sitemaps.Add(new Uri(value));
                    }
                    else if (key == "crawl-delay:")
                    {
                        int minDelay = - 1;
                        if (!Int32.TryParse(value, out minDelay))
                        {
                            minDelay = Scraper.Settings.DefaultMinDelay - 1;
                        }

                        // Adding extra second just in case
                        this.policies[currentAgent].CrawlDelayInSecs = minDelay + 1;
                    }
                }
            }
        }

        public PolicyManager(Uri siteUrl, Uri robotsUrl, string robotsPath)
        {
            if (siteUrl == null)
            {
                throw new ArgumentNullException("siteUrl");
            }

            if (robotsUrl == null)
            {
                throw new ArgumentNullException("robotsUrl");
            }

            if (robotsPath == null)
            {
                throw new ArgumentNullException("robotsPath");
            }

            this.site = siteUrl;
            this.robotsUrl = robotsUrl;
            this.robotsPath = robotsPath;
            this.sitemaps = new HashSet<Uri>();
        }

        /// <summary>Gets policy for the specified agent.</summary>
        /// <param name="agent">Star a common non-agent specific policy.</param>
        public Policy GetPolicy(string agent = "*")
        {
            if (!this.policies.ContainsKey(agent))
            {
                // Empty policy
                return new Policy();
            }

            var current = this.policies[agent];

            // Following settings are applied to all agents
            // Maybe refactor because they don't really belong to agent policy
            current.Sitemaps = this.sitemaps;
            current.IsRobots = this.robotsDetected;

            return current;
        }

        /// <summary>Retrieves site policies if any.</summary>
        public async Task<bool> RetrieveAsync()
        {
            var res = await new UriDownload().DownloadAsync(this.robotsUrl, this.robotsPath);
            if (!res)
            {
                return false;
            }

            this.robotsDetected = true;
            this.Parse(this.robotsPath);

            return true;
        }
    }
}