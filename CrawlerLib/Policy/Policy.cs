using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WebCrawler
{
    /// <summary>Agent policy class.</summary>
    /// <remarks>Allow/disallow policy rules 
    /// https://www.deepcrawl.com/knowledge/technical-seo-library/robots-txt/</remarks>
    public class Policy
    {
        private class ByLengthDescending : IComparer<string>
        {
            public int Compare(string first, string second)
            {
                return second.Length.CompareTo(first.Length);
            }
        }

        /// <summary>Sorted collection of allowed urls (needed to be sorted to support longest match win rule).</summary>
        private SortedSet<string> allowed = new SortedSet<string>(new ByLengthDescending());

        /// <summary>Sorted collection of dissalowed urls (needed to be sorted to support longest match win rule).</summary>
        private SortedSet<string> disallowed = new SortedSet<string>(new ByLengthDescending());

        /// <summary>Checks whether suffix represents valid/supported policy record or not.</summary>
        private bool IsValid(string value)
        {
            if (value.Length == 0)
            {
                return false;
            }            

            var uri = new Uri(this.Site, value);
            if (uri.Query.Length != 0)
            {
                Trace.TraceWarning(string.Format("Skipping query record {0}", value));

                return false;
            }

            return true;
        }

        private bool Add(SortedSet<string> set, string value)
        {
            if (!this.IsValid(value))
            {
                Trace.TraceWarning(string.Format("Policy record {0} is not supported", value));

                return false;
            }

            set.Add(value);

            return true;
        }

        private int GetLongestMatch(SortedSet<string> set, string localPath)
        {
            foreach (var val in set)
            {
                var match = Regex.Match(localPath, val);
                if (match.Success)
                {
                    return val.Length;
                }
            }   

            return -1;
        }

        /// <summary>Gets or sets site's url.</summary>
        public Uri Site { get; private set; }

        /// <summary>Gets agent's name.</summary>
        public string Agent { get; private set; }

        /// <summary>A value indicating whether site contains robots file or not.</summary>
        public bool IsRobots { get; set; }

        /// <summary>Checks whether site contains sitemap or not.</summary>
        public bool IsSitemap { get { return this.Sitemap != null; }}

        /// <summary>Gets or sets sitemap url if any.</summary>
        public Uri Sitemap { get; set; }

        /// <summary>Checks whether policy is empty or not.</summary>
        public bool IsEmpty { get { return this.Site == null; } }

        /// <summary>Empty policy constructor.</summary>
        public Policy()
        {
            this.Site = null;
            this.Agent = null;
            this.Sitemap = null;
        }

        public Policy(Uri site, string agent) : this()
        {
            this.Site = site;
            this.Agent = agent;
        }

        public bool AddAllowed(string value)
        {
            return this.Add(this.allowed, value);
        }

        public bool AddDisallowed(string value)
        {
            return this.Add(this.disallowed, value);
        }

        /// <summary>Checks wether specified local path (relative to host url) is 
        /// allowed to crawl by a current agent or not.</summary>
        public bool IsAllowed(string localPath)
        {
            int maxDisallowedMatch = this.GetLongestMatch(this.disallowed, localPath);
            if (maxDisallowedMatch == -1)
            {
                // No match found
                return true;
            }

            int maxAllowedMatch = this.GetLongestMatch(this.allowed, localPath);

            // Longest match wins however equality favors disallowed rule
            return (maxAllowedMatch > maxDisallowedMatch); 
        }
    }
}