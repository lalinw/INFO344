using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using ClassLibrary1;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;




//
//
// HAVE TO CHANGE THE CODE OR STORAGE CONNECTION STRING SO THAT IT WORKS TOGETHER WITH PA3

namespace WebRole1
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class getQuerySuggestions : System.Web.Services.WebService
    {
        public static TrieTree trie = new TrieTree();
        public string filePath = System.IO.Path.GetTempPath() + "\\titles_cleaned.txt";

        //  downloads the titles file from the blob storage to the instance
        //  reads the content to the stream reader 
        //pre:  takes no parameter
        //post: returns a string that indicates the method has terminated
        [WebMethod]
        public string downloadTitles()
        {
            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("helloblob");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("titles_cleaned.txt");
            using (var fileStream = System.IO.File.OpenWrite(filePath))
            {
                blockBlob.DownloadToStream(fileStream);
            }
            return "Download successful!";
        }

        //  builds a trie from an empty trie object
        //  resets the trie before building
        //pre:  takes no parameter
        //post: returns a string that indicates the method has terminated
        [WebMethod]
        public string buildTrie()
        {
            trie = new TrieTree();  //reset trie before building 
            string status = "";
            System.Diagnostics.PerformanceCounter ramAvailable = new PerformanceCounter("Memory", "Available MBytes");
            using (StreamReader file = new StreamReader(filePath))
            {
                int counter = 0;
                string line = file.ReadLine();
                while (line != null)
                {
                    trie.addTitle(line);
                    line = file.ReadLine();
                    counter++;
                    if (counter % 1000 == 0)
                    {
                        if (ramAvailable.NextValue() < 50)
                        {
                            break;
                        }
                    }
                }
                status = "Last word added: " + line + "; Total words: " + counter;  //just a report for the user, remove later
                line = line.Replace("_", " ");
                CloudTable stat = statTable();
                Stats startStat = new Stats("trie", counter, line, "nothing", "nothing", 0);
                TableOperation initializeStats = TableOperation.InsertOrReplace(startStat);
                stat.Execute(initializeStats);
            }
            return status;
        }

        //  search for the words that has the indicated prefixes
        //  returns a list of string of size 10 or less
        //pre:  takes a string as the prefix
        //post: prepare the pointer to call the helper method
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchForPrefix(string input)
        {
            string prefix = input.Trim().ToLower();
            List<string> suggestions = new List<string>();
            if (prefix.Equals(""))
            {
                return new JavaScriptSerializer().Serialize(suggestions);
            }
            TrieNode temp = trie.overallRoot;
            for (int i = 0; i < prefix.Length; i++)
            {
                char x = prefix[i];
                if (temp.dict.ContainsKey(x))
                {
                    temp = temp.dict[x];
                }
                else {
                    //has nothing to suggest, list will be empty; 
                    return new JavaScriptSerializer().Serialize(suggestions);
                }
            }
            //pointer is at the node of the last character            
            //DFS recurse through the trie
            List<string> result = searchHelper("", temp, suggestions);
            return new JavaScriptSerializer().Serialize(result);
        }

        //  helper method for searchForPrefix
        //pre:  takes string of prefix, node pointer, and list to return
        //post: returns a list of string when size is 10 or when there is nothing left to suggest
        public List<string> searchHelper(string prefix, TrieNode curr, List<string> suggestions)
        {
            if (suggestions.Count >= 10)
            {
                return suggestions;
            }
            else
            {
                if (curr.EOF)
                {
                    suggestions.Add(prefix);
                }
                foreach (char nextKey in curr.dict.Keys)
                {
                    suggestions = searchHelper(prefix + curr.dict[nextKey].data, curr.dict[nextKey], suggestions);
                }
                return suggestions;
            }
        }

        private CloudTable statTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("stattable");
            table.CreateIfNotExists();
            return table;
        }

    }
}
