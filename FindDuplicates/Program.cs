using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FindDuplicates
{
    class Program
    {
        int files;
        int dups;
        bool verbose;
        bool compare;
        List<string> fullPaths = new List<string>();
        Dictionary<HashedFile, List<HashedFile>> fileIndex = new Dictionary<HashedFile, List<HashedFile>>();

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: FindDuplicates [options] <dirs>");
            Console.WriteLine("Searches given directories for any duplicate files and prints out their full path.");
            Console.WriteLine("Options:");
            Console.WriteLine("    -v    verbose output");
            Console.WriteLine("    -c    compare directories");
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

        void Run()
        {

            Stopwatch watch = new Stopwatch();
            watch.Start();

            foreach (string path in this.fullPaths)
            {
                CreateIndex(path);
            }

            watch.Stop();
            long total = watch.ElapsedMilliseconds;
            Console.WriteLine("Hashed {0} files in {1:N3} seconds", files, (double)watch.ElapsedMilliseconds / 1000.0);
            watch.Reset();
            watch.Start();

            OptimizeIndex();
            watch.Stop();
            total += watch.ElapsedMilliseconds;
            Console.WriteLine("Optimized the index in {1:N3} seconds", files, (double)watch.ElapsedMilliseconds / 1000.0);
            watch.Reset();
            watch.Start();

            ReportDuplicates();
            watch.Stop();
            total += watch.ElapsedMilliseconds;
            Console.WriteLine("Found {0} duplicates in {1:N3} seconds", dups, (double)watch.ElapsedMilliseconds / 1000.0);

            Console.WriteLine();
            Console.WriteLine("Total time is {1:N3} seconds", dups, (double)total / 1000.0);
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
                            compare = true;
                            break;
                        default:
                            Console.WriteLine("Unexpected argument: " + arg);
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
                            Console.WriteLine("Directory not found: " + path);
                            return false;
                        }
                        fullPaths.Add(path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("### error with directory: {0}\n{1}", arg, ex.Message);
                        return false;
                    }

                }
            }

            if (fullPaths.Count == 0)
            {
                Console.WriteLine("Missing directory argument to search");
                return false;
            }


            return true;
        }


        private void CreateIndex(string path)
        {
            if (verbose) Console.WriteLine(path);
            foreach (string file in Directory.GetFiles(path))
            {
                files++;
                HashedFile key = null;
                key = new HashedFile(file);

                AddFile(key);
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                CreateIndex(dir);
            }
        }

        private void AddFile(HashedFile key)
        {
            List<HashedFile> list = null;
            if (!fileIndex.TryGetValue(key, out list))
            {
                list = new List<HashedFile>();
                list.Add(key);
                fileIndex[key] = list;
            }
            else
            {
                list.Add(key);
            }
        }

        int GetLongestConflict()
        {
            return (from i in fileIndex.Values select i.Count).Max(); 
        }

        private void ReportDuplicates()
        {
            dups = 0;

            // ok, now do a deep compare of any files that have identical hashes to see what is really duplicated or not.
            foreach (var pair in fileIndex)
            {
                var list = pair.Value;
                while (list.Count > 1)
                {                    
                    List<HashedFile> nondups = new List<HashedFile>();
                    HashedFile first = list[0];
                    bool foundDup = false;
                    for (int i = 1; i < list.Count; i++)
                    {
                        HashedFile other = list[i];
                        if (!first.Equals(other))
                        {
                            nondups.Add(other);
                        }
                        else if (first.DeepEquals(other))
                        {
                            if (!foundDup)
                            {
                                foundDup = true;
                                Console.WriteLine(first.Path);
                            }
                            dups++;
                            Console.WriteLine(other.Path);
                        }
                        else
                        {
                            nondups.Add(other);
                        }
                    }
                    if (foundDup)
                    {
                        Console.WriteLine();
                    }

                    // now search the remainder for other matches.
                    list = nondups;
                }
            }
        }

        private void OptimizeIndex()
        {
            int hashPrefixLength = 32000; // amount of the file to read to compute hash.

            while (GetLongestConflict() > 5)
            {
                bool fileIsLonger = false;

                // now rehashing anything that shows same file size to get less clashes.
                foreach (var pair in fileIndex.ToArray())
                {
                    var list = pair.Value;

                    if (list.Count > 2)
                    {
                        // re-hash these files
                        fileIndex.Remove(pair.Key);
                        foreach (var info in list)
                        {
                            info.SetSha1PrefixHash(hashPrefixLength);
                            if (info.FileLength > hashPrefixLength)
                            {
                                fileIsLonger = true;
                            }
                            AddFile(info);
                        }
                    }
                }
                if (!fileIsLonger)
                {
                    // rehashing won't help
                    break;
                }

                // if this is still not good enough, then increase the length.
                hashPrefixLength *= 2;
            }
        }

    }
}
