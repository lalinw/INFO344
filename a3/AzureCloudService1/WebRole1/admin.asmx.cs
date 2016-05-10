using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
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
        private HashSet<string> visitedLinks = new HashSet<string>(); //store visited Links
        private Dictionary<string, List<string>> disallowList;
        

        [WebMethod]
        public string startCrawling()
        {
            string cnnRobot = "http://www.cnn.com/robots.txt";
            string bleacherRobot = "http://bleacherreport.com/robots.txt";

            parseRobot(cnnRobot, "cnn");
            parseRobot(bleacherRobot, "bleacher");

            return "crawling started";
        }

        private string parseRobot(string robotLink, string name) {
            List<string> disallow = new List<string>();
            using (StreamReader reader = new StreamReader(robotLink))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("SiteMap"))
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
            }
            disallowList.Add(name, disallow);
            return "done with robots.txt";
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
            return "index cleared";
        }
        [WebMethod]
        public string getPageTitle()
        {
            return "title";
        }


        [WebMethod]
        public string getStats()
        {
            //returns stats to show on dashboard
            //JSON??
            //worker state (loading/crawling/idle)
            //cpu utilization, ram
            //urls crawled, last 10 crawled
            //size of queue, size of index(table of crawled data)
            //errors and their urls 
            return "Hello World";
        }






        //queues & tables
        private CloudQueue getQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("crawlQueue");
            queue.CreateIfNotExists();
            return queue;
        }

        private CloudTable getTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("crawlTable");
            table.CreateIfNotExists();
            return table;
        }
        private CloudQueue getCommandQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("commandQueue");
            queue.CreateIfNotExists();
            return queue;
        }
        
        
    }
}
