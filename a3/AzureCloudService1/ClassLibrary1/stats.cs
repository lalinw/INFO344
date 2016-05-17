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
        public int tableSize { get; set; }
        public string workerState { get; set; }
        public List<string> lastCrawled { get; set; }

        //parameterless constructor
        public Stats() { }

        public Stats(int tableSize, string workerState, List<string> lastTen)
        {
            this.PartitionKey = "stats";
            this.RowKey = "this";
            this.tableSize = tableSize;
            this.workerState = workerState;
            this.lastCrawled = lastTen;
        }



    }
}
