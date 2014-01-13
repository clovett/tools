using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FtpMirror
{
    class Program
    {
        string source;
        string target;
        string userName;
        string password;

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
            }
            else
            {
                p.Run();
            }
        }

        private void Run()
        {
            try
            {
                Walkabout.Utilities.FtpUtilities.MirrorDirectory(source, target, userName, password);
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Error: " + ex.Message);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: FtpMirror SourceDir TargetDir /u userName /p pswd");
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
                        case "u":
                            if (i + 1 < n)
                            {
                                userName = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("### Error: username after -u argument is missing");
                                return false;
                            }
                            break;
                        case "p":
                            if (i + 1 < n)
                            {
                                password = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("### Error: password after -p argument is missing");
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
                else if (source == null)
                {
                    source = arg;
                    if (!Directory.Exists(source))
                    {
                        Console.WriteLine("### Error: input directory '" + source + "' does not exist");
                        return false;
                    }
                }
                else if (target == null)
                {
                    target = arg;
                }
                else
                {
                    Console.WriteLine("### Error: too many arguments");
                    PrintUsage();
                    return false;
                }
            }

            return true;
        }
    }
}
