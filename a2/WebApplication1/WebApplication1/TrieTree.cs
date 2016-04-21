﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication1
{
    public class TrieTree {

        //private Scanner console; //needs something to scan the file with 
        public TrieNode overallRoot;

        public TrieTree() {
            this.overallRoot = new TrieNode('.', false); //what to put for the first node
            //creates a blank TrieNode 
            //console = new Scanner(System.in);
        }


        public void addTitle(string title) {
            TrieNode temp = overallRoot;
            for (int i = 0; i < title.Length; i++) {
                char key = title[i];
                TrieNode valueNode;
                if (temp.dict.ContainsKey(key)) {
                    //access the node in the value, and keep constructing the tree
                    temp = temp.dict[key];  //points to the node in the dict of that key
                } else {
                    //if the character doesn't exist, create a new key of this character
                    
                    if (i == (title.Length-1)) { //last character in the title
                        valueNode = new TrieNode(key, true);
                        //creates an EOF
                    } else {
                        valueNode = new TrieNode(key, false);
                    }
                    temp.dict.Add(key, valueNode);
                    temp = valueNode;
                }
            }
        }


        //return a list/set of string
        public void searchForPrefix() {
        }
        
       

    }   
}