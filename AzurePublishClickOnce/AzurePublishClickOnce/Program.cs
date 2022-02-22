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
        string connectionString;
        string target;

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
                Folder sourceFolder = new Folder(source);
                Folder targetFolder = new Folder(target, connectionString);

                if (sourceFolder.HasSubfolder("Application Files"))
                {
                    // trim all but the latest local version.
                    Folder appFiles = sourceFolder.GetSubfolder("Application Files");
                    if (appFiles != null)
                    {
                        List<FileVersion> versions = new List<FileVersion>(appFiles.ChildFolders.Select(it => new FileVersion(it)));
                        versions.Sort();
                        while (versions.Count > 1)
                        {
                            var f = versions[0];
                            versions.RemoveAt(0);
                            appFiles.GetSubfolder(f.name).DeleteSubtree();
                        }
                    }
                }

                sourceFolder.MirrorDirectory(targetFolder, true);
                sourceFolder.MirrorDirectory(targetFolder, false);
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
                foreach (var folder in Directory.GetDirectories(appFiles))
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
                foreach (var folder in toRemove)
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
            Console.WriteLine("Usage: AzurePublishClickOnce SourceFolder TargetFolder connectionString");
            Console.WriteLine("Copies all files and directories from the source directory to the target directory");
            Console.WriteLine("using the given Azure storage connection string.");
            Console.WriteLine("It also cleans up old versions in 'Application Files'");
            Console.WriteLine("versions after the copy to ensure local and remote folders only contain the latest verion.");
            Console.WriteLine();
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
                        default:
                        case "?":
                        case "h":
                        case "help":
                            return false;
                    }
                }
                else if (source == null)
                {
                    source = System.IO.Path.GetFullPath(arg);
                }
                else if (target == null)
                {
                    target = arg;
                    if (!target.Contains('/'))
                    {
                        Console.WriteLine("### Error: target folder should be prefixed with container name, separated by '/' ");
                    }
                }
                else if (connectionString == null)
                {
                    connectionString = arg;
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

            if (connectionString == null)
            {
                Console.WriteLine("### Error: missing blob storage connectionString");
                return false;
            }

            return true;
        }

        class FileVersion : IComparable<FileVersion>
        {
            public string name;
            public Version version;

            public FileVersion(string name)
            {
                string[] parts = name.Split('_');
                if (parts.Length < 2)
                {
                    throw new Exception("Unknown version: " + name);
                }
                this.name = name;
                this.version = new Version(string.Join(".", parts.Skip(1)));
            }

            public int CompareTo(FileVersion other)
            {
                return this.version.CompareTo(other.version);
            }
        }
    }
}
