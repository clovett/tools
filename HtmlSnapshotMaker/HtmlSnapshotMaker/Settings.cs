using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlSnapshotMaker
{
    public class Settings
    {
        const string DataFile = "settings.xml";

        public Settings()
        {
        }

        static Settings _instance;

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Settings.Load();
                }
                return _instance;
            }
        }

        public string Url { get; set; }

        public string Directory { get; set; }

        public double IntervalSeconds { get; set; }

        public static Settings Load()
        {
            IsolatedStorage<Settings> f = new IsolatedStorage<Settings>();
            Settings data = f.LoadFromFile(DataFile);
            if (data == null)
            {
                data = new Settings();
            }
            return data;
        }

        public void Save()
        {
            try
            {
                IsolatedStorage<Settings> f = new IsolatedStorage<Settings>();
                f.SaveToFile(DataFile, this);
            }
            catch
            {
            }
        }

    }
}
