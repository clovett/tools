using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    class Program
    {
        Regex regex;
        string inputFile;

        static int Main(string[] args)
        {
            Program p = new Program();

            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return 1;
            }

            try
            {
                p.Run();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("### Error: " + e.Message);
            }
            return 1;
        }

        int CountLines(string file)
        {
            TextReader reader = new StreamReader(inputFile);
           
            int count = 1;

            using (reader)
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    
                    line = reader.ReadLine();
                    count++;
                }
            }
            return count;
        }

        void Run()
        {
            string linePrefix = "{0:0000000}: ";
            TextReader reader = Console.In;
            if (!string.IsNullOrEmpty(inputFile))
            {
                int digits = (int)Math.Log10(CountLines(inputFile)) + 1;                
                linePrefix = "{0:" + new string('0', digits) + "}: ";
                reader = new StreamReader(inputFile);
            }

            int i = 1;
            
            using (reader)
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    line = line.Trim();
                    if (regex.IsMatch(line))
                    {
                        Console.WriteLine(linePrefix + "{1}", i, line);                        
                    }
                    line = reader.ReadLine();
                    i++;
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: grep [options] [file]");
            Console.WriteLine("Outputs matching lines from the given file with the expression given in the options");
            Console.WriteLine("Options:");
            Console.WriteLine("  -e expr     the regular expression to match (in .NET regex syntax)");
            Console.WriteLine("  file        the file to process, or standard input if no file provided");
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "e":
                            if (i + 1 < n)
                            {
                                regex = new Regex(args[++i]);
                            }
                            else
                            {
                                Console.WriteLine("### Error: expression after -e argument is missing");
                                return false;
                            }
                            break;
                        default:
                        case "?":
                        case "h":
                        case "help":
                            PrintUsage();
                            return false;
                    }                    
                }
                else if (inputFile == null)
                {
                    inputFile = arg;
                    if (!File.Exists(inputFile))
                    {
                        Console.WriteLine("### Error: input file '" + inputFile + "' does not exist");
                        return false;
                    }
                }
                else
                {
                    PrintUsage();
                    return false;
                }
            }

            if (regex == null)
            {
                PrintUsage();
                return false;
            }

            return true;
        }
    }
}
