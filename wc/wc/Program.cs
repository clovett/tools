using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace wc
{
    class Program
    {
        class FileStats
        {
            public long totalCharCount;
            public long totalLineCount;
            public long totalWordCount;
            public long totalLongestLine;

            public string Name { get; internal set; }
        }

        bool doCharCount;
        bool doLineCount;
        bool doWordCount;
        bool doLongestLine;

        bool readFileNames;
        bool printVersion;
        bool walkSubdirs;
        bool extensions;
        int fileCount;

        // when summarizing by file extension with -e
        Dictionary<string, FileStats> extmap = new Dictionary<string, FileStats>();

        // when not summarizing with -e
        List<FileStats> fileStats = new List<FileStats>(); 

        List<string> files = new List<string>();

        static int Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return -1;
            }
            try
            {
                return p.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine(@"Usage: wc [OPTION]... [FILE]...
Print newline, word, and byte counts for each FILE, and a total line if
more than one FILE is specified.  With no FILE read standard input.  
A word is a non-zero-length sequence of characters
delimited by white space.
The options below may be used to select which counts are printed, always in
the following order: newline, word, character, byte, maximum line length.
  -c,       print the character counts
  -l,       print the newline counts
  -f        read input from the files specified where each line in the
            file is the file name to examine (or wildcard pattern)
  -L,       print the length of the longest line
  -w,       print the word counts
  -s,       find all files in specified subdirectory tree
  -e        summarise stats by file extension
  -h        display this help and exit
  -version  output version information and exit
");
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1))
                    {
                        case "c":
                            doCharCount = true;
                            break;
                        case "l":
                            doLineCount = true;
                            break;
                        case "L":
                            doLongestLine = true;
                            break;
                        case "w":
                            doWordCount = true;
                            break;
                        case "f":
                            readFileNames = true;
                            break;
                        case "s":
                            walkSubdirs = true;
                            break;
                        case "e":
                            extensions = true;
                            break;
                        case "h":
                        case "help":
                        case "?":
                            return false;
                        case "version":
                            printVersion = true;
                            return true;
                        default:
                            WriteError("Unknown argument: " + arg);
                            return false;
                    }
                }
                else
                {
                    files.Add(arg);
                }
            }

            if (!doLineCount && !doCharCount && !doWordCount && !doLongestLine)
            {
                doLineCount = true; // a default
            }
            return true;
        }

        void WriteError(string msg)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("### Error: " + msg);
            Console.ForegroundColor = saved;
        }

        int Run()
        {
            if (printVersion)
            {
                Console.WriteLine("wc version " + this.GetType().Assembly.GetName().Version.ToString());
                return 0;
            }

            if (readFileNames)
            {
                ProcessFileNames();
            }

            if (walkSubdirs)
            {
                if (files.Count == 0)
                {
                    files.Add("*.*");
                }
                // then walk subdirectories looking for matching filenames in this.files.
                Regex compiled = CompilePatterns(this.files);
                List<string> matching = new List<string>();
                WalkSubdirectories(Directory.GetCurrentDirectory(), compiled, matching);
                this.files = matching;
            }
            else if (files.Count > 0)
            {
                this.files = ExpandWildCards(this.files);
            }

            if (files.Count == 0)
            {
                extensions = false;
                var root = new FileStats() { Name = "stdin" };
                this.fileStats.Add(root);
                ProcessFile("stdin", Console.In, root);                
            }
            else
            {
                foreach (string file in files)
                {
                    ProcessFile(file);
                }
            }

            int firstCol = 0;
            List<string> cols = new List<string>();
            if (extensions)
            {
                // we print the summary by file extension.
                foreach (var key in this.extmap.Keys)
                {
                    if (key.Length > firstCol)
                    {
                        firstCol = key.Length;
                    }
                }
                cols.Add(SpacePadRight("ext", firstCol));
            }
            else
            {
                // then we print each file we find.
                foreach (var s in this.fileStats)
                {
                    var name = s.Name;
                    if (name.Length > firstCol)
                    {
                        firstCol = name.Length;
                    }
                }
                cols.Add(SpacePadRight("file", firstCol));
            }

            if (doCharCount)
            {
                cols.Add("     chars");
            }
            if (doWordCount)
            {
                cols.Add("   words");
            }
            if (doLineCount)
            {
                cols.Add("   lines");
            }
            if (doLongestLine)
            {
                cols.Add(" longest");
            }
            Console.WriteLine(string.Join(",", cols));

            FileStats total = new FileStats();
            List<FileStats> list = this.fileStats;
            if (extensions)
            {
                var sorted = new List<string>(this.extmap.Keys);
                sorted.Sort();
                foreach (string key in sorted)
                {
                    var stats = this.extmap[key];
                    list.Add(stats);
                }
            }

            foreach (var stats in list)
            { 
                Console.Write(SpacePadRight(stats.Name, firstCol));

                if (doCharCount)
                {
                    total.totalCharCount += stats.totalCharCount;
                    Console.Write(",{0,10}", stats.totalCharCount);
                }
                if (doWordCount)
                {
                    total.totalWordCount += stats.totalWordCount;
                    Console.Write(",{0,8}", stats.totalWordCount);
                }
                if (doLineCount)
                {
                    total.totalLineCount += stats.totalLineCount;
                    Console.Write(",{0,8}", stats.totalLineCount);
                }
                if (doLongestLine)
                {
                    total.totalLongestLine += stats.totalLongestLine;
                    Console.Write(",{0,8}", stats.totalLongestLine);
                }
                Console.WriteLine();
            }

            if (fileCount > 1)
            {
                // print the totals
                Console.Write(SpacePadRight("total", firstCol));
                
                if (doCharCount)
                {
                    Console.Write(",{0,10}", total.totalCharCount);
                }
                if (doWordCount)
                {
                    Console.Write(",{0,8}", total.totalWordCount);
                }
                if (doLineCount)
                {
                    Console.Write(",{0,8}", total.totalLineCount);
                }
                if (doLongestLine)
                {
                    Console.Write(",{0,8}", total.totalLongestLine);
                }
                Console.WriteLine();
            }
            return 0;
        }

        private string SpacePadRight(string s, int len)
        {
            while (s.Length < len)
            {
                s = s + " ";
            }
            return s;
        }

        private Regex CompilePatterns(List<string> patterns)
        {
            // wild cards are a form of regular expression where "." is "\." and "*" is ".*" and "?" is "?".
            string regex = null;
            foreach (string pattern in patterns)
            {
                if (regex != null)
                {
                    regex += "|";
                }
                regex += pattern.Replace("\\", "\\\\").Replace(".","\\.").Replace("*", ".*");
            }
            return new Regex(regex);
        }

        private List<string> ExpandWildCards(List<string> files)
        {
            List<string> expanded = new List<string>();
            foreach (string file in files)
            {
                // support wild cards.
                string path = Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(path))
                {
                    path = Directory.GetCurrentDirectory();
                }
                string nameWithWildCards = Path.GetFileName(file);
                foreach (string fullPath in Directory.GetFiles(path, nameWithWildCards))
                {
                    expanded.Add(fullPath);
                }
            }
            return expanded;
        }

        private void WalkSubdirectories(string dir, Regex pattern, List<string> matching)
        {
            foreach (string file in Directory.GetFiles(dir))
            {
                if (pattern.IsMatch(file))
                {
                    matching.Add(file);
                }
            }
            foreach (string child in Directory.GetDirectories(dir))
            {
                var attr = new System.IO.DirectoryInfo(child);
                if ((attr.Attributes & FileAttributes.Hidden) == 0)
                {
                    WalkSubdirectories(child, pattern, matching);
                }
            }
        }

        // preocess the "-f" option which is a level of indirection, the command
        // line points to files that contain the real file names to process.
        void ProcessFileNames()
        {
            List<string> fileNames = new List<string>();

            if (files.Count == 0)
            {
                extensions = false;
                // Read file names from stdin.

                string line = null;
                while ((line = Console.In.ReadLine()) != null)
                {
                    fileNames.Add(line);
                }

                if (fileNames.Count == 0)
                {
                    Console.WriteLine("no files found");
                    Environment.Exit(1);
                }
            }
            else
            {
                foreach (string file in files)
                {
                    if (file.Contains("*") || file.Contains("?"))
                    {
                        throw new Exception("the -f option does not support wild cards");
                    }
                    using (StreamReader reader = new StreamReader(file))
                    {
                        string line = reader.ReadLine();
                        while (line != null)
                        {
                            fileNames.Add(line);
                            line = reader.ReadLine();
                        }
                    }
                }
            }

            // ok, we've process the -f option now everything continues normally.
            this.files = fileNames;
        }

        void ProcessFile(string filePath)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                FileStats stats = null;
                if (extensions)
                {
                    if (!extmap.TryGetValue(ext, out stats)) 
                    {
                        stats = new FileStats() { Name = ext };
                        extmap[ext] = stats;
                    }
                }
                else
                {
                    stats = new FileStats() { Name = MakeRelative(filePath) };
                    this.fileStats.Add(stats);
                }

                using (StreamReader reader = new StreamReader(filePath))
                {
                    ProcessFile(filePath, reader, stats);
                }
            }
            catch (Exception ex)
            {
                WriteError("Error reading " + filePath + ", " + ex.Message);
            }
        }

        void ProcessFile(string filename, TextReader reader, FileStats stats)
        {
            string line = reader.ReadLine();
            while (line != null)
            {
                if (doCharCount)
                {
                    stats.totalCharCount += line.Length;
                }
                if (doLongestLine)
                {
                    if (line.Length > stats.totalLongestLine)
                    {
                        stats.totalLongestLine = line.Length;
                    }
                }
                if (doWordCount)
                {
                    stats.totalWordCount += CountWords(line);
                }
                if (doLineCount)
                {
                    stats.totalLineCount++;
                }
                line = reader.ReadLine();
            }
            fileCount++;
        }

        int CountWords(string line)
        {
            bool whitespace = true;
            int wordCount = 0;
            for (int i = 0, n = line.Length; i<n;i++)
            {
                char ch = line[i];
                if (Char.IsWhiteSpace(ch))
                {
                    whitespace = true;
                }
                else
                {
                    if (whitespace)
                    {
                        wordCount++;
                        whitespace = false;
                    }
                }
            }
            return wordCount;
        }

        static string MakeRelative(string filename)
        {
            var baseUri = new Uri(Directory.GetCurrentDirectory() + "\\");
            var uri = new Uri(filename);
            var relative = baseUri.MakeRelativeUri(uri);
            if (relative.IsAbsoluteUri)
            {
                return relative.LocalPath;
            }

            string original = uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped).Replace('/', '\\');
            string result = relative.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped).Replace('/', '\\');
            if (result.Length > original.Length)
            {
                // keep the full path then, it's shorter!
                return filename;
            }
            return result;
        }
    }
}
