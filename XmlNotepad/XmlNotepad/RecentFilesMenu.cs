using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace XmlNotepad {
    public class RecentFilesMenu {
        List<Uri> recentFiles = new List<Uri>();
        const int maxRecentFiles = 10;
        ToolStripMenuItem parent;

        public event EventHandler RecentFileSelected;

        public RecentFilesMenu(ToolStripMenuItem parent) {
            this.parent = parent;
        }

        public Uri[] ToArray() {
            return recentFiles.ToArray();
        }

        public void Clear() {
            recentFiles.Clear();
        }

        public void SetFiles(Uri[] files) {
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
            if (this.recentFiles.Count == 0) {
                this.parent.Visible = false;
                return;
            }
            this.parent.Visible = true;
            this.parent.Enabled = true;
            
            ToolStripItemCollection ic = this.parent.DropDownItems;
            // Add most recent files first.
            for (int i = this.recentFiles.Count-1, j = 0; i >= 0; i--, j++) {
                ToolStripItem item = null;
                if (ic.Count > j) {
                    item = ic[j];
                } else {
                    item = new ToolStripMenuItem();
                    item.Click += new EventHandler(OnRecentFile);
                    ic.Add(item);
                }
                Uri uri = this.recentFiles[i];
                item.Text = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;  
            }

            // Remove any extra menu items.
            for (int i = ic.Count - 1, n = this.recentFiles.Count; i > n; i--) {
                ic.RemoveAt(i);
            }
        }

        void OnRecentFile(object sender, EventArgs e) {
            if (this.RecentFileSelected != null) {
                this.RecentFileSelected(sender, e);
            }
        }

    }
}
