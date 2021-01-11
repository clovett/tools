using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace Walkabout.Utilities
{
    /// <summary>
    /// This class unifies an FTP directory or a local directory so we can handle
    /// both with one abstraction.
    /// </summary>
    public class Folder
    {
        public List<string> Files { get; }
        public List<string> Subfolders { get; }

        private string location;
        private bool isFtp;
        private string user;
        private string password;

        /// <summary>
        /// Load up the listing for a given location
        /// </summary>
        /// <param name="location"> either local or FTP location</param>
        /// <param name="user">Required for FTP locations</param>
        /// <param name="password">Required for FTP locations</param>
        public Folder(string location, string user, string password)
        {
            this.location = location;
            this.Files = new List<string>();
            this.Subfolders = new List<string>();
            this.user = user;
            this.password = password;

            Uri uri = new Uri(location);
            if (uri.Scheme == "ftp")
            {
                this.isFtp = true;
                LoadFtpDirectoryInfo(location, user, password);
            }
            else
            {
                LoadLocalDirectoryInfo(location);
            }
        }

        public override string ToString()
        {
            return this.location;
        }

        private void LoadLocalDirectoryInfo(string location)
        {
            foreach (string fileName in Directory.GetFiles(location))
            {
                string name = Path.GetFileName(fileName);
                this.Files.Add(name);
            }
            foreach (string dir in Directory.GetDirectories(location))
            {
                string name = Path.GetFileName(dir);
                this.Subfolders.Add(name);
            }
        }

        private void LoadFtpDirectoryInfo(string target, string user, string password)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(target);
            request.Credentials = new NetworkCredential(user, password);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            FtpWebResponse response = null;

            try
            {
                using (response = (FtpWebResponse)request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string line = reader.ReadLine();
                        while (line != null)
                        {
                            // format: "08-21-12  08:12PM       <DIR>          Icons"
                            int i = line.IndexOf("<DIR>");
                            if (i > 0)
                            {
                                string name = line.Substring(39);
                                this.Subfolders.Add(name);
                            }
                            else
                            {
                                string name = line.Substring(39);
                                this.Files.Add(name);
                            }
                            line = reader.ReadLine();
                        }
                    }
                }
            }
            catch
            {
                // doesn't exist.
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
            foreach (string dir in this.Subfolders)
            {
                string name = Path.GetFileName(dir);
                target.EnsureDirectory(name);

                Folder subfolder = new Folder(target.GetFullPath(name), this.user, this.password);
                Folder srcFolder = new Folder(this.GetFullPath(name), this.user, this.password);
                srcFolder.CopySubtree(subfolder);
            }
        }

        private string GetFullPath(string name)
        {
            if (this.isFtp)
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
            if (!this.Subfolders.Contains(name, StringComparer.OrdinalIgnoreCase))
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
            if (this.isFtp)
            {
                // this requires a download
                if (target.isFtp)
                {
                    // this requires a local cache.
                }
                else
                {
                    this.DownloadFiles(target);
                }
            }
            else
            {
                if (target.isFtp)
                {
                    this.UploadFiles(target);
                }
            }
        }

        private void UploadFiles(Folder target)
        {
            foreach (string fileName in this.Files)
            {
                string name = Path.GetFileName(fileName);
                Console.Write("Uploading file: " + name + " ...");

                try
                {
                    string address = target.GetFullPath(name);
                    string localFile = this.GetFullPath(name);

                    WebClient client = new WebClient();
                    client.Credentials = new NetworkCredential(user, password);
                    client.UploadFile(address, localFile);

                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.WriteLine("### Error : " + e.Message);
                }
            }
        }

        private void DownloadFiles(Folder target)
        {
            foreach (string name in this.Files)
            {
                Console.Write("Downloading file: " + name + " ...");

                try
                {
                    var address = this.GetFullPath(name);
                    string localName = target.GetFullPath(name);
                    WebClient client = new WebClient();
                    client.Credentials = new NetworkCredential(user, password);
                    client.DownloadFile(address, localName);
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

            foreach (string dir in this.Subfolders)
            {
                Folder sub = new Folder(this.GetFullPath(dir), this.user, this.password);
                sub.DeleteSubtree();
            }

            // now it is empty, we should be able to remove it.
            this.RemoveDirectory();
        }

        private void RemoveDirectory()
        {
            try
            {
                if (this.isFtp)
                {
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(this.location);
                    request.Credentials = new NetworkCredential(user, password);
                    request.Method = WebRequestMethods.Ftp.RemoveDirectory;
                    using (var response = (FtpWebResponse)request.GetResponse())
                    {
                        // "250 RMD command successful.\r\n"
                        var state = response.StatusDescription;
                        if (response.StatusCode != FtpStatusCode.FileActionOK)
                        {
                            throw new IOException("### failed to delete target: " + this.location + "\r\n" + state);
                        }
                    }
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
            if (this.isFtp)
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fullPath);
                request.Credentials = new NetworkCredential(user, password);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    // "250 DELE command successful.\r\n"
                    var state = response.StatusDescription;
                    if (response.StatusCode != FtpStatusCode.FileActionOK)
                    {
                        throw new IOException("### failed to delete target: " + fullPath + "\r\n" + state);
                    }
                    Console.WriteLine("Deleted " + fullPath);
                }
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
        /// Copies all files and directories from the source directory up to the target FTP directory
        /// using the given user name and password and deletes any files or directories at the target
        /// that do not exist in the source.
        /// </summary>
        /// <param name="source">Source directory containing files and directories to be copied</param>
        /// <param name="target">Target directory relative to FTP server</param>
        /// <param name="user">The FTP user name</param>
        /// <param name="password">The FTP password</param>
        /// <returns>The number of files that failed to upload</returns>
        public void MirrorDirectory(Folder target)
        {
            // depth first
            foreach (string name in this.Subfolders)
            {
                if (!target.Subfolders.Contains(name))
                {
                    target.EnsureDirectory(name);
                }

                Folder subTarget = new Folder(target.GetFullPath(name), this.user, this.password);
                Folder subSource = new Folder(this.GetFullPath(name), this.user, this.password);

                subSource.MirrorDirectory(subTarget);
            }

            // Delete stale subdirectories that should no longer exist on the target.
            foreach (string staleDir in target.Subfolders)
            {
                if (!this.Subfolders.Contains(staleDir))
                {
                    Folder subTarget = new Folder(target.GetFullPath(staleDir), this.user, this.password);
                    subTarget.DeleteSubtree();
                }
            }

            Console.WriteLine("Mirroring directory: " + target);
            this.MirrorFiles(target);
        }

        private void MirrorFiles(Folder target)
        {
            this.CopyFilesTo(target);

            // Delete the stale files that should no longer be on the server.
            foreach (string old in target.Files)
            {
                if (!this.Files.Contains(old, StringComparer.OrdinalIgnoreCase))
                {
                    target.DeleteFile(old);
                }
            }
        }

        private void CreateTargetDirectory(string fullPath)
        {
            if (this.isFtp)
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fullPath);
                request.Credentials = new NetworkCredential(this.user, this.password);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    // 257 "MyMoney/download//Application Files/MyMoney_1_0_0_198" directory created.
                    if (response.StatusCode != FtpStatusCode.PathnameCreated)
                    {
                        throw new IOException("### failed to create target directory: " + fullPath + "\r\n" + response.StatusDescription);
                    }
                }
                return;
            }
            else
            {
                Directory.CreateDirectory(fullPath);
            }
        }

    }
}