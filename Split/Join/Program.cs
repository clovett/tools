using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Split
{
    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("Usage: join characters [filename]");
            Console.WriteLine("Joins all lines in the given input using the characters provided in first argument");
        }

        static int Main(string[] args)
        {
            string join = null;
            string fileName = null;
            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                { 
                    switch (arg.Trim('-').ToLowerInvariant())
                    {
                        default:
                            PrintUsage();
                            return 1;
                    }
                } 
                else if (join == null)
                {
                    join = arg;
                }
                else if (fileName == null)
                {
                    fileName = arg;
                }
                else
                {
                    Console.WriteLine("Error: too many arguments");
                    PrintUsage();
                }
            }

            if (join == null)
            {
                PrintUsage();
                return 1;
            }
            TextReader reader = null;
            if (fileName == null)
            {
                reader = Console.In;
            }
            else
            {
                reader = new StreamReader(args[1]);
            }
            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    break; // eof
                }
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Console.Write(line);
                    Console.Write(join);
                }
            }
            Console.WriteLine();

            return 0;
        }
    }
}
