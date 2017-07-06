using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Walkabout.Utilities;
using System.Diagnostics;

namespace GrovelIncludes
{
    class Program
    {
        List<string> includePaths = new List<string>();
        List<string> inputFiles = new List<string>();
        string output;
        SimpleGraph graph = new SimpleGraph();
        HashSet<string> processed = new HashSet<string>();
        Dictionary<string, string> standardIncludes = new Dictionary<string, string>();

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

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: GrovelIncludes [-i includepath] <files> -o output.dgml");
            Console.WriteLine("Scans the given files for #include statements and uses the given include paths to find");
            Console.WriteLine("those includes and everything they include, then it outputs a DGML graph of the result");
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
                        case "i":
                        case "include":
                            if (i + 1 < args.Length)
                            {
                                includePaths.Add(args[++i]);
                            }
                            break;

                        case "o":
                        case "out":
                        case "output":
                            if (i + 1 < args.Length)
                            {
                                output = args[++i];
                            }
                            break;
                    }
                }
                else
                {
                    inputFiles.Add(arg);
                }
            }
            return true;
        }

        void LoadStandardIncludes()
        {
            foreach (string file in includePaths)
            {
                string path = Path.GetFullPath(file);
                LoadStandardIncludePath(path, "");
            }
        }

        void LoadStandardIncludePath(string path, string prefix)
        {
            foreach (string name in Directory.GetFiles(path, "*.*"))
            {
                string id = Path.GetFileName(name);
                standardIncludes[id] = name;
                if (!string.IsNullOrEmpty(prefix))
                {
                    id = prefix + "/" + id;
                }
                standardIncludes[id] = name;
            }
            foreach (string name in Directory.GetDirectories(path, "*.*"))
            {
                LoadStandardIncludePath(name, Path.GetFileName(name));
            }
        }


        int Run()
        {
            LoadStandardIncludes();

            while (inputFiles.Count > 0) 
            {
                string file = inputFiles[0];
                inputFiles.RemoveAt(0);

                try
                {
                    string dir = Path.GetDirectoryName(file);
                    if (string.IsNullOrEmpty(dir))
                    {
                        dir = Directory.GetCurrentDirectory();
                    }

                    foreach (string name in Directory.GetFiles(dir, Path.GetFileName(file)))
                    {
                        ProcessFile(name);
                    }
                }
                catch
                {
                }
            }


            if (!string.IsNullOrEmpty(output))
            {
                graph.Save(output);
            }
            else
            {
                graph.Save(Console.Out);
            }

            return 0;
        }

        void ProcessFile(string file)
        {
            if (processed.Contains(file)) 
            {
                return;
            }
            processed.Add(file);

            using (StreamReader reader = new StreamReader(file))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("#include"))
                    {
                        string path = FindInclude(file, line.Substring(8));
                        if (path != null)
                        {
                            GroupByDirectory(file, path);
                            inputFiles.Add(path);
                        }
                        else
                        {
                            GroupByDirectory(file, path);
                            inputFiles.Add(path);
                        }
                    }
                    line = reader.ReadLine();
                }
            }
        }

        private void GroupByDirectory(string file, string path)
        {
            string dir1;
            string name1;
            SplitPath(file, out dir1, out name1);

            string dir2 = null;
            string name2 = null;
            SplitPath(path, out dir2, out name2);

            if (dir1 != null)
            {
                graph.GetOrAddLink(dir1, name1).Category = "Contains";
                graph.AddOrGetNode(dir1).AddProperty("Group", "Expanded");
            }
            if (dir2 != null)
            {
                graph.GetOrAddLink(dir2, name2).Category = "Contains";
                graph.AddOrGetNode(dir2).AddProperty("Group", "Expanded");
            }            

            graph.GetOrAddLink(name1, name2);

            graph.AddOrGetNode(name1).AddProperty("Reference", file);
            graph.AddOrGetNode(name2).AddProperty("Reference", path);

        }

        private static void SplitPath(string file, out string dir1, out string name1)
        {
            dir1 = null;
            name1 = null;
            int i = file.LastIndexOfAny(new char[] { '\\', '/' });
            if (i > 0)
            {
                name1 = file.Substring(i + 1);
                dir1 = file.Substring(0, i);
            }
            else
            {
                dir1 = null;
                name1 = file;
            }
        }

        private string Resolve(string baseUri, string relative)
        {
            Uri uri = new Uri(baseUri);
            Uri resolved = new Uri(uri, relative);
            return resolved.LocalPath;
        }

        private string FindInclude(string baseUri, string name)
        {
            // strip off the quotes
            name = name.Trim();
            char c = name[0];
            if (c == '<') c = '>';
            int i = name.IndexOf(c, 1);
            if (i > 0)
            {
                name = name.Substring(1, i - 1);
            }
            name = name.Replace("/", "\\");
            if (name.Contains("CodeGen.h"))
            {
                Debug.WriteLine("Debugme");
            }

            string local = Resolve(baseUri, name);
            if (File.Exists(local)) 
            {
                return local;
            }

            // perhaps it is in the include search paths.
            if (standardIncludes.TryGetValue(name, out local)) {
                return local;
            }

            foreach (string path in includePaths)
            {
                local = Resolve(path + "\\", name);
                if (File.Exists(local))
                {
                    return local;
                }
            }

            Console.WriteLine("### include {0} not found", name);
            return name;
        }

    }
}
