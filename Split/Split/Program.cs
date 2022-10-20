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
            Console.WriteLine("Usage: split character [filename]");
            Console.WriteLine("Splits all lines in the given input using the characters provided in first argument");
        }

        static int Main(string[] args)
        {
            string split = null;
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
                else if (split == null)
                {
                    split = arg;
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

            if (split == null)
            {
                PrintUsage();
                return 1;
            }

            TextReader reader = null;
            char[] splitChars = split.ToCharArray();
            if (args.Length == 1)
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
                foreach (var part in line.Split(splitChars))
                {
                    Console.WriteLine(part);
                }
            }
            return 0;
        }
    }
}
