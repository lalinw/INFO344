using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using HtmlAgilityPack;
using System.IO;
using ClassLibrary1;
using System.Text.RegularExpressions;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private static HashSet<string> visitedLinks = new HashSet<string>(); //store visited Links
        private static Dictionary<string, List<string>> disallowList = new Dictionary<string, List<string>>();

        //initialize the stats parameters for the tables and queues etc.
        public static List<string> lastTen = new List<string> { };
        public static List<string> tenErrors = new List<string> { };
        public static bool crawlYes = true;
        public int countertemp = 0;

        public override void Run()
        {
            CloudQueue queue = getQueue();
            CloudTable stat = statTable();

            //initialize the statTable
            Stats startStat = new Stats();
            TableOperation initializeStats = TableOperation.InsertOrReplace(startStat);
            stat.Execute(initializeStats);

            while (true) {
                checkCmdQueue();
                while (crawlYes) {
                    checkCmdQueue();
                    CloudQueueMessage retrievedMessage = queue.GetMessage();
                    if (retrievedMessage != null) {
                        string link = retrievedMessage.AsString;
                        if (link.EndsWith("robots.txt")) {
                            parseRobot(link);
                        } else if (link.EndsWith(".xml") && countertemp <= 2) {
                            parseXml(link);
                            countertemp++;
                        } else {
                            parseHtml(link);
                        }
                        queue.DeleteMessage(retrievedMessage);
                    } else {
                        checkCmdQueue();
                        updateWorkerState("Idling");
                        Thread.Sleep(50);
                        //if queue is empty, let the worker role sleep
                    }
                }
            }
        }






        private string checkCmdQueue() {
            CloudQueue cmdQueue = getCommandQueue();
            //check for command
            CloudQueueMessage nextCmd = cmdQueue.GetMessage();
            if (nextCmd != null)
            {
                string cmdString = nextCmd.AsString;
                if (cmdString.Equals("run"))
                {
                    //start or resume crawling
                    crawlYes = true;
                    updateWorkerState("Loading");
                } else {
                    //stop crawling
                    crawlYes = false;
                    updateWorkerState("Stopped");
                }
                cmdQueue.DeleteMessage(nextCmd);
            }
            return "check queue";
        }

        //post: increase the table size column in the stat table by 1
        private string updateTableSize()
        {
            CloudTable table = statTable();
            TableQuery<Stats> search = new TableQuery<Stats>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "stats")
            );
            var result = table.ExecuteQuery(search).ToList();
            Stats retrieved = result[0];
            retrieved.tableSize++;
            TableOperation upsertOperation = TableOperation.InsertOrReplace(retrieved);
            table.Execute(upsertOperation);
            return "update tableSize";
        }

        //post: increase the total urls column in the stat table by 1
        private string updateTotalUrls()
        {
            CloudTable table = statTable();
            TableQuery<Stats> search = new TableQuery<Stats>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "stats")
            );
            var result = table.ExecuteQuery(search).ToList();
            Stats retrieved = result[0];
            retrieved.totalUrls++;
            TableOperation upsertOperation = TableOperation.InsertOrReplace(retrieved);
            table.Execute(upsertOperation);
            return "update total urls";
        }

        //pre:  takes the name of the new state as string
        //post: sets the worker state to the new state 
        private string updateWorkerState(string newState)
        {
            CloudTable table = statTable();
            TableQuery<Stats> search = new TableQuery<Stats>().Where(
               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "stats")
            );
            var result = table.ExecuteQuery(search).ToList();
            Stats retrieved = result[0];
            retrieved.workerState = newState;
            TableOperation upsertOperation = TableOperation.InsertOrReplace(retrieved);
            table.Execute(upsertOperation);
            return "update worker state";
        }

        //pre:  takes an HTML link as string
        //post: adds a link to the end of the last crawled list
        //      removes the list if it already has 10 elements
        //      to keep the list size <= 10
        private string updateLast10Links(string newlink)
        {
            CloudTable table = statTable();
            TableQuery<Stats> search = new TableQuery<Stats>().Where(
               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "stats")
            );
            var result = table.ExecuteQuery(search).ToList();
            Stats retrieved = result[0];
            if (lastTen.Count >= 10)
            {
                lastTen.RemoveAt(0);
            }
            lastTen.Add(newlink);
            string recentLinks = string.Join(",", lastTen.ToArray());
            retrieved.lastCrawled = recentLinks;
            TableOperation upsertOperation = TableOperation.InsertOrReplace(retrieved);
            table.Execute(upsertOperation);

            return "update most recent links crawled";
        }

        //pre:  takes an HTML link as string
        //post: adds a link to the end of the error list
        //      removes the list if it already has 10 elements
        //      to keep the list size <= 10
        private string updateErrorLinks(string newlink)
        {
            CloudTable table = statTable();
            TableQuery<Stats> search = new TableQuery<Stats>().Where(
               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "stats")
            );
            var result = table.ExecuteQuery(search).ToList();
            Stats retrieved = result[0];
            if (tenErrors.Count >= 10)
            {
                tenErrors.RemoveAt(0);
            }
            tenErrors.Add(newlink);
            string recentLinks = string.Join(",", tenErrors.ToArray());
            retrieved.tenErrors = recentLinks;
            TableOperation upsertOperation = TableOperation.InsertOrReplace(retrieved);
            table.Execute(upsertOperation);

            return "update last 10 error links";
        }

        //pre:  take a link as string
        //post: checks the link with the conditions according to the specs
        //      returns true if it is valid to crawl, false otherwise
        private bool htmlOkayToAdd(string link) {

            Uri linkUri = new Uri(link);
            //check for disallow paths 
            if (linkUri.Host.Contains("cnn.com") && disallowList["www.cnn.com"] != null)
            {
                //if CNN, check for disallow for cnn
                foreach (string disallowPath in disallowList["www.cnn.com"])
                {
                    if (link.Contains(linkUri.Host + disallowPath))
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (linkUri.Host.Contains("bleacherreport.com"))
            {
                if (!linkUri.AbsolutePath.StartsWith("/articles"))
                {
                    return false;
                }
                else
                {
                    foreach (string disallowPath in disallowList["bleacherreport.com"])
                    {
                        if (link.Contains(linkUri.Host + disallowPath))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            else
            {
                return true;
            }
        }


        //pre:  take a link as string
        //post: Checks if the link from bleacherreport is nba-related
        private bool nbaXML(string link) {
            Uri linkUri = new Uri(link);
            if (linkUri.Host.EndsWith("bleacherreport.com"))
            {
                if (link.Contains(linkUri.Host + "/nba"))
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        //pre:  takes a link as string
        //post: parse HTML for links inside the page,
        //      mark the link as visited and crawled,
        //      add the new links to the queue
        private string parseHtml(string link) {
            updateWorkerState("Crawling");
            checkCmdQueue();
            //remove disallowed ones
            //remove already visited ones 
            //put valid links in queue
            HtmlWeb web = new HtmlWeb();
            try
            {
                HtmlDocument doc = web.Load(link);
                string title = doc.DocumentNode.SelectSingleNode("//head/title").InnerHtml;

                if (!title.Equals(""))
                {
                    Uri linkUri = new Uri(link);

                    bool validRoot = htmlOkayToAdd(link);

                    if (validRoot)
                    {
                        //cleaning the link in case of a request
                        link = linkUri.Scheme + "://" + linkUri.Host + linkUri.AbsolutePath;

                       

                        if (Uri.IsWellFormedUriString(link, UriKind.Absolute))
                        {
                            //check if the cleaned link is a "good" link or not
                            try {
                                var request = HttpWebRequest.Create(link);
                                request.Method = "HEAD";
                                var response = (HttpWebResponse)request.GetResponse();

                                if (!visitedLinks.Contains(link) && response.StatusCode == HttpStatusCode.OK)
                                //check if visited
                                {
                                    addToTable(link, title);
                                    visitedLinks.Add(link);
                                    //parsing the page
                                    foreach (HtmlNode linkitem in doc.DocumentNode.SelectNodes("//a[@href]"))
                                    {
                                        // Get the value of the HREF attribute
                                        string hrefValue = linkitem.GetAttributeValue("href", "");
                                        if (Uri.IsWellFormedUriString(hrefValue, UriKind.Absolute) && htmlOkayToAdd(hrefValue) && !visitedLinks.Contains(hrefValue))
                                        {
                                            addToQueue(hrefValue);
                                        }
                                        else if (Uri.IsWellFormedUriString(hrefValue, UriKind.Relative))
                                        {
                                            if (hrefValue != "/")
                                            {
                                                string newLink = linkUri.Scheme + "://" + linkUri.Host + hrefValue;
                                                if (htmlOkayToAdd(newLink) && !visitedLinks.Contains(newLink))
                                                {
                                                    addToQueue(newLink);
                                                }
                                            }
                                        }
                                        checkCmdQueue();
                                    }

                                }
                                else if (!visitedLinks.Contains(link) && response.StatusCode != HttpStatusCode.OK)
                                {
                                    addToErrorTable(link, title);
                                    visitedLinks.Add(link);
                                }
                            }
                            catch (Exception e)
                            {
                                string errmsg = e.Message;
                                addToErrorTable(link, errmsg);
                                visitedLinks.Add(link);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //string errmsg = e.Message;
                //addToErrorTable(link, errmsg);
                visitedLinks.Add(link);
            }
            return "parsed HTML";
        }


        //pre:  take a link as string
        //post: parse the XML according to the specs
        private string parseXml(string link) {
            updateWorkerState("Loading");
            Uri linkUri = new Uri(link);
            if (linkUri.Host.Contains("cnn.com"))
            {
                XElement xml = XElement.Load(link);
                XName sitemap = XName.Get("sitemap", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName url = XName.Get("url", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName loc = XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName date = XName.Get("lastmod", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName date2 = XName.Get("publication_date", "http://www.sitemaps.org/schemas/sitemap/0.9");
                DateTime cutoffTime = new DateTime(2016, 3, 1);

                var top = xml.Elements(url);
                if (xml.Elements(url).Count() == 0)
                {
                    top = xml.Elements(sitemap);
                }

                var next = xml.Elements(date);
                if (xml.Elements(date).Count() == 0)
                {
                    next = xml.Elements(date2);
                }

                foreach (var smElement in top)
                {
                    checkCmdQueue();
                    DateTime dateOfLink = DateTime.Now;
                    if (smElement.Element(date) != null)
                    {
                        dateOfLink = Convert.ToDateTime(smElement.Element(date).Value);
                    }
                    else if (smElement.Element(date2) != null)
                    {
                        dateOfLink = Convert.ToDateTime(smElement.Element(date2).Value);
                    }
                    if (dateOfLink.CompareTo(cutoffTime) >= 0)
                    {
                        var element = smElement.Element(loc).Value;
                        if (!visitedLinks.Contains(link)) {
                            addToQueue(element);
                        }
                    }
                }
            }
            else if (linkUri.Host.Contains("bleacherreport.com"))
            {
                XElement xml = XElement.Load(link);
                XName url = XName.Get("url", "http://www.google.com/schemas/sitemap/0.9");
                XName loc = XName.Get("loc", "http://www.google.com/schemas/sitemap/0.9");
                var top = xml.Elements(url);
                foreach (var smElement in top)
                {
                    checkCmdQueue();
                    var element = smElement.Element(loc).Value;
                    if (element.ToLower().EndsWith(".xml") && !visitedLinks.Contains(link))
                    {
                        addToQueue(element);
                    }
                    else {
                        if (nbaXML(element) && !visitedLinks.Contains(link))
                        {
                            addToQueue(element);
                        }
                    }
                }
            }
            visitedLinks.Add(link);
            return "parsed XML";
        }

        //pre:  take a link to robots.txt as string
        //post: parse according to specs,
        //      don't parse if it is in the disallowed condition
        private string parseRobot(string robotLink)
        {
            updateWorkerState("Loading");
            List<string> disallow = new List<string>();
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(robotLink);
            StreamReader reader = new StreamReader(stream);
            Uri root = new Uri(robotLink);
            while (!reader.EndOfStream)
            {
                checkCmdQueue();
                string line = reader.ReadLine();
                if (line.Contains("Sitemap"))
                {
                    string[] link = line.Split(' ');
                    string xml = link[1];
                    addToQueue(xml);
                }
                else if (line.Contains("Disallow"))
                {
                    string[] notAllow = line.Split(' ');
                    disallow.Add(notAllow[1]);
                }
            }
            if (disallowList.ContainsKey(root.Host)) {
                List<string> keyContent = disallowList[root.Host];
                foreach (string item in keyContent) {
                    disallow.Add(item);
                }
            }
            disallowList.Add(root.Host, disallow);
            visitedLinks.Add(robotLink);
            return "done with " + root.Host + " robots.txt";
        }


        //pre:  take a link as string
        //post: adds the link to the crawl queue to be crawled
        private string addToQueue(string url)
        {
            CloudQueue queue = getQueue();
            //add queue for worker to receiver
            string msg = url;
            CloudQueueMessage message = new CloudQueueMessage(msg);
            queue.AddMessageAsync(message);
            //sw.WriteLine(msg);
            updateTotalUrls();
            return "add to queue";
        }


        private string[] splitTitle(string pageTitle)
        {
            string lowercaseTitle = pageTitle.Trim().ToLower();
            string validChar = Regex.Replace(lowercaseTitle, "[^0-9a-zA-Z ]+", "");
            string[] keyTitles = validChar.Split(' ');
            return keyTitles;
        }

        //pre:  take a link as string and a page title as string
        //post: add a Page object to index table with that url and title
        private string addToTable(string url, string pageTitle) {
            CloudTable table = getTable();
            //add this one link to table
            string[] keyTitles = splitTitle(pageTitle);
            for (int i = 0; i < keyTitles.Length; i++)
            {
                string key = keyTitles[i].Trim();
                if (key != "") {
                    if (key == "cnncom" || key == "cnnpoliticscom") {
                        key = "cnn";
                    }
                    Page newEntity = new Page(url, pageTitle, key);
                    TableOperation insertOperation = TableOperation.Insert(newEntity);
                    table.Execute(insertOperation);
                }
            }
            addToDashboardTable(url, pageTitle);
            updateLast10Links(url);
            updateTableSize();
            return "add to table";
        }

        private string addToDashboardTable(string url, string pageTitle)
        {
            CloudTable table = dashboardTable();
            Page newEntity = new Page(url, pageTitle, "titleDashboard");
            TableOperation insertOperation = TableOperation.Insert(newEntity);
            table.Execute(insertOperation);
            return "add to dashboard table";
        }

        //pre:  take a link as string and a page title as string
        //post: add a Page object to the error table with that url and title
        private string addToErrorTable(string url, string pageTitle)
        {
            CloudTable table = errorTable();
            //add this one link to table
            Page newEntity = new Page(url, pageTitle, "error123456");
            TableOperation insertOperation = TableOperation.Insert(newEntity);
            table.Execute(insertOperation);
            updateErrorLinks(url);
            return "add to error table";
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }


        //Helper methods to retrieve the queue and table
        private CloudQueue getQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("crawlqueue");
            queue.CreateIfNotExists();
            return queue;
        }

        private CloudQueue getCommandQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("commandqueue");
            queue.CreateIfNotExists();
            return queue;
        }

        private CloudTable getTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("crawltable");
            table.CreateIfNotExists();
            return table;
        }

        private CloudTable statTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("stattable");
            table.CreateIfNotExists();
            return table;
        }

        private CloudTable errorTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("errortable");
            table.CreateIfNotExists();
            return table;
        }

        private CloudTable dashboardTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("dashboardtable");
            table.CreateIfNotExists();
            return table;
        }

  
    }
}
