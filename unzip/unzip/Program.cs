using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unzip
{
    class Program
    {
        List<string> files = new List<string>();

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "H":
                        case "help":
                        case "?":
                            return false;
                        default:
                            Console.WriteLine("Unknown argument: {0}", arg);
                            return false;
                    }
                }
                else
                {
                    string dir = System.IO.Path.GetDirectoryName(arg);
                    if (string.IsNullOrEmpty(dir))
                    {
                        dir = ".";
                    }
                    string pattern = System.IO.Path.GetFileName(arg);
                    foreach (string file in System.IO.Directory.GetFiles(dir, pattern))
                    {
                        files.Add(file);
                    }
                }
            }
            return files.Count > 0;
        }

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
                Console.WriteLine("Error: " + e.Message);
                return 1;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: unzip file.zip");
            Console.WriteLine("Unzips the given file in the current directory");
        }

        void Run()
        {
            foreach (var item in files)
            {
                Unzip(item);
            }
        }

        void Unzip(string filename)
        {
            using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(Directory.GetCurrentDirectory());
                }
            }
        }
    }
}
