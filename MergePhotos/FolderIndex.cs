using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MergePhotos
{
    class MergeOptions
    {
        /// <summary>
        /// Whether to cleanup source folder by removing anything that was copied to the 
        /// target folder.  This will leave only conflicting files.  Also removes duplicates
        /// from either source or target folders.
        /// </summary>
        public bool RemoveFiles;

        /// <summary>
        /// Whether to actually do the copy file operations or just print what will be copied.
        /// </summary>
        public bool CopyFiles;
    }


    class FolderIndex
    {
        bool verbose;
        string dir;
        int files;
        Dictionary<HashedFile, List<HashedFile>> fileIndex = new Dictionary<HashedFile, List<HashedFile>>();
        Dictionary<string, HashedFile> fileToHashMap = new Dictionary<string, HashedFile>(StringComparer.OrdinalIgnoreCase);

        public FolderIndex(string dir, bool verbose=false)
        {
            this.dir = dir;
            this.verbose = verbose;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            // index the files
            CreateIndex(dir);

            watch.Stop();
            Console.WriteLine("Hashed {0} files in {1:N3} seconds", files, (double)watch.ElapsedMilliseconds / 1000.0);
            watch.Reset();
            watch.Start();

            // optimize the index
            OptimizeIndex();

            watch.Stop();
            Console.WriteLine("Optimized the index in {1:N3} seconds", files, (double)watch.ElapsedMilliseconds / 1000.0);
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
            this.fileToHashMap[key.Path] = key;
        }

        private void OptimizeIndex()
        {
            // Now if file length alone is not a good enough hash and we have hash
            // conflicts then re-hash using a 64kb buffer.
            const int hashPrefixLength = 64000;
            if (GetLongestConflict() > 1)
            {
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
                            AddFile(info);
                        }
                    }
                }
            }
        }

        int GetLongestConflict()
        {
            return (from i in fileIndex.Values select i.Count).Max();
        }

        public IEnumerable<List<HashedFile>> FindDuplicates()
        {
            List<HashedFile> duplicates = new List<HashedFile>();

            // ok, now do a deep compare of any files that have identical hashes to see what is really duplicated or not.
            foreach (var pair in fileIndex)
            {
                var list = new List<HashedFile>(pair.Value);
                if (list.Count > 1)
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
                                duplicates.Add(first);
                            }

                            duplicates.Add(other);
                        }
                        else
                        {
                            nondups.Add(other);
                        }
                    }

                    if (foundDup)
                    {
                        yield return duplicates;
                        duplicates.Clear();
                        foundDup = false;
                    }
                    if (nondups.Count > 0)
                    {
                        // now search the remainder for other matches.
                        list = nondups;
                    }
                }
            }
        }

        internal void ResolveDuplicate(HashedFile choice, MergeOptions options)
        {
            List<HashedFile> dups = fileIndex[choice];
            foreach (var item in dups.ToArray())
            {
                if (item.Path != choice.Path && item.DeepEquals(choice))
                {
                    dups.Remove(item);                    
                    if (options.RemoveFiles)
                    {
                        Console.WriteLine("    Deleting " + item.Path);
                        System.IO.File.Delete(item.Path);
                    }
                    else {
                        Console.WriteLine("    Will delete " + item.Path);
                    }
                }
            }
        }

        internal void PickDuplicate(string name, List<HashedFile> files, MergeOptions options)
        {
            // heuristic, if file name ends with (1), (2) or "-1", "-2" and so on, then it is probably a copy paste error, so pick the
            // file name that doesn't contain this suffix.
            // Otherwise pick the longest file name because it is probably the most descriptive.

            string longest = null;
            HashedFile longestFile = null;
            string shortest = null;
            HashedFile shortestFile = null;

            foreach (var item in files)
            {
                string baseName = System.IO.Path.GetFileNameWithoutExtension(item.Path);
                if (longest == null || longest.Length < baseName.Length)
                {
                    longest = baseName;
                    longestFile = item;
                }
                if (shortest == null || shortest.Length > baseName.Length)
                {
                    shortest = baseName;
                    shortestFile = item;
                }
            }

            Regex re = new Regex("[-\\(\\)0-9]*"); // any combination of - ( ) and 0-9.

            bool isIndexed = true;
            // now see if all the names are an indexed (1) or -1 extension on the shortest name.
            foreach (var item in files)
            {
                string baseName = System.IO.Path.GetFileNameWithoutExtension(item.Path);
                if (baseName != shortest)
                {
                    string suffix = baseName.Substring(shortest.Length);

                    var match = re.Match(baseName);
                    if (!match.Success)
                    {
                        isIndexed = false;
                    }
                }
            }

            if (isIndexed)
            {
                Console.WriteLine("Picking " + name + " duplicate: " + shortestFile.Path);
                this.ResolveDuplicate(shortestFile, options);
            }
            else
            {
                Console.WriteLine("Picking " + name + " duplicate: " + longestFile.Path);
                this.ResolveDuplicate(longestFile, options);
            }
        }

        internal void Merge(FolderIndex sourceIndex, MergeOptions options)
        {
            foreach (var pair in sourceIndex.fileIndex)
            {
                // multiple items here means hash clashes, they are actually different files.
                foreach (var item in pair.Value)
                {
                    bool isMetadata = string.Compare(Path.GetExtension(item.Path), ".xmp", StringComparison.OrdinalIgnoreCase) == 0;
                    bool found = false;
                    if (this.fileIndex.ContainsKey(item))
                    {
                        // multiple items in target index also means hash clashes, so we have
                        // to search with deep equals to see if any 2 files actually match.
                        var list = this.fileIndex[item];
                        foreach (var target in list.ToArray())
                        {
                            if (item.HashEquals(target) && item.DeepEquals(target))
                            {
                                if (verbose)
                                {
                                    Console.WriteLine("Matching file already exists in target: {0}", target.Path);
                                    Console.WriteLine("         Matching Source file: {0}", item.Path);
                                }
                                if (!isMetadata)
                                {
                                    TryMergeMetadata(sourceIndex, item, target, options);
                                }
                                if (found)
                                {
                                    throw new Exception("bugbug: still have duplicates in the target index???");
                                }
                                found = true;
                                //this.RemoveFile(target);
                            }
                        }
                    }
                    if (!found)
                    {
                        string target = System.IO.Path.Combine(dir, System.IO.Path.GetFileName(item.Path));
                        if (File.Exists(target))
                        {
                            // bugbug: why wasn't this file in this.fileIndex???
                            // interesting, same file name, so some sort of "merge" operation is needed here.
                            // perhaps one photo was slightly edited, so take the "best" one.  If it is a .xmp
                            // metadata file then perhaps we can merge the metadata.
                            if (isMetadata)
                            {                                
                                TryMergeMetadata(sourceIndex, item, this.FindFile(target), options);
                            }
                            else 
                            {
                                Console.WriteLine("### Conflicting target file name: {0}", target);
                                Console.WriteLine("       Skipping source file name: {0}", item.Path);
                            }
                        }
                        else
                        {
                            Console.WriteLine("copy \"{0}\" \"{1}\"", item.Path, target);
                            if (options.CopyFiles)
                            {
                                File.Copy(item.Path, target);
                                if (options.RemoveFiles)
                                {
                                    File.Delete(item.Path);
                                }
                            }
                        }
                    }
                }
            }

        }

        private void RemoveFile(HashedFile target)
        {
            var list = this.fileIndex[target];
            list.Remove(target);
            if (list.Count == 0)
            {
                this.fileIndex.Remove(target);
            }
        }

        private void TryMergeMetadata(FolderIndex sourceIndex, HashedFile source, HashedFile target, MergeOptions options)
        {
            string sourceMetadata = Path.ChangeExtension(source.Path, ".xmp");
            string targetMetadata = Path.ChangeExtension(target.Path, ".xmp");

            HashedFile sm = sourceIndex.FindFile(sourceMetadata);
            HashedFile tm = this.FindFile(targetMetadata);

            if (sm != null && tm != null)
            {
                if (sm.HashEquals(tm) && sm.DeepEquals(tm))
                {
                    // files match!
                    return;
                }
                else
                {
                    MergeMetadata(sourceMetadata, targetMetadata, options);
                }
            }
            else if (sm != null)
            {
                Console.WriteLine("copy \"{0}\" \"{1}\"", sourceMetadata, targetMetadata);
                if (options.CopyFiles)
                {
                    File.Copy(sourceMetadata, targetMetadata);
                    if (options.RemoveFiles)
                    {
                        File.Delete(sourceMetadata);
                    }
                }
            }
        }

        private void MergeMetadata(string sourceMetadata, string targetMetadata, MergeOptions options)
        {
            Metadata source = Metadata.Load(sourceMetadata);
            Metadata target = Metadata.Load(targetMetadata);
            if (target.Merge(source))
            {
                Console.WriteLine("Merging \"{0}\" into \"{1}\"", sourceMetadata, targetMetadata);
                if (options.CopyFiles)
                {
                    target.Save(targetMetadata);
                    if (options.RemoveFiles)
                    {
                        File.Delete(sourceMetadata);
                    }
                }
            }
            else
            {
                Console.WriteLine("### Conflicting target metada file name: {0}", targetMetadata);
                Console.WriteLine("       Skipping source metada file name: {0}", sourceMetadata);
            }
        }

        private HashedFile FindFile(string path)
        {
            if (File.Exists(path))
            {
                HashedFile h;
                if (this.fileToHashMap.TryGetValue(path, out h))
                {
                    return h;
                }

                AddFile(new HashedFile(path));
                return h;
            }
            return null;
        }
    }
}
