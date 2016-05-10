using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WorkerRole1
{
    public class Page : TableEntity
    {
        private DateTime datetime {get; set;}
        private string title { get; set; }

        //parameterless constructor
        public Page() { }

        public Page(string url, string pageTitle, DateTime datetime) {
            this.PartitionKey = url;
            this.RowKey = this.RowKey = Guid.NewGuid().ToString();
            this.datetime = datetime;
            this.title = pageTitle;
        }
    }
}