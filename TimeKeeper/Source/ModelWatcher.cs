//-----------------------------------------------------------------------
// <copyright file="ModelWatcher.cs" company="Lovett Software">
//   (c) Lovett Software.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace TimeKeeper
{
    /// <summary>
    /// Watches for changes of the model file on disk and reloads it when necessary.
    /// </summary>
    class ModelWatcher : IDisposable
    {
        FileSystemWatcher watcher;
        Dispatcher dispatcher;
        DispatcherTimer timer;
        bool fileChanged;
        TaskModel model;

        public ModelWatcher(TaskModel model) 
        {
            this.model = model;
            model.Loaded += new EventHandler(OnModelLoaded);
            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, new EventHandler(OnTick), dispatcher);
        }

        void OnModelLoaded(object sender, EventArgs e)
        {
            if (watcher != null)
            {
                watcher.Changed -= new FileSystemEventHandler(OnFileChanged);
                watcher = null;
            }

            if (!string.IsNullOrEmpty(model.FileName))
            {
                watcher = new FileSystemWatcher(Path.GetDirectoryName(model.FileName), "*.xml");
                watcher.Changed += new FileSystemEventHandler(OnFileChanged);
                watcher.EnableRaisingEvents = true;
            }
        }

        void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath == model.FileName)
            {
                timer.Start();
                fileChanged = true;
            }
        }

        void OnTick(object sender, EventArgs e)
        {
            if (fileChanged)
            {
                if (model.LastWriteTime < File.GetLastWriteTime(model.FileName))
                {
                    // someone updated it from another machine.
                    model.Reload();
                }
                fileChanged = false;
            }
        }

        ~ModelWatcher()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            using (watcher)
            {
                if (watcher != null)
                {
                    watcher.Changed -= new FileSystemEventHandler(OnFileChanged);
                    watcher = null;
                }
            }
        }
    }
}
