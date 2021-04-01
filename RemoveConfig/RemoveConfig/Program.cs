using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace RemoveConfig
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: RemoveConfig solution configname");
                Console.WriteLine("Removes the given build configuration from the given solution");
                return 1;
            }

            string solution = args[0];
            string config = args[1];
            if (!File.Exists(solution))
            {
                Console.WriteLine("Solution '{0}' not found", solution);
                return 1;
            }

            Solution s = new Solution();
            s.Load(solution);
            s.RemoveConfig(config);
            s.Save();
            return 0;
        }
    }

    class Solution 
    {
        List<string> contents;
        string fileName;
        bool changed;

        public void Load(string filename)
        {
            fileName = filename;
            contents = new List<string>();
            using (StreamReader reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        contents.Add(line);
                    }
                }
            }
        }

        internal void RemoveConfig(string config)
        {
            Console.WriteLine($"Removing configuration {config} from {this.fileName}");
            var prefix = "= " + config + "|";
            Uri baseUri = new Uri(this.fileName);
            int pos = 0;
            while (pos < contents.Count)
            {
                var line = contents[pos];
                if (line.StartsWith("Project("))
                {
                    var parts = line.Split('=');
                    if (parts.Length > 1)
                    {
                        var args = parts[1].Split(',');
                        if (args.Length > 1)
                        {
                            var rel = args[1].Trim().Trim('"');
                            if (rel != "Solution Items")
                            {
                                var resolved = new Uri(baseUri, rel);
                                StripProject(resolved.LocalPath, config);
                            }
                        }
                    }
                }
                if (line.Contains(prefix))
                {
                    Console.WriteLine("Stripping: {0}", line);
                    contents.RemoveAt(pos);
                    changed = true;
                }
                else
                {
                    pos++;
                }
            }
        }

        private void StripProject(string filename, string config)
        {            
            Console.WriteLine($"Stripping {config} from {filename}");
            var doc = XDocument.Load(filename);
            var ns = doc.Root.Name.Namespace;
            List<XElement> toRemove = new List<XElement>();
            foreach (var e in doc.Descendants(ns + "ProjectConfiguration"))
            {
                var include = (string)e.Attribute("Include");
                if (!string.IsNullOrEmpty(include) && include.StartsWith(config + "|"))
                {
                    toRemove.Add(e);
                }
            }

            var prefix = $"'$(Configuration)|$(Platform)'=='{config}|";
            foreach (var e in doc.Descendants(ns + "PropertyGroup"))
            {
                var condition = (string)e.Attribute("Condition");
                if (!string.IsNullOrEmpty(condition) && condition.Contains(prefix))
                {
                    toRemove.Add(e);
                }
            }

            foreach (var e in doc.Descendants(ns + "ItemDefinitionGroup"))
            {
                var condition = (string)e.Attribute("Condition");
                if (!string.IsNullOrEmpty(condition) && condition.Contains(prefix))
                {
                    toRemove.Add(e);
                }
            }

            foreach (var e in doc.Descendants(ns + "ImportGroup"))
            {
                var condition = (string)e.Attribute("Condition");
                if (!string.IsNullOrEmpty(condition) && condition.Contains(prefix))
                {
                    toRemove.Add(e);
                }
            }

            if (toRemove.Count > 0)
            {
                toRemove.ForEach((e) => e.Remove());
                doc.Save(filename, SaveOptions.None);
            }
        }

        internal void Save()
        {
            if (changed)
            {
                using (StreamWriter writer = new StreamWriter(this.fileName))
                {
                    foreach(var line in this.contents)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }
    }
}
