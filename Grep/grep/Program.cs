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
        bool echo = false;
        bool lineNumbers = false;
        bool ignoreCase = false;
        bool negate = false;

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
                    if (regex.IsMatch(line) == !this.negate)
                    {
                        var saved = Console.BackgroundColor;
                        if (echo)
                        {
                            Console.BackgroundColor = ConsoleColor.DarkRed;
                        }
                        if (lineNumbers)
                        {
                            Console.WriteLine(linePrefix + "{1}", i, line);
                        }
                        else
                        {
                            Console.WriteLine(line);
                        }
                        Console.BackgroundColor = saved;
                    }
                    else
                    {
                        if (echo)
                        {
                            Console.WriteLine(line);
                        }
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
            Console.WriteLine("  -f filename file to process (if no file uses stdin)");
            Console.WriteLine("  -ln         add line numbers");
            Console.WriteLine("  -e          echo all input, and highlight matching lines");
            Console.WriteLine("  -i          case insensitive");
            Console.WriteLine("  -n          negate, print all lines that don't match the expression");
            Console.WriteLine("  expression  the regular expression to match (in .NET regex syntax)");
        }

        private bool ParseCommandLine(string[] args)
        {
            string expression = null;
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "f":
                            if (i + 1 < n)
                            {
                                if (inputFile != null)
                                {
                                    Console.WriteLine("### Error: file name already provided");
                                    return false;
                                }
                                inputFile = args[++i];
                                if (!File.Exists(inputFile))
                                {
                                    Console.WriteLine("### Error: input file '" + inputFile + "' does not exist");
                                    return false;
                                }
                            }
                            else
                            {
                                Console.WriteLine("### Error: expression after -e argument is missing");
                                return false;
                            }
                            break;
                        case "noln":
                            this.lineNumbers = false;
                            break;
                        case "e":
                            this.echo = true;
                            break;
                        case "i":
                            this.ignoreCase = true;
                            break;
                        case "n":
                            this.negate = true;
                            break;
                        default:
                        case "?":
                        case "h":
                        case "help":
                            PrintUsage();
                            return false;
                    }                    
                }
                else if (expression == null)
                {
                    expression = arg;
                }
                else
                {
                    PrintUsage();
                    return false;
                }
            }

            if (expression == null)
            {
                PrintUsage();
                return false;
            }

            RegexOptions options = RegexOptions.None;
            if (ignoreCase)
            {
                options = RegexOptions.IgnoreCase;
            }
            regex = new Regex(expression, options);

            return true;
        }
    }
}
