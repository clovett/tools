using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace OutlookSyncPhone.Utilities
{
    public class Settings : INotifyPropertyChanged
    {
        string fileName;
        string serverAddress;
        private List<string> licenses;
        public const string RemoveAdsProductId = "OutlookSyncRemoveAdsId";

        public Settings()
        {
        }

        public string ServerAddress
        {
            get { return serverAddress; }
            set { serverAddress = value; }
        }

        /// <summary>
        /// Array of licenses that are owned by this user.
        /// </summary>
        public List<string> Licenses
        {
            get { return this.licenses; }
            set { this.licenses = value; }
        }

        public static async Task<Settings> LoadAsync(string fileName)
        {
            Settings result = null;
            
            IsolatedStorage<Settings> store = new IsolatedStorage<Settings>();
            result = store.LoadFromFile(fileName);
            if (result == null)
            {
                result = new Settings();
            }

            result.fileName = fileName;
            return result;
        }

        public async Task SaveAsync()
        {
            await Task.Run(new Action(() =>
            {
                IsolatedStorage<Settings> store = new IsolatedStorage<Settings>();
                store.SaveToFile(this.fileName, this);
            }));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                Application.Current.RootVisual.Dispatcher.BeginInvoke(new Action(() =>
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }));
            }
        }
    }

}
