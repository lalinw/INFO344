using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using System.Web;

namespace ClassLibrary1
{
    public class Page : TableEntity
    {
        //a class to store an HTML page and its title
        public string title { get; set; }
        public string url { get; set; }

        //parameterless constructor
        public Page() { }

        public Page(string url, string pageTitle)
        {
            this.PartitionKey = "title";
            this.RowKey = createMD5(url);
            this.url = url;
            this.title = pageTitle;
        }

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

    }
}
