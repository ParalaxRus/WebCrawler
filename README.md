# WebCrawler

WebCrawler pet project in C#


CrawlerApp - console application
CrawlerLib - main assembly
CrawlerTests - VS unit tests
WinUiApp - Windows Forms UI application


Breif description:
- Starts crawling with provided seed sites
- Builds directed connected graph with the weighted edges
- Graph can be persisted. Serialization to a file and dataset is supported


Crawling steps:
- Tries to retrieve crawl policy from the robots site if provided
- To crawl a seed site it determines its structure according with the sitemap if any
- Downloads htmls, parses them and retrieves references to another sites and updates graph

Notes:
 - Supported protocols: https
 - To run test in VSC UI make sure that omnisharp project is crawler.sln 
   (command pallete -> omnisharp -> select project)

To-Do:
 1) Figure out Uri scheme with graph issue (for now its restricted to https)

Issues/bugs:
