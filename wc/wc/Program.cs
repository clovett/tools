using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wc
{
    class Program
    {
        int? charCount;
        int? lineCount;
        int? wordCount;
        int? longestLine;
        int? totalCharCount;
        int? totalLineCount;
        int? totalWordCount;
        int? totalLongestLine;
        bool readFileNames;
        bool printVersion;
        bool walkSubdirs;
        int fileCount;

        List<string> files = new List<string>();

        static int Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return 1;
            }
            return p.Run();
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
  -f        read input from the files specified by
            NUL-terminated names in FILE or standard input
  -L,       print the length of the longest line
  -w,       print the word counts
  -s,       find all files in specified subdirectory tree
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
                            charCount = 0;
                            totalCharCount = 0;
                            break;
                        case "l":
                            lineCount = 0;
                            totalLineCount = 0;
                            break;
                        case "f":
                            readFileNames = true;
                            break;
                        case "L":
                            longestLine = 0;
                            totalLongestLine = 0;
                            break;
                        case "w":
                            wordCount = 0;
                            totalWordCount = 0;
                            break;
                        case "s":
                            walkSubdirs = true;
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

            if (walkSubdirs && readFileNames)
            {
                WriteError("The arguments -f and -s are mutually exclusive");
                return false;
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

            if (files.Count == 0)
            {
                // read standard input.
                if (walkSubdirs)
                {
                    WalkSubdirectories(Directory.GetCurrentDirectory(), "*.*");
                }
                else if (readFileNames)
                {
                    ProcessFileNames(Console.In);
                }
                else
                {
                    ProcessFile("stdin", Console.In);
                }
            }
            else
            {
                foreach (string file in files)
                {
                    // support wild cards.
                    string path = Path.GetDirectoryName(file);
                    if (string.IsNullOrEmpty(path))
                    {
                        path = Directory.GetCurrentDirectory();
                    }
                    string nameWithWildCards = Path.GetFileName(file);

                    if (walkSubdirs)
                    {
                        WalkSubdirectories(path, nameWithWildCards);
                    }
                    else
                    {
                        foreach (string fullPath in Directory.GetFiles(path, nameWithWildCards))
                        {
                            if (readFileNames)
                            {
                                using (StreamReader reader = new StreamReader(fullPath))
                                {
                                    ProcessFileNames(reader);
                                }
                            }
                            else
                            {
                                ProcessFile(fullPath);
                            }
                        }
                    }
                }
            }

            if (fileCount > 1)
            {
                // print the totals
                if (totalLineCount.HasValue)
                {
                    Console.Write("{0,8}", totalLineCount.Value);
                }
                if (totalWordCount.HasValue)
                {
                    Console.Write(" {0,8}", totalWordCount);
                }
                if (totalCharCount.HasValue)
                {
                    Console.Write(" {0,8}", totalCharCount.Value);
                }
                if (totalLongestLine.HasValue)
                {
                    Console.Write(" {0,8}", totalLongestLine.Value);
                }
                Console.WriteLine(" total");
            }
            return 0;
        }

        private void WalkSubdirectories(string dir, string pattern)
        {
            foreach (string file in Directory.GetFiles(dir, pattern))
            {
                ProcessFile(file);
            }
            foreach (string child in Directory.GetDirectories(dir))
            {
                WalkSubdirectories(child, pattern);
            }
        }

        void ProcessFileNames(TextReader reader)
        {
            string line = reader.ReadLine();
            while (line != null)
            {
                ProcessFile(line);
                line = reader.ReadLine();
            }
        }

        void ProcessFile(string filePath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    ProcessFile(filePath, reader);
                }
            }
            catch (Exception ex)
            {
                WriteError("Error reading " + filePath + ", " + ex.Message);
            }
        }

        void ProcessFile(string filename, TextReader reader)
        {
            string line = reader.ReadLine();
            while (line != null)
            {
                if (charCount.HasValue)
                {
                    charCount += line.Length;
                }
                if (longestLine.HasValue)
                {
                    if (line.Length > longestLine.Value)
                    {
                        longestLine = line.Length;
                    }
                }
                if (wordCount.HasValue)
                {
                    wordCount += CountWords(line);
                }
                if (lineCount.HasValue)
                {
                    lineCount++;
                }
                line = reader.ReadLine();
            }
            reader.Dispose();

            // print the results
            if (lineCount.HasValue)
            {
                Console.Write("{0,8}", lineCount.Value);
                totalLineCount += lineCount.Value;
                lineCount = 0;
            }
            if (wordCount.HasValue)
            {
                Console.Write(" {0,8}", wordCount.Value);
                totalWordCount += wordCount.Value;
                wordCount = 0;
            }
            if (charCount.HasValue)
            {
                Console.Write(" {0,8}", charCount.Value);
                totalCharCount += charCount.Value;
                charCount = 0;
            }
            if (longestLine.HasValue)
            {
                Console.Write(" {0,8}", longestLine.Value);
                if (!totalLongestLine.HasValue || longestLine.Value > totalLongestLine)
                {
                    totalLongestLine = longestLine.Value;
                }
                longestLine = 0;
            }
            Console.WriteLine(" " + filename);
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
    }
}
