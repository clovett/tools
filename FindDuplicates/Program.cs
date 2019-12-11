using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MergePhotos
{
    class Program
    {
        bool verbose;
        string source;
        string target;
        bool docopy = false;
        FolderIndex sourceIndex;
        FolderIndex targetIndex;

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: MergePhotos [options] source_dir target_dir");
            Console.WriteLine("Merges two photos folders into target_dir, resulting in no duplicates and merged metadata.");
            Console.WriteLine("Options:");
            Console.WriteLine("    -v    verbose output");
            Console.WriteLine("    -c    do the actual recommended file copies");
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            p.Run();
        }

        bool ParseCommandLine(string[] args)
        {

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "?":
                        case "help":
                            return false;
                        case "v":
                        case "verbose":
                            verbose = true;
                            break;
                        case "c":
                            docopy = true;
                            break;
                        default:
                            WriteError("Unexpected argument: " + arg);
                            return false;
                    }
                }
                else 
                {
                    try
                    {
                        var path = Path.GetFullPath(arg);
                        if (!Directory.Exists(path))
                        {
                            WriteError("Directory not found: " + path);
                            return false;
                        }
                        if (source == null)
                        {
                            source = path;
                        }
                        else if (target == null)
                        {
                            target = path;
                        }
                        else
                        {
                            WriteError("Too many directories provided");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError("Error with directory: {0}\n{1}", arg, ex.Message);
                        return false;
                    }

                }
            }

            if (source == null || target == null)
            {
                WriteError("Please provide source and target folders");
                return false;
            }


            return true;
        }

        private static void WriteError(string format, params string[] args)
        {
            var saved = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(format, args);
            } 
            finally
            {
                Console.ForegroundColor = saved;
            }
        }

        void Run()
        {
            Stopwatch watch = new Stopwatch();

            sourceIndex = new FolderIndex(source, verbose);
            targetIndex = new FolderIndex(target, verbose);

            watch.Start();
            bool header = false;
            foreach(var dups in targetIndex.FindDuplicates())
            {
                if (!header)
                {
                    WriteError("Target directory contains its own duplicates!");
                    header = true;
                }

                foreach (var item in dups)
                {
                    Console.WriteLine("    " + item.Path);
                }
                Console.WriteLine();
            }
            if (header)
            {
                return;
            }

            foreach(var dups in sourceIndex.FindDuplicates())
            {
                PickSourceDuplicate(dups);
            }

            watch.Stop();

            Console.WriteLine("Checked self-duplicates in {0:N3} seconds", (double)watch.ElapsedMilliseconds / 1000.0);


            watch.Reset();
            watch.Start();

            targetIndex.Merge(sourceIndex, docopy);

            Console.WriteLine("Merging folders in {0:N3} seconds", (double)watch.ElapsedMilliseconds / 1000.0);

            Console.WriteLine();
        }

        private void PickSourceDuplicate(List<HashedFile> files)
        {
            // heuristic, if file name ends with (1), (2) and so on, then it is probably a copy paste error, so pick the
            // file name that doesn't contain this suffix.
            // Otherwise pick the longest file name because it is probably the most descriptive.

            string longest = null;
            HashedFile longestFile = null;
            string nonIndexed = null;
            HashedFile nonIndexedFile = null;
            bool hasIndexes = false;
            Regex re = new Regex(".*\\(([0-9]+)\\)$");
            foreach (var item in files)
            {
                string baseName = System.IO.Path.GetFileNameWithoutExtension(item.Path);
                if (longest == null || longest.Length < baseName.Length)
                {
                    longest = baseName;
                    longestFile = item;
                }
                var match = re.Match(baseName);
                if (match.Success)
                {
                    hasIndexes = true;
                } else {
                    nonIndexed = baseName;
                    nonIndexedFile = item;
                }
            }
            if (hasIndexes && nonIndexed != null)
            {
                Console.WriteLine("Picking source duplicate: " + nonIndexedFile.Path);
                sourceIndex.ResolveDuplicate(nonIndexedFile);
            }
            else
            {
                Console.WriteLine("Picking source duplicate: " + longestFile.Path);
                sourceIndex.ResolveDuplicate(longestFile);
            }
        }

    }
}
