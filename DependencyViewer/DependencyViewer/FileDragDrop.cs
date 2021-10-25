using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DependencyViewer {
    public class FileEventArgs : EventArgs {
        string[] files;
        public FileEventArgs(string[] files) {
            this.files = files;
        }
        public string[] FileNames { get { return files; } }
    }

    internal class FileDragDrop {
        string[] formats = new string[] { "FileDrop", "FileNameW" };

        public event EventHandler<FileEventArgs> FilesDropped;

        public FileDragDrop(DependencyObject obj) {
            UIElement e = obj as UIElement;
            if (e != null) {
                e.AllowDrop = true;
                e.DragEnter += new DragEventHandler(OnDragEnter);
                e.DragLeave += new DragEventHandler(OnDragLeave);
                e.DragOver += new DragEventHandler(OnDragOver);
                e.Drop += new DragEventHandler(OnDrop);
                e.GiveFeedback += new GiveFeedbackEventHandler(OnGiveFeedback);
            } else {
                ContentElement ce = obj as ContentElement;
                if (ce != null) {
                    ce.AllowDrop = true;
                    ce.DragEnter += new DragEventHandler(OnDragEnter);
                    ce.DragLeave += new DragEventHandler(OnDragLeave);
                    ce.DragOver += new DragEventHandler(OnDragOver);
                    ce.Drop += new DragEventHandler(OnDrop);
                    ce.GiveFeedback += new GiveFeedbackEventHandler(OnGiveFeedback);
                } else {
                    throw new Exception("Object must be a UIElement or ContentElement");
                }
            }
        }

        void OnGiveFeedback(object sender, GiveFeedbackEventArgs e) {
            
        }

        void OnDrop(object sender, DragEventArgs e) {
            if (!e.Handled && CanReceive(e.Data)) {
                DoFileDrop(e.Data);
                e.Handled = true;
            } 
        }

        void OnDragOver(object sender, DragEventArgs e) {
            if (CanReceive(e.Data)) {
                e.Effects = DragDropEffects.Link;
                e.Handled = true;
            } 
        }

        void OnDragLeave(object sender, DragEventArgs e) {            
        }

        void OnDragEnter(object sender, DragEventArgs e) {
            if (CanReceive(e.Data)) {
                e.Effects = DragDropEffects.Link;
                e.Handled = true;
            }            
        }

        bool CanReceive(IDataObject data) {
            foreach (string f in formats) {
                if (data.GetDataPresent(f))
                    return true;
            }
            return false;
        }

        void DoFileDrop(IDataObject data) {
            if (FilesDropped != null) {                
                foreach (string format in formats) {
                    if (data.GetDataPresent(format)) {
                        string[] list = data.GetData(format) as string[];
                        if (list != null) {
                            FilesDropped(this, new FileEventArgs(list));
                            return;
                        }
                    }
                }
            }
        }

    }
}
