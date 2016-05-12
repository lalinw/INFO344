using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WorkerRole1
{
    public class Page : TableEntity
    {
        //private DateTime datetime {get; set;}
        private string title { get; set; }
        private string url { get; set; }

        //parameterless constructor
        public Page() { }

        public Page(string root, string url, string pageTitle)
        {
            this.PartitionKey = root;
            this.RowKey = this.RowKey = Guid.NewGuid().ToString();
            this.url = url;
            //this.datetime = datetime;
            this.title = pageTitle;
        }
    }
}