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
    // [System.Web.Script.Services.ScriptService]
    public class admin : System.Web.Services.WebService
    {
        
        public bool firstRun = true; 

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


        //helper method, called from startCrawling()
        //adds a url to crawlQueue to crawl
        private string addToQueue(string url)
        {
            CloudQueue queue = getQueue();
            //add queue for worker to receiver
            string msg = url;
            CloudQueueMessage message = new CloudQueueMessage(msg);
            queue.AddMessage(message);
            return "success";
        }

        [WebMethod]
        public string stopCrawling()
        {
            CloudQueue cmd = getCommandQueue();
            cmd.AddMessage(new CloudQueueMessage("stop"));
            return "stopped crawling";
        }

        [WebMethod]
        public string resumeCrawling()
        {
            CloudQueue cmd = getCommandQueue();
            cmd.AddMessage(new CloudQueueMessage("run"));
            return "resumed crawling";
        }

        [WebMethod]
        public string clearIndex()
        {
            CloudTable table = getTable();
            table.DeleteIfExists();
            CloudQueue queue = getQueue();
            queue.DeleteIfExists();
            return "table and queue cleared";
        }

        [WebMethod]
        public string getPageTitle()
        {
            return "title";
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getStats()
        {
            //returns stats to show on dashboard
            //JSON??
            
            //cpu utilization, ram
            PerformanceCounter ramAvailable = new PerformanceCounter("Memory", "Available MBytes");
            var ramFree = ramAvailable.NextValue() + "MB";

            PerformanceCounter cpuCounter;
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time");
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            //cpuCounter.InstanceName = "_Total";
            var cpuUsed = cpuCounter.NextValue() + "%";

            //urls crawled, last 10 crawled
            CloudTable stat = statTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<Stats>("stats", "this");
            TableResult retrievedResult = stat.Execute(retrieveOperation);
            Stats results = (Stats)retrievedResult.Result;
            int tableSize = results.tableSize;
            string workerState = results.workerState;
            List<string> lastCrawled = results.lastCrawled;

            //size of queue, size of index(table of crawled data)
            CloudQueue queue = getQueue();
            queue.FetchAttributes();
            int curQueueSize = (int)queue.ApproximateMessageCount;


            //errors and their urls
            CloudTable error = errorTable();
            TableOperation retrieveOperation2 = TableOperation.Retrieve<Page>("error", "this");
            TableResult retrievedResult2 = error.Execute(retrieveOperation);
            Stats results2 = (Stats)retrievedResult.Result;

            List <string> placeholderList = new List<string>();
            var stats = new WorkerStats
            {
                workerState = workerState,
                cpuUsed = cpuUsed, //in %
                ramAvailable = ramFree,
                curQueueSize = curQueueSize,
                tableSize = tableSize,
                last10Crawled = lastCrawled,
                errors = placeholderList
            };

            return new JavaScriptSerializer().Serialize(stats); 
        }

        //inner class for json info to display worker role stats 
        public class WorkerStats
        {
            public string workerState;
            public string cpuUsed;
            public string ramAvailable;
            public int curQueueSize;
            public int tableSize;
            public List<string> last10Crawled;
            public List<string> errors;
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

     
    }
}
