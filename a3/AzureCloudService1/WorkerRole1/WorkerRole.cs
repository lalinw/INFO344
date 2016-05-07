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
            while (true)
            { //keep checking the queue, always
                if (queue.ApproximateMessageCount > 0)
                {
                    //read the queue msg and parse
                    CloudQueueMessage retrievedMessage = queue.GetMessage();
                    string msg = retrievedMessage.AsString;
                    string[] numbers = msg.Split(' ');
                    int sum = Convert.ToInt32(numbers[0]) + Convert.ToInt32(numbers[1]) + Convert.ToInt32(numbers[2]);
                    string queueId = retrievedMessage.Id;


                    Thread.Sleep(2000);

                    //add to table
                    Sum newEntity = new Sum(queueId, sum);
                    TableOperation insertOperation = TableOperation.Insert(newEntity);
                    table.Execute(insertOperation);

                    //delete when done
                    queue.DeleteMessage(retrievedMessage);
                }

                Thread.Sleep(3000);  //give the CPU a downtime, so it can do other stuff and not keep checking the queue all the time 
            }
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
