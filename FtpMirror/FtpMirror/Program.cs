using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Walkabout.Utilities;

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
                Folder sourceFolder = new Folder(source, userName, password);
                Folder targetFolder = new Folder(target, userName, password);

                sourceFolder.MirrorDirectory(targetFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Error: " + ex.Message);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: FtpMirror SourceDir TargetDir /u userName /p pswd");
            Console.WriteLine("Copies all files and directories from the source directory to the target directory");
            Console.WriteLine("using the given user name and password.  It ensures the TargetDir is an exact copy");
            Console.WriteLine("by deleting any files or directories at the target that do not exist in the source.");
            Console.WriteLine("Either SourceDir or TargetDir can be FTP locaations.");
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
