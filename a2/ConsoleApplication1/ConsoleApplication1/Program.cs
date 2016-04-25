using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)

            
        {
            TrieTree trie = new TrieTree();
            trie.addTitle("hello");
            trie.addTitle("help");
            trie.addTitle("hey");
            trie.addTitle("yo");
            Console.WriteLine("yah");
        }
    }
}

