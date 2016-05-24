using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class Stats : TableEntity
    {
        //A class for keeping the stats of the application, including
        //the table size, worker state, last 10 links crawled, last 10 error urls
        //and the number of total urls founded

        public int tableSize { get; set; }
        public string workerState { get; set; }
        public string lastCrawled { get; set; }
        public string tenErrors { get; set; }
        public int totalUrls { get; set; }

        //parameterless constructor
        public Stats() { }

        public Stats(int tableSize, string workerState, string lastTen, string errors, int total)
        {
            this.PartitionKey = "stats";
            this.RowKey = "this";
            this.tableSize = tableSize;
            this.workerState = workerState;
            this.lastCrawled = lastTen;
            this.tenErrors = errors;
            this.totalUrls = total;
        }


    }
}
