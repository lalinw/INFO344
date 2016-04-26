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

        public TrieNode(char letter, bool eof) {
            this.data = letter;
            this.EOF = eof;
            this.dict = new Dictionary<char, TrieNode>();
        }

    }
}