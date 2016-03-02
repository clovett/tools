using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeMaps;

namespace FileTreeMap
{
    delegate void TreeBuilderProgressHandler(int min, int max, int progress, bool indeterminate);

    class TreeBuilder
    {
        DirectoryCache cache = new DirectoryCache();

        public IEnumerable<string> FindFilesInDirectory(string path)
        {
            List<string> files = new List<string>();
            AddFilesInDirectory(path, files);
            return files;
        }

        private void AddFilesInDirectory(string path, List<string> files)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            foreach (FileInfo file in info.GetFiles("*.*"))
            {
                if ((file.Attributes & FileAttributes.Hidden) == 0)
                {
                    files.Add(file.FullName);
                }
            }
            foreach (DirectoryInfo dir in info.GetDirectories("*.*"))
            {
                if ((dir.Attributes & FileAttributes.Hidden) == 0)
                {
                    AddFilesInDirectory(dir.FullName, files);
                }
            }
        }

        public TreeNodeData AnalyzeFiles(IEnumerable<string> files, TreeBuilderProgressHandler progress)
        {
            TreeNodeData tree = new TreeNodeData("nodeData", null);

            int count = files.Count();
            int index = 0;
            foreach (string file in files)
            {
                double lines = CountLines(file);
                AddNode(tree, file, lines);
                index++;
                progress(0, count, index, false);
            }

            ColorTree(tree);

            // skip singleton nodes
            TreeNodeData parent = tree;
            while (parent != null && parent.Children != null && parent.Children.Count == 1)
            {
                parent = parent.Children[0];
            }

            parent.Parent = null;
            return parent;
        }

        private void ColorTree(TreeNodeData tree)
        {
            if (tree.Children != null)
            {
                double max = (from n in tree.Children select n.Size).Max();
                if (max == 0) max = 1;

                foreach (var node in tree.Children)
                {
                    node.SetColor(0, Color.FromArgb(0xff, 0x20, (byte)(50 + (255.0 - 50) * node.Size / max), 0x20)); // green scale where maximum size is bright green.
                    ColorTree(node);
                }
            }
        }

        private void AddNode(TreeNodeData tree, string file, double lines)
        {
            string[] parts = file.Split('\\');
            TreeNodeData parent = tree;
            string key = null;
            foreach (string part in parts)
            {
                if (key == null)
                {
                    key = part;
                    if (key.EndsWith(":"))
                    {
                        key += '\\';
                    }
                }
                else
                {
                    key = System.IO.Path.Combine(key, part);
                }
                TreeNodeData child = parent.GetOrCreateNode(key, part);
                child.AddSize(0, lines); // parent folders accumulate size of children.
                parent = child;
            }
        }

        private double CountLines(string file)
        {
            int count = 0;
            using (StreamReader reader = new StreamReader(file))
            {
                string line = null;
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        count++;
                    }
                } while (line != null);
            }
            return count;
        }

        public IEnumerable<String> FindFilesInLogFile(string file)
        {
            HashSet<string> files = new HashSet<string>();
            using (StreamReader reader = new StreamReader(file))
            {
                string line = null;
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        string path = ParseLine(line);
                        if (path != null)
                        {
                            files.Add(path);
                        }
                    }
                }
                while (line != null);
            }
            List<String> sorted = new List<string>(files);
            sorted.Sort(StringComparer.OrdinalIgnoreCase);
            return sorted;
        }

        const string MakeEnteringDirectory = "Entering directory";
        static char[] DirectoryTrimChars = new char[] { '`', '\'' };
        static char[] WhitespaceChars = new char[] { ' ', '\t' };
        const string SubmodulesPrefix = "Skipping submodules";

        private string ParseLine(string line)
        {
            string name = null;
            if (line.StartsWith("CC:"))
            {
                name = LinuxToWindows(line.Substring(3));
            }
            else if (line.StartsWith("CXX:"))
            {
                name = LinuxToWindows(line.Substring(4));
            }
            else if (line.Contains(MakeEnteringDirectory))
            {
                int i = line.IndexOf(MakeEnteringDirectory);
                string tail = line.Substring(i + MakeEnteringDirectory.Length + 1);
                tail = tail.Trim(DirectoryTrimChars);
                cache.AddDirectory(LinuxToWindows(tail));
            }
            else if (line.Contains(SubmodulesPrefix))
            {
                int i = line.IndexOf('/');
                if (i > 0)
                {
                    cache.AddDirectory(LinuxToWindows(line.Substring(i)));
                }
            }

            if (name != null)
            {
                string path = cache.FindFile(name);
                if (path != null)
                {
                    return path;
                }
                else
                {
                    Debug.WriteLine("File not found: " + name + " under directory " + path);
                }
            }
            return null;
        }

        private string LinuxToWindows(string path)
        {
            path = path.Trim(WhitespaceChars);
            path = path.Replace('/', '\\');
            if (path.StartsWith("\\"))
            {
                path = path.Trim('\\');
                path = path[0] + ":" + path.Substring(1);
            }
            return path;
        }
    }
}
