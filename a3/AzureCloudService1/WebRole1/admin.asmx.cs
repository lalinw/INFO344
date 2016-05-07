using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Services;

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
            return "Hello World";
        }

        [WebMethod]
        public string stopCrawling()
        {
            return "Hello World";
        }
        [WebMethod]
        public string clearIndex()
        {
            return "Hello World";
        }
        [WebMethod]
        public string getPageTitle()
        {
            return "Hello World";
        }

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
    }
}
