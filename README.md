# WebCrawler

WebCrawler pet project in C#

App, Lib and Unit Tests

Breif description:
- Starts crawling with provided seed sites
- Builds directed connected graph with the weighted edges
- Graph can be persisted. Serialization to a file and dataset is supported

Crawling steps:
- Tries to retrieve crawl policy from the robots site if provided
- To crawl a seed site it determines its structure according with the sitemap if any
- Downloads htmls, parses them and retrieves references to another sites and updates graph
