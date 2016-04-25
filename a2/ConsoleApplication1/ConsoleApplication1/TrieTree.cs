using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class TrieTree
    {
        //private Scanner console; //needs something to scan the file with 
        public TrieNode overallRoot;

        public TrieTree()
        {
            this.overallRoot = new TrieNode('.', false); //what to put for the first node
            //creates a blank TrieNode 
            //console = new Scanner(System.in);
        }


        public void addTitle(string title)
        {
            TrieNode temp = overallRoot;
            for (int i = 0; i < title.Length; i++)
            {
                char key = title[i];
                TrieNode valueNode;
                if (temp.dict.ContainsKey(key))
                {
                    //access the node in the value, and keep constructing the tree
                    temp = temp.dict[key];  //points to the node in the dict of that key
                }
                else
                {
                    //if the character doesn't exist, create a new key of this character

                    if (i == (title.Length - 1))
                    { //last character in the title
                        valueNode = new TrieNode(key, true);
                        //creates an EOF
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

        //brings the Node pointer to the end of the prefix node 
        public List<string> searchForPrefix(string prefix) {
            TrieNode temp = overallRoot; 
            for (int i = 0; i < prefix.Length; i++) {
                char x = prefix[i];
                if (temp.dict.ContainsKey(x)) {
                    temp = temp.dict[x];
                } else { 
                    //no more nodes to search in the TrieTree
                    if (temp.EOF == true)
                    {
                        return new List<string>() { prefix };
                    }
                    else {
                        return null;
                        //title with the name 'prefix' does not exist && does not have anything else to search
                    }
                }
            }
            return getSuggestions(prefix, temp); 
        }

        public List<string> getSuggestions(string prefix, TrieNode root) {
            List<string> words = new List<string>();
            TrieNode temp = root;
            foreach (char key in temp.dict.Keys.ToArray()) {
                temp.dict[key]
            }
        }

        public string helper(string word, TrieNode root) {
            if (root.EOF == true) {
                return word; 
            } else {
                return helper(word)
            }
        }
    }
}
