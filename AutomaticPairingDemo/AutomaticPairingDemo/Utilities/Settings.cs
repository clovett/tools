using Microsoft.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class Settings : INotifyPropertyChanged
    {
        string _deviceAddress;
        string _pinCode;

        static Settings _instance;

        public Settings()
        {
            _instance = this;
        }

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    return new Settings();
                }
                return _instance;
            }
        }


        public string DeviceAddress
        {
            get
            {
                return _deviceAddress;
            }

            set
            {
                if (_deviceAddress != value)
                {
                    _deviceAddress = value;
                    OnPropertyChanged("DeviceAddress");
                }
            }
        }

        public string PinCode
        {
            get
            {
                return _pinCode;
            }

            set
            {
                if (_pinCode != value)
                {
                    _pinCode = value;
                    OnPropertyChanged("PinCode");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public static async Task<Settings> LoadAsync()
        {
            var store = new IsolatedStorage<Settings>();
            Settings result = null;
            try
            {
                result = await store.LoadFromFileAsync(Windows.Storage.ApplicationData.Current.LocalFolder, "settings.xml");
            }
            catch
            {
            }
            if (result == null)
            {
                result = new Settings();
            }
            return result;
        }

        public async Task SaveAsync()
        {
            var store = new IsolatedStorage<Settings>();
            await store.SaveToFileAsync(Windows.Storage.ApplicationData.Current.LocalFolder, "settings.xml", this);
        }

    }
}
