using Microsoft.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VideoThumbnailMaker.Utilities
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public class Settings : INotifyPropertyChanged
    {
        const string SettingsFileName = "settings.xml";
        string fileName;
        int width = 100;
        Point windowLocation;
        Size windowSize;
        AppTheme theme = AppTheme.Dark;

        static Settings _instance;

        public Settings()
        {
            _instance = this;
        }

        public static string SettingsFolder
        {
            get
            {
                string appSetttingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\VideoThumbnailMNaker");
                Directory.CreateDirectory(appSetttingsPath);
                return appSetttingsPath;
            }
        }

        public static Settings Instance
        {
            get
            {
                return _instance;
            }
        }

        public static event EventHandler Loaded;

        public Point WindowLocation
        {
            get { return this.windowLocation; }
            set {
                if (this.windowLocation != value)
                {
                    this.windowLocation = value;
                    OnPropertyChanged("WindowLocation");
                }
            }
        }

        public int ThumbnailWidth
        {
            get { return this.width; }
            set {
                if (this.width != value)
                {
                    this.width = value;
                    OnPropertyChanged("ThumbnailWidth");
                }
            }
        }

        public Size WindowSize
        {
            get { return this.windowSize; }
            set
            {
                if (this.windowSize != value)
                {
                    this.windowSize = value;
                    OnPropertyChanged("WindowSize");
                }
            }
        }

        public AppTheme Theme
        {
            get { return this.theme; }
            set
            {
                if (this.theme != value)
                {
                    this.theme = value;
                    OnPropertyChanged("Theme");
                }
            }
        }

        public string LastFile
        {
            get
            {
                return this.fileName;
            }
            set
            {
                if (this.fileName != value)
                {
                    this.fileName = value;
                    OnPropertyChanged("LastFile");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                UiDispatcher.RunOnUIThread(() =>
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                });
            }
        }

        public static async Task<Settings> LoadAsync()
        {
            var store = new IsolatedStorage<Settings>();
            Settings result = null;
            try
            {
                Debug.WriteLine("Loading settings from : " + SettingsFolder);
                result = await store.LoadFromFileAsync(SettingsFolder, SettingsFileName);
            }
            catch
            {
            }
            if (result == null)
            {
                result = new Settings();
                await result.SaveAsync();
            }
            result.OnLoaded();
            return result;
        }

        private void OnLoaded()
        {
            if (Loaded != null)
            {
                Loaded(this, EventArgs.Empty);
            }
        }

        bool saving;

        public async Task SaveAsync()
        {
            var store = new IsolatedStorage<Settings>();
            if (!saving)
            {
                saving = true;
                try
                {
                    Debug.WriteLine("Saving settings to : " + SettingsFolder);
                    await store.SaveToFileAsync(SettingsFolder, SettingsFileName, this);
                }
                finally
                {
                    saving = false;
                }
            }
        }

    }


}
