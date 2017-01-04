using Microsoft.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NetgearDataUsage.Model
{
    public class Settings : INotifyPropertyChanged
    {
        static Settings _instance;
        string fileName;
        string accessToken;
        int targetUsage = 1024;
        string trafficMeterUri = "http://192.168.1.1/traffic_meter.htm";

        private Settings()
        {
            _instance = this;
        }

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Settings();
                }
                return _instance;
            }
        }
        
        public string FileName
        {
            get { return this.fileName; }
            set
            {
                if (this.fileName != value)
                {
                    this.fileName = value;
                    OnPropertyChanged("FileName");
                }
            }
        }

        public string TrafficMeterUri
        {
            get { return this.trafficMeterUri; }
            set
            {
                if (this.trafficMeterUri != value)
                {
                    this.trafficMeterUri = value;
                    OnPropertyChanged("TrafficMeterUri");
                }
            }
        }

        public string FileAccessToken
        {
            get { return this.accessToken; }
            set
            {
                if (this.accessToken != value)
                {
                    this.accessToken = value;
                    OnPropertyChanged("FileAccessTokenName");
                }
            }
        }

        public int TargetUsage
        {
            get { return this.targetUsage; }
            set
            {
                if (this.targetUsage != value)
                {
                    this.targetUsage = value;
                    OnPropertyChanged("TargetUsage");
                }
            }
        }

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<Settings> LoadAsync()
        {
            var store = new IsolatedStorage<Settings>();
            Settings result = await store.LoadFromFolderAsync(Windows.Storage.ApplicationData.Current.LocalFolder, "settings.xml");
            if (result == null)
            {
                result = new Settings();
                await result.SaveAsync();
            }
            return result;
        }

        public async Task SaveAsync()
        {
            var store = new IsolatedStorage<Settings>();
            await store.SaveToFolderAsync(Windows.Storage.ApplicationData.Current.LocalFolder, "settings.xml", this);
        }

    }

}
