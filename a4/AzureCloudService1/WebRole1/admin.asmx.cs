using ClassLibrary1;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Xml.Linq;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class admin : System.Web.Services.WebService
    {
        
        public bool firstRun = true; 
        public static Dictionary<string, Tuple<DateTime, List<Tuple<string, int, string, string>>>> cache;

        //pre:  takes no parameter
        //post: starts the crawler from 2 root sites' robots, CNN and BleacherReport
        [WebMethod]
        public string startCrawling()
        {
            if (firstRun)
            {
                string cnnRobot = "http://www.cnn.com/robots.txt";
                string bleacherRobot = "http://bleacherreport.com/robots.txt";
                addToQueue(cnnRobot);
                addToQueue(bleacherRobot);
                resumeCrawling();
                firstRun = false;
                return "crawling started";
            }
            else {
                resumeCrawling();
                return "resumed";
            }
            
        }


        //pre:  takes a link as a string
        //post: Helper method that adds a link to be crawled to the crawlqueue
        private string addToQueue(string url)
        {
            CloudQueue queue = getQueue();
            //add queue for worker to receiver
            string msg = url;
            CloudQueueMessage message = new CloudQueueMessage(msg);
            queue.AddMessage(message);
            return "success";
        }


        //pre:  takes no parameter
        //post: stops the crawler (sends a request to stop it)
        [WebMethod]
        public string stopCrawling()
        {
            CloudQueue cmd = getCommandQueue();
            cmd.AddMessage(new CloudQueueMessage("stop"));
            return "stopped crawling";
        }


        //pre:  takes no parameter
        //post: resumes the crawler 
        [WebMethod]
        public string resumeCrawling()
        {
            CloudQueue cmd = getCommandQueue();
            cmd.AddMessage(new CloudQueueMessage("run"));
            return "resumed crawling";
        }

        //pre:  takes no parameter
        //post: Clears the index; 
        //      deletes the table, clears the queue and reset the stats
        [WebMethod]
        public string clearIndex()
        {
            stopCrawling();
            //stop the crawler and then delete
            CloudTable table = getTable();
            table.DeleteIfExists();
            CloudTable tableError = errorTable();
            tableError.DeleteIfExists();
            CloudTable tableDashboard = dashboardTable();
            tableDashboard.DeleteIfExists();
            CloudQueue queue = getQueue();
            queue.Clear();
            CloudQueue cmdQueue = getCommandQueue();
            cmdQueue.Clear();
            //reset the crawler stats
            resetTrieWord();
            resetTableSize();
            resetTotalUrls();
            return "table/error table and queue and stats cleared";
        }

        //pre:  takes an HTML link as a string
        //post: search for the title of the html page and return as a string
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getPageTitle(string url)
        {
            string rowKey = createMD5(url);
            CloudTable table = dashboardTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<Page>("titleDashboard", rowKey);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            Page results = (Page)retrievedResult.Result;
            string title;
            if (results == null)
            {
                title = "No results to display";
            }
            else {
                title = results.title;
            }
            return new JavaScriptSerializer().Serialize(title);
        }


        //I don't know what I'm doing FML
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getSearchResults(string input)
        {
            if (cache == null)
            {
                cache = new Dictionary<string, Tuple<DateTime, List<Tuple<string, int, string, string>>>>();
            }

            if (cache.ContainsKey(input) && cache[input].Item1.AddMinutes(20) > DateTime.Now) {
                return new JavaScriptSerializer().Serialize(cache[input].Item2); 
            }
            string[] keyTitles = input.Trim().ToLower().Split(' ');
            CloudTable table = getTable();
            List<Page> queryPages = new List<Page>();
            foreach (string keyword in keyTitles) {
                var thisQuery = table.CreateQuery<Page>().Where(e => e.PartitionKey == keyword).ToList();
                queryPages.AddRange(thisQuery);
            }
            var rankedResults = queryPages
                .GroupBy(x => x.RowKey)
                .Select(x => new Tuple<string, int, string, string>(x.Key, x.ToList().Count, x.First().title, x.First().url))
                .OrderByDescending(x => x.Item2)
                .Take(20);

            if (!cache.ContainsKey(input) && rankedResults.ToList().Count > 0)
            {
                cache.Add(input, new Tuple<DateTime, List<Tuple<string, int, string, string>>>(DateTime.Now, rankedResults.ToList()));
            }


            return new JavaScriptSerializer().Serialize(rankedResults);
        }


        //pre:  takes a string
        //post: returns the MD5 hash version of the input
        private static string createMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        //pre:  takes no parameter
        //post: returns a JSON string of the current stats
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getStats()
        {
            //cpu utilization, ram
            PerformanceCounter ramAvailable = new PerformanceCounter("Memory", "Available MBytes");
            var ramFree = "" + ramAvailable.NextValue();

            PerformanceCounter cpuCounter;
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time");
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            var cpuUsed = cpuCounter.NextValue() + "%";
            
            //urls crawled, last 10 crawled, table size, worker state, total urls, errors
            CloudTable stat = statTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<Stats>("stats", "this");
            TableResult retrievedResult = stat.Execute(retrieveOperation);
            Stats results = (Stats)retrievedResult.Result;
            string tableSize;
            string workerState;
            string totalurls;
            string lastCrawled;
            string errors;

            try
            {
                tableSize = "" + results.tableSize;
                workerState = results.workerState;
                totalurls = "" + results.totalUrls;
                lastCrawled = results.lastCrawled;
                errors = results.tenErrors;
            }
            catch {
                tableSize = "0";
                workerState = "Idling";
                totalurls = "0";
                lastCrawled = "";
                errors = "";
            }

            TableOperation retrieveOperation2 = TableOperation.Retrieve<Stats>("trie", "this");
            TableResult retrievedResult2 = stat.Execute(retrieveOperation2);
            Stats results2 = (Stats)retrievedResult2.Result;
            string trieword;
            string triesize;

            try
            {
                triesize = "" + results2.tableSize;
                trieword = results2.workerState;
            }
            catch {
                triesize = "0";
                trieword = "Trie is not yet built";
            }

            //size of queue, size of index(table of crawled data)
            CloudQueue queue = getQueue();
            queue.FetchAttributes();
            string curQueueSize = "" + queue.ApproximateMessageCount;

            List<string> stats = new List<string>();
            stats.Add(workerState);
            stats.Add(cpuUsed);
            stats.Add(ramFree);
            stats.Add(curQueueSize);
            stats.Add(tableSize);
            stats.Add(totalurls);
            stats.Add(lastCrawled);
            stats.Add(errors);
            stats.Add(triesize);
            stats.Add(trieword);
            return new JavaScriptSerializer().Serialize(stats); 
        }

        //post: resets the table size
        private string resetTableSize()
        {
            CloudTable table = statTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<Stats>("stats", "this");
            TableResult retrievedResult = table.Execute(retrieveOperation);
            Stats updateEntity = (Stats)retrievedResult.Result;
            //update the column
            updateEntity.tableSize = 0;
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateEntity);
            table.Execute(insertOrReplaceOperation);
            return "update tableSize";
        }

        private string resetTrieWord()
        {
            CloudTable table = statTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<Stats>("trie", "this");
            TableResult retrievedResult = table.Execute(retrieveOperation);
            Stats updateEntity = (Stats)retrievedResult.Result;
            //update the column
            updateEntity.tableSize = 0;
            updateEntity.workerState = "Trie is not yet built";
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateEntity);
            table.Execute(insertOrReplaceOperation);
            return "update Trie";
        }

        //post: resets the number of total urls found
        private string resetTotalUrls()
        {
            CloudTable table = statTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<Stats>("stats", "this");
            TableResult retrievedResult = table.Execute(retrieveOperation);
            Stats updateEntity = (Stats)retrievedResult.Result;
            //update the column
            updateEntity.totalUrls = 0;
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateEntity);
            table.Execute(insertOrReplaceOperation);
            return "update total urls";
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

        private CloudTable getTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("crawltable");
            table.CreateIfNotExists();
            return table;
        }
        private CloudQueue getCommandQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("commandqueue");
            queue.CreateIfNotExists();
            return queue;
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
