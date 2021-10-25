using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace DependencyViewer
{
    public class RecentFileEventArgs : EventArgs
    {
        Uri uri;
        public RecentFileEventArgs(Uri file)
        {
            this.uri = file;
        }
        public Uri Uri { get { return uri; } }
    }

    public class RecentFilesMenu {
        List<Uri> recentFiles = new List<Uri>();
        const int maxRecentFiles = 10;
        MenuItem parent;

        public event EventHandler<RecentFileEventArgs> RecentFileSelected;

        public RecentFilesMenu(MenuItem parent)
        {
            this.parent = parent;
        }

        public List<Uri> RecentFiles { get { return new List<Uri>(recentFiles); } }

        public void Clear() {
            recentFiles.Clear();
        }

        public void SetFiles(List<Uri> files) {
            Clear();
            foreach (Uri fileName in files) {
                AddRecentFileName(fileName);
            }
            SyncRecentFilesMenu();
        }

        void AddRecentFileName(Uri fileName) {
            try {
                if (this.recentFiles.Contains(fileName)) {
                    this.recentFiles.Remove(fileName);
                }
                if (fileName.IsFile && !File.Exists(fileName.LocalPath)) {
                    return; // ignore deleted files.
                }
                this.recentFiles.Add(fileName);
                if (this.recentFiles.Count > maxRecentFiles) {
                    this.recentFiles.RemoveAt(0);
                }
            } catch (System.UriFormatException) {
                // ignore bad file names
            } catch (System.IO.IOException) {
                // ignore bad files
            }
        }

        public void AddRecentFile(Uri fileName) {
            AddRecentFileName(fileName);
            SyncRecentFilesMenu();
        }

        void SyncRecentFilesMenu() {
            // Synchronize menu items.            
            ItemCollection ic = this.parent.Items;
            // Add most recent files first.
            int index = 1;
            for (int i = this.recentFiles.Count-1, j = 0; i >= 0; i--, j++) {
                MenuItem item = null;
                if (ic.Count > j) {
                    item = ic[j] as MenuItem;
                } else {
                    item = new MenuItem();
                    item.Click += new RoutedEventHandler(OnRecentFile);
                    ic.Add(item);
                }
                Uri uri = this.recentFiles[i];
                item.Tag = uri;
                item.Header = "_" + index.ToString() + " " + (uri.IsFile ? uri.LocalPath : uri.AbsoluteUri);  
                index++;
            }

            // Remove any extra menu items.
            for (int i = ic.Count - 1, n = this.recentFiles.Count; i > n; i--) {
                ic.RemoveAt(i);
            }
        }

        void OnRecentFile(object sender, RoutedEventArgs e) {
            MenuItem item = (MenuItem)sender;
            if (this.RecentFileSelected != null) {
                this.RecentFileSelected(sender, new RecentFileEventArgs((Uri)item.Tag));
            }
        }

    }
}
