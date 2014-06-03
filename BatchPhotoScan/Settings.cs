using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchPhotoScan
{
    public class Settings
    {
        private string photoDir;

        public const string RemoveAdsProductId = "RemoveAdsId";

        const string SettingsFile = "settings.xml";

        public Settings()
        {
            photoDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
        }

        public static string SettingsPath
        {
            get
            {
                string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                string dir = System.IO.Path.Combine(appData, "BatchPhotoScan");
                string path = System.IO.Path.Combine(dir, SettingsFile);
                return path;
            }
        }

        private static Settings instance;

        public static Settings Instance
        {
            get
            {
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        public event EventHandler Changed;

        void OnChanged()
        {
            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        public static Settings LoadSettings()
        {
            IsolatedStorage<Settings> store = new IsolatedStorage<Settings>();
            Settings settings = store.LoadFromFile(SettingsPath);
            if (settings == null)
            {
                settings = new Settings();
            }
            return settings;
        }

        public void SaveSettings()
        {
            try
            {                
                IsolatedStorage<Settings> store = new IsolatedStorage<Settings>();
                store.SaveToFile(SettingsPath, this);
            }
            catch
            {
            }
        }

        public string PhotoDir
        {

            get { return photoDir; }

            set
            {
                if (photoDir != value)
                {
                    photoDir = value;
                    OnChanged();
                }
            }
        }

    }


}
