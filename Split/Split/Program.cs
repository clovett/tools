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
            bool sort = false;
            bool toLower = false;
            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    switch (arg.Trim('-').ToLowerInvariant())
                    {
                        case "sort":
                            sort = true;
                            break;
                        case "tolower":
                            toLower = true;
                            break;
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
            if (fileName == null)
            {
                reader = Console.In;
            }
            else
            {
                reader = new StreamReader(fileName);
            }
            List<string> output = new List<string>();
            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    break; // eof
                }
                foreach (var part in line.Split(splitChars))
                {
                    output.Add(toLower ? part.ToLowerInvariant() : part);
                }
            }
            if (sort)
            {
                output.Sort();
            }
            output.ForEach(line => Console.WriteLine(line));
            return 0;
        }
    }
}
