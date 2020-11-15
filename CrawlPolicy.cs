using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebCrawler
{
    internal class Agent
    {
        public string Name { get; private set; }

        public HashSet<string> Allow { get; private set; }

        public HashSet<string> Disallow { get; private set; }

        public Agent(string name)
        {
            this.Name = name;
            this.Allow = new HashSet<string>();
            this.Disallow = new HashSet<string>();
        }
    }

    internal class CrawlPolicy
    {
        private Uri root = null;

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
                        this.SitemapUri = new Uri(value);
                    }
                }
            }
        }

        public string RobotsFile { get; private set; }

        public Uri RobotsUri { get; private set; }

        public Uri SitemapUri { get; private set; }

        /// <summary>Gets crowler rules for the specified agent.</summary>
        /// <param name="agent">Crowler agent. Star represents settings for all agents.</param>
        public Agent GetAgentPolicy(string agent = "*")
        {
            if (!this.agents.ContainsKey(agent))
            {
                // Empty policy
                return new Agent("");
            }

            return this.agents[agent];
        }

        /// <summary>Get policies from site's robots file.</summary>
        /// <param name="uri">Site url.</param>
        /// <param name="path">Path to the site location on disk.</param>
        public async Task<bool> DownloadPolicyAsync(Uri uri, string path, bool saveRobotsFile = true)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            this.root = uri;
            this.RobotsFile = Path.Combine(path, "robots.txt");
            this.RobotsUri = new Uri(this.root + "robots.txt");

            try
            {
                var res = await new AsyncDonwload().DownloadAsync(this.RobotsUri, this.RobotsFile);
                if (!res)
                {
                    return false;
                }

                this.Parse(this.RobotsFile);
            }
            finally
            {
                if (!saveRobotsFile)
                {
                    File.Delete(this.RobotsFile);
                }
            }

            return true;
        }
    }
}