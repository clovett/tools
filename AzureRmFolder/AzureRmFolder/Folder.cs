using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

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

        public void DeleteSubtree(bool preview)
        {
            if (preview)
            {
                Console.WriteLine("Would delete target directory: " + this.location);
            }
            else
            {
                Console.WriteLine("Deleting target directory: " + this.location);
            }

            foreach (string file in this.Files)
            {
                if (preview)
                {
                    Console.WriteLine("Would delete file: " + file);
                }
                else
                {
                    this.DeleteFile(file);
                }
            }

            foreach (string dir in this.ChildFolders)
            {
                Folder sub = this.GetSubfolder(dir);
                sub.DeleteSubtree(preview);
            }

            // now it is empty, we should be able to remove it.
            if (!preview) this.RemoveDirectory();
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

    }
}