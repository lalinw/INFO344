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
            stopCrawling();
            //stop the crawler and then delete
            CloudTable table = getTable();
            table.DeleteIfExists();
            CloudQueue queue = getQueue();
            queue.DeleteIfExists();
            //reset the crawler stats
            resetTableSize();
            resetTotalUrls();
            return "table and queue and stats cleared";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getPageTitle(string url)
        {
            string rowKey = createMD5(url);
            CloudTable table = getTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<Page>("title", rowKey);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            Page results = (Page)retrievedResult.Result;
            return new JavaScriptSerializer().Serialize(results.title);
        }
        //search implementation

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

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getStats()
        {
           
            //cpu utilization, ram
            PerformanceCounter ramAvailable = new PerformanceCounter("Memory", "Available MBytes");
            var ramFree = ramAvailable.NextValue() + "MB";

            
            PerformanceCounter cpuCounter;
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time");
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            var cpuUsed = cpuCounter.NextValue() + "%";

           

            //urls crawled, last 10 crawled
            CloudTable stat = statTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<Stats>("stats", "this");
            TableResult retrievedResult = stat.Execute(retrieveOperation);
            Stats results = (Stats)retrievedResult.Result;
            string tableSize = "" + results.tableSize;
            string workerState = results.workerState;
            string totalurls = "" + results.totalUrls;
            string lastCrawled = results.lastCrawled;
            string errors = results.tenErrors;

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
            
            return new JavaScriptSerializer().Serialize(stats); 
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

    }
}
