using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergePhotos
{
    class FolderIndex
    {
        bool verbose;
        string dir;
        int files;
        Dictionary<HashedFile, List<HashedFile>> fileIndex = new Dictionary<HashedFile, List<HashedFile>>();
        HashSet<string> metadataFiles = new HashSet<string>();

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
                if (System.IO.Path.GetExtension(file).ToLowerInvariant() == ".xmp")
                {
                    metadataFiles.Add(file);
                }
                else
                {
                    files++;
                    HashedFile key = null;
                    key = new HashedFile(file);
                    AddFile(key);
                }
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

        internal void ResolveDuplicate(HashedFile choice)
        {
            List<HashedFile> dups = fileIndex[choice];
            foreach (var item in dups.ToArray())
            {
                if (item.Path != choice.Path && item.DeepEquals(choice))
                {
                    dups.Remove(item);
                }
            }
        }
        internal void Merge(FolderIndex sourceIndex, bool doCopy)
        {
            foreach (var pair in sourceIndex.fileIndex)
            {
                // multiple items here means hash clashes, they are actually different.
                foreach (var item in pair.Value)
                {
                    if (this.fileIndex.ContainsKey(item))
                    {
                        if (verbose)
                        {
                            Console.WriteLine("File already exists in target: {0}", this.fileIndex[item][0].Path);
                            Console.WriteLine("         Matching Source file: {0}", item.Path);
                        }
                        this.fileIndex[item][0].Match = true;
                    }
                    else
                    {
                        string target = System.IO.Path.Combine(dir, System.IO.Path.GetFileName(item.Path));
                        if (File.Exists(target))
                        {
                            // interesting, same file name, so if the photos are similar we could take the largest one, but
                            // if the photos are not similar then we have a name clash!
                            Console.WriteLine("### Conflicting target file name: {0}", target);
                            Console.WriteLine("       Skipping source file name: {0}", item.Path);
                        }
                        else
                        {
                            Console.WriteLine("copy \"{0}\" \"{1}\"", item.Path, target);
                            if (doCopy)
                            {
                                System.IO.File.Copy(item.Path, target);
                            }
                        }
                    }
                }
            }

            foreach (var pair in this.fileIndex)
            {
                if (!pair.Value[0].Match)
                {
                    Console.WriteLine("Not found in target: {0}", pair.Value[0].Path);
                }
            }
        }

    }
}
