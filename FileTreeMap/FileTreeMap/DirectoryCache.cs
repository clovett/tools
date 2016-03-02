using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTreeMap
{
    class DirectoryCache
    {
        Dictionary<string, HashSet<string>> tree = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, HashSet<string>> files = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        HashSet<String> topLevel = new HashSet<string>();
        string mostRecent;

        public string FindFile(string name)
        {
            if (File.Exists(name))
            {
                return name;
            }

            string path = FindFileInternal(mostRecent, name);
            if (path == null)
            {
                foreach (string topDir in topLevel)
                {
                    path = FindFileInternal(topDir, name);
                    if (path != null)
                    {
                        break;
                    }
                }
            }
            if (path != null)
            {
                // search in this dir next time for added speed.
                mostRecent = Path.GetDirectoryName(path);
            }
            return path;
        }

        private string FindFileInternal(string context, string name)
        {
            string fileName = Path.GetFileName(name);
            HashSet<string> children = null;
            if (files.TryGetValue(context, out children))
            {
                if (children.Contains(fileName))
                {
                    string fullPath = Path.Combine(context, fileName);
                    if (fullPath.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return fullPath;
                    }
                }
            }

            HashSet<string> subdirs = null;
            if (tree.TryGetValue(context, out subdirs))
            {
                foreach (string dir in subdirs)
                {
                    string fullPath = FindFileInternal(dir, name);
                    if (fullPath != null)
                    {
                        return fullPath;
                    }
                }
            }
            return null;
        }

        private void Populate(string context)
        {
            if (tree.ContainsKey(context))
            {
                return;
            }

            Debug.WriteLine("Loading dir: " + context);
            AddTopLevel(context);
            HashSet<string> subdirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dir in Directory.GetDirectories(context))
            {
                subdirs.Add(dir);
                Populate(dir);
            }
            tree[context] = subdirs;


            if (files.ContainsKey(context))
            {
                return;
            }
            HashSet<string> children = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(context))
            {
                children.Add(Path.GetFileName(file));
            }
            files[context] = children;
        }

        private void AddTopLevel(string context)
        {
            foreach (string s in topLevel)
            {
                if (context.StartsWith(s, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            topLevel.Add(context);

            foreach (string s in topLevel.ToArray())
            {
                if (s.Length > context.Length && context.StartsWith(s))
                {
                    topLevel.Remove(s); // this is not longer a top level.
                }
            }
        }

        internal void AddDirectory(string dir)
        {
            if (Directory.Exists(dir))
            {
                mostRecent = dir;
                Populate(dir);
            }
            else
            {
                Debug.WriteLine("Directory not found: " + dir);
            }
        }

    }
}
