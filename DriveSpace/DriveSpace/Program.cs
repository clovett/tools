using System.Diagnostics;
using System.Threading.Channels;

namespace DriveSpace
{
    class Program
    {
        List<string> folders = new List<string>();

        static void PrintUsage()
        {
            Console.WriteLine("Usage: drivespace <folder>");
            Console.WriteLine("Scans the folder and prints report on where the space is going");
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-')
                {
                    switch (arg.Trim('-').ToLowerInvariant())
                    {
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        default:
                            break;
                    }
                }
                folders.Add(args[i]);
            }
            if (folders.Count == 0)
            {
                Console.WriteLine( "### error: missing folder name");
                return false;
            }
            return true;
        }

        long GetFileSpace(string folder)
        {
            long total = 0;
            foreach (string file in Directory.GetFiles(folder))
            {
                var info = new FileInfo(file);
                total += info.Length;
            }
            return total;
        }

        long GetSpace(string folder)
        {
            long total = GetFileSpace(folder);
            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                total += GetSpace(subFolder);
            }
            return total;
        }

        string FormatSpace(long bytes)
        {
            if (bytes > 1e9)
            {
                return Math.Round(bytes / 1e9, 2) + " GB";
            }
            if (bytes > 1e6)
            {
                return Math.Round(bytes / 1e6, 2) + " MB";
            }
            if (bytes > 1e3)
            {
                return Math.Round(bytes / 1e3, 2) + " KB";
            }
            return bytes.ToString();
        }

        int GetMaxNameLength(string folder)
        {
            int maxLength = 10;
            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                var name = subFolder.Substring(folder.Length + 1);
                if (name.Length + 1 > maxLength)
                {
                    maxLength = name.Length + 1;
                }
            }
            return maxLength;
        }

        int Run()
        {
            foreach (string folder in folders)
            {
                int indent = GetMaxNameLength(folder);
                Console.WriteLine("=====================================================================");
                Console.WriteLine(folder);
                string prompt = "files";
                string space = new string('_', 2 + indent - prompt.Length);
                Console.WriteLine($"  {prompt}:{space}{FormatSpace(GetFileSpace(folder))}");

                foreach (string subFolder in Directory.GetDirectories(folder))
                {
                    var name = subFolder.Substring(folder.Length + 1);
                    space = new string('_', 2 + indent - name.Length);
                    Console.WriteLine($"  {name}:{space}{FormatSpace(GetSpace(subFolder))}");
                }
            }
            return 0;
        }

        static int Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return 1;
            }

            Stopwatch w = new Stopwatch();
            w.Start();
            var rc = p.Run();
            w.Stop();
            Console.WriteLine($"Scanned folder in {w.Elapsed.TotalSeconds} seconds");
            return rc;
        }
    }
}