using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace WebApplication1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class getQuerySuggestions : System.Web.Services.WebService
    {

        [WebMethod]
        public void downloadTitles() {
            //download titles.txt to blob
        }

        [WebMethod]
        public void buildTrie() {
            //build 

            //add the check for memory
            while (file.hasNextLine()) {
                string line = file.nextLine();
                addTitle(line);

            }
        }   


        [WebMethod]
        public void searchTrie() {

        }
         