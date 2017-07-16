using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeRdfMetadata
{
    enum MergeAction
    {
        Exists,
        Added,
        Removed
    }

    class TreeMap
    {
        public TreeMap Parent;
        public string Path;
        public bool IsDirectory;

        public TreeMap GetOrCreateChild(string path)
        {
            string name = System.IO.Path.GetFileName(path);
            if (Children == null)
            {
                Children = new Dictionary<string, TreeMap>();
            }
            TreeMap node = null;
            if (Children.TryGetValue(name, out node))
            {
                return node;
            }
            Children[name] = node = new MergeRdfMetadata.TreeMap() { Parent = this, Path = path };
            return node;
        }

        public int Size()
        {
            int i = 1;
            if (Children != null)
            {
                foreach (var node in Children.Values)
                {
                    i += node.Size();
                }
            }
            return i;
        }

        public static TreeMap LoadSource(string dir)
        {
            TreeMap root = new TreeMap();
            root.Load(dir);
            return root;
        }

        private void Load(string dir)
        {
            this.Path = dir;
            foreach (var file in Directory.GetFiles(dir))
            {
                GetOrCreateChild(file);
            }
            foreach (var child in Directory.GetDirectories(dir))
            {
                var node = GetOrCreateChild(child);
                node.IsDirectory = true;
                node.Load(child);                
            }
        }

        Dictionary<string, TreeMap> Children;

        /// <summary>
        /// Compare both TreeMaps and call the given degate with the merged TreeMap and 
        /// a MergeAction that represents the state of the merge.  Either the TreeMap
        /// Exists in both trees, or TreeMap represents an Removed entry (exists in source but not in the destination)
        /// or it represents a Added entry (exists in destination but not in the source).
        /// This Merge function will walk the entire subdirectory of new/removed branches so you don't have to.
        /// </summary>
        /// <param name="dest">The destination map to merge</param>
        /// <param name="func">The merge function to call with the discovered state</param>
        public void Merge(TreeMap dest, Action<TreeMap, TreeMap, MergeAction> func)
        {
            if (dest == null)
            {
                WalkSource(func);
            }
            else if (this.Children == null && dest.Children == null)
            {
                // these these must be the same file
                func(this, dest, MergeAction.Exists);
            }
            else if (this.Children == null)
            {
                // reached the end of this tree so everything from here on is "Removed" entry
                dest.WalkDestination(func);
            }
            else if (dest.Children == null)
            {
                // reached the end of destination tree so everything from here on is "Added" entry
                WalkSource(func);
            }
            else
            {
                // walk "Existing" nodes and 
                foreach (var pair in Children)
                {
                    string name = pair.Key;
                    TreeMap node = pair.Value;
                    TreeMap other = null;
                    if (dest.Children.TryGetValue(name, out other))
                    {
                        func(node, other, MergeAction.Exists);
                    }
                    else
                    {
                        func(node, null, MergeAction.Removed);
                    }
                }

                // walk "Removed" nodes (exist only in destination)
                foreach (var pair in dest.Children)
                {
                    string name = pair.Key;
                    TreeMap node = pair.Value;
                    TreeMap other = null;
                    if (!Children.TryGetValue(name, out other))
                    {
                        node.WalkDestination(func);
                    }
                }

                // now recurse (doing this last improves disk locality at each directory level)
                foreach (var pair in Children)
                {
                    string name = pair.Key;
                    TreeMap node = pair.Value;
                    TreeMap other = null;
                    if (dest.Children.TryGetValue(name, out other))
                    {
                        node.Merge(other, func);
                    }
                }
            }
        }

        internal string GetRelativePath()
        {
            if (this.Parent == null)
            {
                return "";
            }
            else
            {
                string name = System.IO.Path.GetFileName(this.Path);
                return System.IO.Path.Combine(this.Parent.GetRelativePath(), name);
            }
        }

        private void WalkSource(Action<TreeMap, TreeMap, MergeAction> func)
        {
            func(this, null, MergeAction.Removed);
            if (this.Children != null)
            {
                foreach (var node in Children.Values)
                {
                    node.WalkSource(func);
                }
            }
        }

        private void WalkDestination(Action<TreeMap, TreeMap, MergeAction> func)
        {
            func(null, this, MergeAction.Added);
            if (this.Children != null)
            {
                foreach (var node in Children.Values)
                {
                    node.WalkDestination(func);
                }
            }
        }
    }

}
