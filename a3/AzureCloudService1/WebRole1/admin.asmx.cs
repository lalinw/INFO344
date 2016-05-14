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
        
        [WebMethod]
        public string startCrawling()
        {
            string cnnRobot = "http://www.cnn.com/robots.txt";
            string bleacherRobot = "http://bleacherreport.com/robots.txt";
            addToQueue(cnnRobot);
            addToQueue(bleacherRobot);
            resumeCrawling();
            return "crawling started";
        }

        //parses robots.txt
        

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
            return "index cleared";
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
            
            //worker state (loading/crawling/idle)

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
            //HOW?!, what data structure to use? 
            //how to communicate between webrole and workerole 

            //size of queue, size of index(table of crawled data)

            CloudQueue queue = getQueue();
            var curQueueSize = queue.ApproximateMessageCount;
            //==insertOrReplace a row in a table


            //errors and their urls
            //WHAT DO YOU MEAN BY ERROR PAGES

            List<string> placeholderList = new List<string>();
            var stats = new WorkerStats
            {
                workerState = "running",
                cpuUsed = cpuUsed,
                ramAvailable = ramFree,
                curQueueSize = (int)curQueueSize,
                tableSize = 3,
                last10Crawled = placeholderList,
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
        
        
    }
}
