using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            CloudQueue queue = getQueue();
            CloudTable table = getTable();
            CloudQueue cmdQueue = getCommandQueue();
            bool crawlYes = true;
            while (true)
            {
                //check for command
                if (cmdQueue != null) {
                    CloudQueueMessage cmd = cmdQueue.GetMessage();
                    string cmdString = cmd.AsString;
                    if (cmdString.Equals("run")) {
                        //start/resume crawling
                        crawlYes = true;
                    }
                }

                while (crawlYes)
                {
                    //check for command
                    if (cmdQueue != null)
                    {
                        CloudQueueMessage cmd = cmdQueue.GetMessage();
                        string cmdString = cmd.AsString;
                        if (cmdString.Equals("stop"))
                        {
                            //stop crawling
                            crawlYes = false;
                        }
                    }

                    
                    if (queue != null)
                    {
                        //read the queue msg and parse
                        CloudQueueMessage retrievedMessage = queue.GetMessage();
                        string msg = retrievedMessage.AsString;

                        if (robots.txt)
                        {
                            //don't know if we'll encounter any more robots.txt
                        }
                        else if (xml)
                        {

                        }
                        else if (html)
                        {

                        }
                        else {
                            //don't add it to the queue
                        }


                        //remove disallowed ones
                        //remove already visited ones 
                        //put valid links in queue



                        
                        //delete when done
                        queue.DeleteMessage(retrievedMessage);
                    }
                    else
                    {
                        Thread.Sleep(50);  
                        //if queue is empty, let the worker role sleep
                    }
                }
            }
        }

        //add a Page object to table
        private string addToTable() {
            //add this one link to table
            Page newEntity = new Page(url, pageTitle, datetime);
            TableOperation insertOperation = TableOperation.Insert(newEntity);
            table.Execute(insertOperation);

            return "add table";
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
            CloudQueue queue = queueClient.GetQueueReference("crawlQueue");
            queue.CreateIfNotExists();
            return queue;
        }

        private CloudQueue getCommandQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("commandQueue");
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
