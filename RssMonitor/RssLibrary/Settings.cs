using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssLibrary
{
    public class FeedInfo
    {
        public FeedInfo() { }

        public string Url { get; set; }

        public string Cache { get; set; }

        public DateTime LastUpdated { get; set; }

        public string Error { get; set; }
    }

    public class Settings : INotifyPropertyChanged
    {
        private string smtpHost;
        private int smtpPort;
        private string userName;
        private string password;
        private string fromEmailAddress;
        private string toEmailAddress; 
        private List<FeedInfo> feedInfo;

        const string SettingsFile = "settings.xml";

        public Settings()
        {
        }

        public static string SettingsPath
        {
            get
            {
                string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                string dir = System.IO.Path.Combine(appData, "RssMonitor");
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

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
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
            IsolatedStorage<Settings> store = new IsolatedStorage<Settings>();
            store.SaveToFile(SettingsPath, this);
        }

        public string SmtpHost
        {
            get { return smtpHost; }
            set
            {
                if (smtpHost != value)
                {
                    smtpHost = value;
                    OnPropertyChanged("SmtpHost");
                }
            }
        }

        public int SmtpPort
        {
            get { return smtpPort; }
            set
            {
                if (smtpPort != value)
                {
                    smtpPort = value;
                    OnPropertyChanged("SmtpPort");
                }
            }
        }

        public string UserName
        {
            get { return userName; }
            set
            {
                if (userName != value)
                {
                    userName = value;
                    OnPropertyChanged("UserName");
                }
            }
        }

        public string Password
        {
            get { return password; }
            set
            {
                if (password != value)
                {
                    password = value;
                    OnPropertyChanged("Password");
                }
            }
        }

        public string FromEmailAddress
        {
            get { return fromEmailAddress; }
            set
            {
                if (fromEmailAddress != value)
                {
                    fromEmailAddress = value;
                    OnPropertyChanged("FromEmailAddress");
                }
            }
        }

        public string ToEmailAddress
        {
            get { return toEmailAddress; }
            set
            {
                if (toEmailAddress != value)
                {
                    toEmailAddress = value;
                    OnPropertyChanged("ToEmailAddress");
                }
            }
        }

        public List<FeedInfo> FeedInfo
        {
            get { return feedInfo; }

            set
            {
                if (feedInfo != value)
                {
                    feedInfo = value;
                    OnPropertyChanged("FeedInfo");
                }
            }
        }

    }


}
