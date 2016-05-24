using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class TrieNode {
        public char data;
        public bool EOF;
        public Dictionary<char, TrieNode> dict;

        //  creates a TrieNode
        //pre:  takes a char as the data and 
        //      a boolean to indicate if there's a word that ends here
        public TrieNode(char letter, bool eof) {
            this.data = letter;
            this.EOF = eof;
            this.dict = new Dictionary<char, TrieNode>();
        }

    }
}