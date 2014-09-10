using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whereis
{
    class Program
    {
        string filename; // what to search for.
        string variable = "PATH"; // the default environment variable to search.

        static void Main(string[] args)
        {
            Program p = new Program();
            if (p.ParseCommandLine(args))
            {
                p.Run();
            }
            else
            {
                PrintUsage();
            }
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
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        case "v":
                        case "var":
                            if (i + 1 < n){
                                this.variable = args[++i];
                            }
                            break;
                        default:
                            Console.WriteLine("### Error: unexpected command line argument: " + arg);
                            return false;
                    }
                }
                else if (filename == null)
                {
                    filename = arg;
                }
                else
                {
                    Console.WriteLine("### Error: too many arguments");
                    return false;
                }
            }
            if (filename == null)
            {
                Console.WriteLine("### Error: missing filename argument");
                return false;
            }
            return true;
        }

        private static void PrintUsage()
        {

                Console.WriteLine("Usage: whereis [options] <filename>");                
                Console.WriteLine("Searches your PATH for the given file");
                Console.WriteLine("Options:");
                Console.WriteLine("   var=other    specifies a different environment variable besides the default PATH variable");
        }

        private void Run()
        {
            bool found = false;
            string file = this.filename;
            string path = Environment.GetEnvironmentVariable(this.variable);
            string[] parts = path.Split(';');
            foreach (string p in parts)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    string fullPath = Path.Combine(p, file);
                    if (File.Exists(fullPath) )
                    {
                        Console.WriteLine(fullPath);
                        found = true;
                    }
                }
            }
            if (!found)
            {
                Console.WriteLine("not found");
            }
        }
    }
}
