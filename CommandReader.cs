using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BTree
{
    public class CommandReader
    {
        BTree? bTree;
        public void ReadCommand(string commandLine)
        {
            string[] words = commandLine.Split(' ');

            switch (words[0])
            {
                case "create":
                    if (bTree != null)
                    {
                        Console.WriteLine("You have to close previous BTree");
                    }
                    else
                    {
                        bTree = new BTree(words[1], CreatingMode.Create);
                        Console.WriteLine($"Created Btree named {words[1]}");
                    }
                    break;
                case "open":
                    if (bTree != null)
                    {
                        Console.WriteLine("You have to close previous BTree");
                    }
                    else
                    {
                        bTree = new BTree(words[1], CreatingMode.Open);
                        Console.WriteLine($"Opened Btree named {words[1]}");
                    }
                    break;
                case "close":
                    if (bTree == null)
                    {
                        Console.WriteLine("Btree wasnt open");
                    }
                    else
                    {
                        bTree.EndTapeConnection();
                        Console.WriteLine($"Closed Btree named {bTree.FilePath}");
                        bTree = null;
                    }
                    break;
                case "readfcmm":
                    foreach (string line in File.ReadLines(words[1]))
                    {
                        ReadCommand(line);
                    }
                    break;
                case "add":
                    if (bTree == null)
                    {
                        Console.WriteLine(" Btree is not loaded");
                        break;
                    }
                    if (words[1] == "random")
                    {
                        bTree.AddRandomRecords(int.Parse(words[2]));
                        Console.WriteLine($"Added {words[2]} random records");
                    }
                    else 
                    {
                        if (bTree.AddRecord(int.Parse(words[1]), words[2].ToCharArray()))
                        {
                            Console.WriteLine($"Add record {words[2]} with key {words[1]}");
                        }
                        else
                        {
                            Console.WriteLine("Record with this key is already in the tree");
                        }
                    }
                    break;
                case "delete":
                    if (bTree == null)
                    {
                        Console.WriteLine(" Btree is not loaded");
                        break;
                    }
                    if (bTree.DeleteRecord(int.Parse(words[1])))
                    {
                        Console.WriteLine($"Deleted record with key {words[1]}");
                    }
                    else
                    {
                        Console.WriteLine("Record with that key in not in the tree");
                    }
                    break;
                case "show":
                    if (bTree == null)
                    {
                        Console.WriteLine(" Btree is not loaded");
                        break;
                    }
                    char[]? record = bTree.GetRecord(int.Parse(words[1]));
                    if (record != null)
                    {
                        Console.WriteLine($"Record with key {words[1]} is {new string(record)}");
                    }
                    else
                    {
                        Console.WriteLine("Record with that key in not in the tree");
                    }
                    break;
                case "prtallinf":
                    if (bTree == null)
                    {
                        Console.WriteLine(" Btree is not loaded");
                        break;
                    }
                    bTree.PrintAllInfo();
                    break;
                case "prtall":
                    if (bTree == null)
                    {
                        Console.WriteLine(" Btree is not loaded");
                        break;
                    }
                    bTree.PrintAllInfo();
                    break;
                default:
                    Console.WriteLine("Command unnknown");
                    break;
            }
        }
    }
}
