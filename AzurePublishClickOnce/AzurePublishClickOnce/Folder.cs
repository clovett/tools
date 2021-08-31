using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Azure.Storage.Blobs;

namespace Walkabout.Utilities
{
    /// <summary>
    /// This class unifies an Azure blob folder abstraction with local file system
    /// so we can easily sync the two.
    /// </summary>
    public class Folder
    {
        public List<string> Files { get; }
        public List<string> ChildFolders { get; }
        private Dictionary<string, Folder> Subfolders { get; }

        private string location;
        private BlobContainerClient client;
        private string containerName;
        private string name;

        /// <summary>
        /// Load up the listing for a given location, if the location starts with
        /// https:// then it is the Azure blob store path.
        /// </summary>
        public Folder(string location)
        {
            this.location = location;
            this.Files = new List<string>();
            this.ChildFolders = new List<string>();
            this.Subfolders = new Dictionary<string, Folder>();

            Uri uri = new Uri(location);
            if (uri.Scheme != "file")
            {
                throw new Exception("This should be a local folder");
            }
            else
            {
                LoadLocalDirectoryInfo();
            }
        }

        public Folder(string location, string connectionString) 
        {
            this.location = location;
            this.Files = new List<string>();
            this.ChildFolders = new List<string>();
            this.Subfolders = new Dictionary<string, Folder>();

            var parts = location.Split('/');
            this.containerName = parts[0];
            this.location = string.Join('/', parts.Skip(1));

            this.client = new BlobContainerClient(connectionString, containerName);
            LoadBlobDirectoryInfo();
        }

        private Folder(string location, BlobContainerClient client) 
        {
            this.location = location;
            this.Files = new List<string>();
            this.ChildFolders = new List<string>();
            this.Subfolders = new Dictionary<string, Folder>();

            if (client != null) 
            { 
                this.client = client;
                // this is only used by LoadBlobDirectoryInfo so it is in the process
                // of loading this new folder.
            }
            else
            {
                LoadLocalDirectoryInfo();
            }
        }

        public override string ToString()
        {
            return this.location;
        }

        private void LoadLocalDirectoryInfo()
        {
            foreach (string fileName in Directory.GetFiles(location))
            {
                string name = Path.GetFileName(fileName);
                this.Files.Add(name);
            }
            foreach (string dir in Directory.GetDirectories(location))
            {
                string name = Path.GetFileName(dir);
                this.ChildFolders.Add(name);
            }

        }

        private void LoadBlobDirectoryInfo()
        {
            int prefixLength = this.location.Length;
            var blobs = client.GetBlobs(prefix: this.location + "/");
            this.name = this.location;
            foreach (var item in blobs)
            {
                var path = item.Name.Substring(prefixLength).Trim('/');
                string[] parts = path.Split('/');
                if (parts.Length > 1)
                {
                    // sub folder
                    this.AddSubFolder(new List<string>(parts));
                } 
                else
                {
                    this.Files.Add(path);
                }
            }
        }

        private void AddSubFolder(List<string> parts)
        {
            var name = parts[0];
            Folder folder = null;
            this.Subfolders.TryGetValue(name, out folder);
            if (folder == null) { 
                this.ChildFolders.Add(name);

                folder = new Folder(this.location + "/" + name, this.client);
                folder.name = name;
                folder.containerName = this.containerName;
                this.Subfolders[name] = folder;
            }

            parts.RemoveAt(0);
            
            if (parts.Count > 1)
            {
                folder.AddSubFolder(parts);
            }
            else
            {
                // terminate the recurrsion at a file name.
                folder.Files.Add(parts[0]);
            }
        }

        public bool HasSubfolder(string name)
        {
            if (this.client != null) {
                Folder folder = null;
                return this.Subfolders.TryGetValue(name, out folder);
            }
            else
            {
                return Directory.Exists(this.GetFullPath(name));
            }
        }

        public Folder GetSubfolder(string name)
        {
            if (this.client != null)
            {
                Folder folder = null;
                if (!this.Subfolders.TryGetValue(name, out folder)) 
                {
                    folder = new Folder(this.location + "/" + name, this.client);
                    folder.name = name;
                    folder.containerName = this.containerName;
                }
                return folder;
            }
            else
            { 
                // must be local file system.
                return new Folder(this.GetFullPath(name));
            }
        }

        /// <summary>
        /// Copies all files and directories from the source directory to the FTP directory
        /// using the given user name and password.
        /// </summary>
        /// <param name="source">Source directory containing files and directories to be copied</param>
        /// <param name="target">Target directory relative to FTP server</param>
        /// <returns>Throws exception on failure</returns>
        public void CopySubtree(Folder target)
        {
            Console.WriteLine("Copying {0} ===> {1}", this.location, target.location);
            this.CopyFilesTo(target);

            // Traverse directory hierarchy...
            foreach (string dir in this.ChildFolders)
            {
                string name = Path.GetFileName(dir);
                target.EnsureDirectory(name);

                Folder subfolder = target.GetSubfolder(name);
                Folder srcFolder = this.GetSubfolder(name);
                srcFolder.CopySubtree(subfolder);
            }
        }

        private string GetFullPath(string name)
        {
            if (this.client != null)
            {
                if (this.location.EndsWith("/"))
                {
                    return this.location + name;
                }
                return this.location + "/" + name;
            }
            else
            {
                return Path.Combine(this.location, name);
            }
        }

        private void EnsureDirectory(string name)
        {
            if (!this.ChildFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                this.CreateTargetDirectory(this.GetFullPath(name));
            }
        }

        /// <summary>
        /// Copy the files from this source directory to the target directory.
        /// </summary>
        /// <param name="source">Target directory</param>
        /// <returns>The file names that were found and uploaded</returns>
        private void CopyFilesTo(Folder target)
        {
            if (this.client != null)
            {
                // this requires a download
                if (target.client != null)
                {
                    // this requires a local cache.
                }
                else
                {
                    target.DownloadFiles(this);
                }
            }
            else
            {
                if (target.client != null)
                {
                    target.UploadFiles(this);
                }
            }
        }

        private void UploadFiles(Folder source)
        {
            foreach (string fileName in source.Files)
            {
                string name = Path.GetFileName(fileName);
                Console.Write("Uploading file: " + name + " ...");

                try
                {
                    string address = this.GetFullPath(name);
                    string localFile = source.GetFullPath(name);
                    var blob = this.client.GetBlobClient(address);
                    using var stream = File.OpenRead(localFile);
                    blob.Upload(stream, overwrite: true);
                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.WriteLine("### Error : " + e.Message);
                }
            }
        }

        private void DownloadFiles(Folder source)
        {
            foreach (string name in this.Files)
            {
                Console.Write("Downloading file: " + name + " ...");

                try
                {
                    var address = this.GetFullPath(name);
                    string localFile = source.GetFullPath(name);

                    var blob = this.client.GetBlobClient(address);
                    using var stream = File.OpenWrite(localFile);
                    blob.DownloadTo(stream);

                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.WriteLine("### Error : " + e.Message);
                }
            }
        }

        public void DeleteSubtree()
        {
            Console.WriteLine("Deleting target directory: " + this.location);

            foreach (string file in this.Files)
            {
                this.DeleteFile(file);
            }

            foreach (string dir in this.ChildFolders)
            {
                Folder sub = this.GetSubfolder(dir);
                sub.DeleteSubtree();
            }

            // now it is empty, we should be able to remove it.
            this.RemoveDirectory();
        }

        private void RemoveDirectory()
        {
            try
            {
                if (this.client != null)
                {
                    // Azure doesn't really have "directories" so nothing to do here.
                }
                else
                {
                    Directory.Delete(this.location);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("### error removing target directory {0}", this.location);
                Console.WriteLine(ex.Message);
            }
        }

        public void DeleteFile(string name)
        {
            string fullPath = this.GetFullPath(name);
            if (this.client != null)
            {
                this.client.DeleteBlob(fullPath);
            } 
            else
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    Console.WriteLine("Deleted " + fullPath);
                }
            }
        }

        /// <summary>
        /// Copies all files and directories from the source directory up to the target directory
        /// in two passes.  The first pass adds existing files and the second pass deletes any files 
        /// or directories at the target that do not exist in the source.
        /// </summary>
        /// <param name="target">Target directory relative to FTP server</param>
        /// <param name="addPass">When true adds the missing files, when false does the cleanup pass d
        /// deleting files from the target that should no longer be there.</param>
        public void MirrorDirectory(Folder target, bool addPass)
        {
            // depth first
            foreach (string name in this.ChildFolders)
            {
                if (addPass && !target.ChildFolders.Contains(name))
                {
                    target.EnsureDirectory(name);
                }

                Folder subTarget = target.GetSubfolder(name);
                Folder subSource = this.GetSubfolder(name);

                subSource.MirrorDirectory(subTarget, addPass);
            }

            if (addPass)
            {
                Console.WriteLine("Mirroring directory: " + target);
            }
            else
            {
                Console.WriteLine("Cleaning up directory: " + target);
            }

            this.MirrorFiles(target, addPass);

            if (!addPass)
            {
                // Delete stale subdirectories that should no longer exist on the target.
                foreach (string staleDir in target.ChildFolders)
                {
                    if (!this.ChildFolders.Contains(staleDir))
                    {
                        Folder subTarget = target.GetSubfolder(staleDir);
                        subTarget.DeleteSubtree();
                    }
                }
            }
        }

        private void MirrorFiles(Folder target, bool addPass)
        {
            if (addPass)
            {
                this.CopyFilesTo(target);
            }
            else
            {
                // Delete the stale files that should no longer be on the server.
                foreach (string old in target.Files)
                {
                    if (!this.Files.Contains(old, StringComparer.OrdinalIgnoreCase))
                    {
                        target.DeleteFile(old);
                    }
                }
            }
        }

        private void CreateTargetDirectory(string fullPath)
        {
            if (this.client != null)
            {
                // directories do not need to be created in azure blob store.
                return;
            }
            else
            {
                Directory.CreateDirectory(fullPath);
            }
        }

    }
}