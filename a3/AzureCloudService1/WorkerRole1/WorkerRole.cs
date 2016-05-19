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

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private static HashSet<string> visitedLinks = new HashSet<string>(); //store visited Links
        private static Dictionary<string, List<string>> disallowList = new Dictionary<string, List<string>>();
        //private StreamWriter sw = new StreamWriter("C:\\Users\\iGuest\\Desktop\\inqueue.txt");

        public static List<string> lastTen = new List<string> { };
        public static List<string> tenErrors = new List<string> { };
        public static int tableSize = 0;
        public static int totalUrls = 0;
        public static string workerState = "Idling";

        public override void Run()
        {
            
            CloudQueue queue = getQueue();
            CloudTable table = getTable();
            CloudTable stat = statTable();
            CloudQueue cmdQueue = getCommandQueue();
            bool crawlYes = false;

            //initialize the statTable
            Stats startStat = new Stats(tableSize, workerState, "", "", totalUrls);
            TableOperation initializeStats = TableOperation.InsertOrReplace(startStat);
            stat.Execute(initializeStats);

            while (true)
            {

                //check for command
                CloudQueueMessage nextCmd = cmdQueue.GetMessage();
                if (nextCmd != null)
                {
                    string cmdString = nextCmd.AsString;
                    if (cmdString.Equals("run"))
                    {
                        //start/resume crawling
                        crawlYes = true;
                    }
                    cmdQueue.DeleteMessage(nextCmd);
                    updateWorkerState("Loading");
                }

                while (crawlYes)
                {

                    //check for command
                    nextCmd = cmdQueue.GetMessage();
                    if (nextCmd != null)
                    {
                        string cmdString = nextCmd.AsString;
                        if (cmdString.Equals("stop"))
                        {
                            //stop crawling
                            crawlYes = false;
                        }
                        cmdQueue.DeleteMessage(nextCmd);
                        updateWorkerState("Stopped");
                    }

                    //read the queue msg and parse
                    CloudQueueMessage retrievedMessage = queue.GetMessage();
                    if (retrievedMessage != null)
                    {
                        string link = retrievedMessage.AsString;
                        if (link.EndsWith("robots.txt")) {
                            parseRobot(link);
                        }
                        else if (link.EndsWith(".xml"))
                        {
                            parseXml(link);
                            updateWorkerState("Crawling");
                        }
                        else
                        //should account for ones that does not end with '/'
                        //what if link ends with index.html or ends with a '/' (will have double slash)
                        {
                            parseHtml(link);
                        }

                        //delete when done
                        queue.DeleteMessage(retrievedMessage);
                    }
                    else
                    {
                        updateWorkerState("Idling");
                        Thread.Sleep(50);
                        //if queue is empty, let the worker role sleep
                    }
                }
            }
        }

        

        private string updateTableSize(int x)
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


        private string updateTotalUrls(int x)
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
            string recentLinks = listToCommaString(lastTen);
            retrieved.lastCrawled = recentLinks;
            TableOperation upsertOperation = TableOperation.InsertOrReplace(retrieved);
            table.Execute(upsertOperation);

            return "update most recent links crawled";
        }

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
            string recentLinks = listToCommaString(tenErrors);
            retrieved.tenErrors = recentLinks;
            TableOperation upsertOperation = TableOperation.InsertOrReplace(retrieved);
            table.Execute(upsertOperation);

            return "update last 10 error links";
        }

        private string listToCommaString(List<string> list) {
            string commaString = "";
            if (list.Count > 0)
            {
                commaString = commaString + list[0];
                for (int i = 1; i < list.Count - 1; i++)
                {
                    commaString = commaString + "," + list[i];
                }
            }
            return commaString;
        }


        private bool htmlOkayToAdd(string link) {

            Uri linkUri = new Uri(link);
            //check for disallow paths 
            if (linkUri.Host.EndsWith("cnn.com") && disallowList["www.cnn.com"] != null)
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
            else if (linkUri.Host.EndsWith("bleacherreport.com"))
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
                return false;
            }
        }

        //checks if the link from bleacherreport is nba-related
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

        private string parseHtml(string link) {
            //remove disallowed ones
            //remove already visited ones 
            //put valid links in queue

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(link);
            string title = doc.DocumentNode.SelectSingleNode("//head/title").InnerHtml;

            if (!title.Equals(""))
            {
                Uri linkUri = new Uri(link);

                bool validRoot = htmlOkayToAdd(link);

                if (validRoot)
                {
                    var linkEnding = linkUri.Segments[linkUri.Segments.Length - 1];


                    //cleaning the link, so it's easier to check for duplicates
                    if (linkEnding.ToLower().Contains("index.html"))
                    {
                        link = linkUri.Scheme + "://" + linkUri.Host + linkUri.AbsolutePath;
                    }
                    else if (linkEnding.ToLower().Contains("index.htm"))
                    {
                        link = linkUri.Scheme + "://" + linkUri.Host + linkUri.AbsolutePath;
                    }
                    else if (!linkUri.Query.Equals(""))
                    {
                        link = linkUri.Scheme + "://" + linkUri.Host + linkUri.AbsolutePath + "/";
                    }
                    else if (!linkEnding.Contains(".") && !linkEnding.Contains("/"))
                    {
                        link = linkUri.Scheme + "://" + linkUri.Host + linkUri.AbsolutePath + "/";
                    }

                    if (Uri.IsWellFormedUriString(link, UriKind.Absolute))
                    {
                        //check if the cleaned link is a "good" link or not
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
                            }
                            
                        }
                        else if (!visitedLinks.Contains(link) && response.StatusCode != HttpStatusCode.OK)
                        {
                            addToErrorTable(link, title);
                            visitedLinks.Add(link);
                        }
                    }
                }
            }
            
            return "parsed HTML";
        }

        private string parseXml(string link) {
            Uri linkUri = new Uri(link);

            if (linkUri.Host.EndsWith("cnn.com"))
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
            else if (linkUri.Host.EndsWith("bleacherreport.com"))
            {
                XElement xml = XElement.Load(link);
                XName url = XName.Get("url", "http://www.google.com/schemas/sitemap/0.9");
                XName loc = XName.Get("loc", "http://www.google.com/schemas/sitemap/0.9");
                var top = xml.Elements(url);
                foreach (var smElement in top)
                {
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
            return "parsed XML";
        }

        private string parseRobot(string robotLink)
        {
            List<string> disallow = new List<string>();
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(robotLink);
            StreamReader reader = new StreamReader(stream);
            Uri root = new Uri(robotLink);
            while (!reader.EndOfStream)
            {
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
            return "done with " + root.Host + " robots.txt";
        }

        private string addToQueue(string url)
        {
            CloudQueue queue = getQueue();
            //add queue for worker to receiver
            string msg = url;
            CloudQueueMessage message = new CloudQueueMessage(msg);
            queue.AddMessageAsync(message);
            //sw.WriteLine(msg);
            updateTotalUrls(1);
            return "add to queue";
        }

        //add a Page object to table
        private string addToTable(string url, string pageTitle) {
            CloudTable table = getTable();
            //add this one link to table
            Uri root = new Uri(url);
            Page newEntity = new Page(url, pageTitle);
            TableOperation insertOperation = TableOperation.Insert(newEntity);
            table.Execute(insertOperation);
            updateTableSize(1);
            updateLast10Links(url);
            return "add to table";
        }

        private string addToErrorTable(string url, string pageTitle)
        {
            CloudTable table = errorTable();
            //add this one link to table
            Uri root = new Uri(url);
            Page newEntity = new Page(url, pageTitle);
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
        
        
        
        //queues & tables
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
    }
}
