using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public class TrieNode
    {
        public char data;
        public bool EOF;
        public Dictionary<char, TrieNode> dict;

        public TrieNode(char letter, bool eof)
        {
            this.data = letter;
            this.EOF = eof;
            this.dict = new Dictionary<char, TrieNode>();
        }

        //might want another constructor 
        //find the C# version of calling the constructor
    }
}

