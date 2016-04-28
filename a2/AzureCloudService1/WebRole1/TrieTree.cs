using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class TrieTree {

        public TrieNode overallRoot { get; private set; }

        //initializes the tree and the root
        public TrieTree() {
            this.overallRoot = new TrieNode('.', false);
            //creates a blank TrieNode as the root
        }

        //  adds titles to the Trie tree
        //pre:  takea a title as string
        //post: modifies the tree to hold the data for this title
        public void addTitle(string title)
        {
            title = title.ToLower();
            TrieNode temp = overallRoot;
            for (int i = 0; i < title.Length; i++)
            {
                char key = title[i];    //stores a character
                if (key.Equals('_')) {
                    key = ' ';  //stores a space instead of "_"
                }
                TrieNode valueNode;
                if (temp.dict.ContainsKey(key))
                { 
                    //access the node in the value, and keep constructing the tree
                    temp = temp.dict[key];  
                }
                else
                {
                    //if the character doesn't exist, create a new key of this character
                    if (i == (title.Length - 1))
                    {
                        //creates an EOF (last character in the title)
                        valueNode = new TrieNode(key, true);
                    }
                    else
                    {
                        valueNode = new TrieNode(key, false);
                    }
                    temp.dict.Add(key, valueNode);
                    temp = valueNode;
                }
            }
        }


    }   
}