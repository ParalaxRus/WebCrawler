using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebCrawler
{
    public class Agent
    {
        public string Name { get; private set; }

        /// <summary>A value indicating whether site contains robots file or not.</summary>
        public bool IsRobots { get; set; }

        /// <summary>Checks whether site contains sitemap or not.</summary>
        public bool IsSitemap { get { return this.Sitemap != null; }}

        /// <summary>Gets or sets sitemap url if any.</summary>
        public Uri Sitemap { get; set; }

        public HashSet<string> Allow { get; private set; }

        public HashSet<string> Disallow { get; private set; }

        public Agent(string name)
        {
            this.Name = name;
            this.Sitemap = null;
            this.Allow = new HashSet<string>();
            this.Disallow = new HashSet<string>();
        }
    }

    /// <summary>Site policy detector class.</summary>
    public class SitePolicy
    {
        private bool robotsDetected = false;

        private Uri robotsUrl = null;

        private string robotsPath = null;

        private Uri sitemap = null;

        private Dictionary<string, Agent> agents = new Dictionary<string, Agent>();

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
                        if (!this.agents.ContainsKey(currentAgent))
                        {
                            this.agents.Add(currentAgent, new Agent(currentAgent));
                        }
                    }
                    else if (key == "allow:")
                    {
                        this.agents[currentAgent].Allow.Add(value);
                    }
                    else if (key == "disallow:")
                    {    
                        this.agents[currentAgent].Disallow.Add(value);
                    }
                    else if (key == "sitemap:")
                    {
                        // Site contains sitemap file with static site structure
                        this.sitemap = new Uri(value);
                    }
                }
            }
        }

        public SitePolicy(Uri robotsUrl, string robotsPath)
        {
            if (robotsUrl == null)
            {
                throw new ArgumentNullException("robotsUrl");
            }

            if (robotsPath == null)
            {
                throw new ArgumentNullException("robotsPath");
            }

            this.robotsUrl = robotsUrl;
            this.robotsPath = robotsPath;
        }

        /// <summary>Gets crawler rules for the specified agent.</summary>
        /// <param name="agent">Crawler agent. Star represents settings for all agents.</param>
        public Agent GetPolicy(string agent = "*")
        {
            if (!this.agents.ContainsKey(agent))
            {
                // Empty policy
                return new Agent("");
            }

            var current = this.agents[agent];

            // Following settings are applied to all agents
            // Maybe refactor because they don't really belong to agent policy
            current.Sitemap = this.sitemap;
            current.IsRobots = this.robotsDetected;

            return current;
        }

        /// <summary>Get policies from site's robots file if any.</summary>
        public async Task<bool> DetectAsync()
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