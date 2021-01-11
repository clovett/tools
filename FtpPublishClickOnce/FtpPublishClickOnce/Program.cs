using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Walkabout.Utilities;

namespace FtpPublishClickOnce
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
                CleanLocalFolder(source);
                Folder sourceFolder = new Folder(source, userName, password);
                Folder targetFolder = new Folder(target, userName, password);

                sourceFolder.MirrorDirectory(targetFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine("### Error: " + ex.Message);
            }
        }

        private void CleanLocalFolder(string source)
        {
            var appFiles = Path.Combine(source, "Application Files");
            if (Directory.Exists(appFiles))
            {
                List<string> toRemove = new List<string>();
                Version latest = null;
                string path = null;
                foreach(var folder in Directory.GetDirectories(appFiles))
                {
                    toRemove.Add(folder);
                    Version v = GetVersion(Path.GetFileName(folder));
                    if (latest == null || v > latest)
                    {
                        latest = v;
                        path = folder;
                    }
                }
                toRemove.Remove(path);
                foreach(var folder in toRemove)
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        private Version GetVersion(string folder)
        {
            List<string> parts = new List<string>(folder.Split('_'));
            parts.RemoveAt(0);
            return new Version(string.Join(".", parts));
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: FtpPublishClickOnce SourceDir TargetDir -u userName -p pswd");
            Console.WriteLine("Copies all files and directories from the source directory to the target directory");
            Console.WriteLine("using the given user name and password.  It also cleans up 'Application Files'");
            Console.WriteLine("versions after the copy to ensure local and remote folders only contain the latest.");
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-')
                {
                    switch (arg.TrimStart('-').ToLowerInvariant())
                    {
                        case "u":
                        case "user":
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
                        case "password":
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

            if (source == null)
            {
                Console.WriteLine("### Error: missing source folder");
                return false;
            }

            if (!Directory.Exists(source))
            {
                Console.WriteLine("### Error: source folder does not exist: " + source);
                return false;
            }

            if (target == null)
            {
                Console.WriteLine("### Error: missing target folder");
                return false;
            }

            return true;
        }
    }
}
