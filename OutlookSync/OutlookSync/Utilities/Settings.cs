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

namespace OutlookSync.Utilities
{
    public class Settings : INotifyPropertyChanged
    {
        string fileName;
        List<string> trustedPhones = new List<string>();

        public Settings()
        {
        }

        public List<string> TrustedPhones
        {
            get { return trustedPhones; }
            set { trustedPhones = value; }
        }

        public static async Task<Settings> LoadAsync(string fileName)
        {
            Settings result = new Settings();
            result.fileName = fileName;

            await Task.Run(new Action(() =>
            {
                if (File.Exists(fileName))
                {
                    try
                    {
                        using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            XmlSerializer s = new XmlSerializer(typeof(Settings));
                            result = (Settings)s.Deserialize(stream);
                            result.fileName = fileName;
                        }
                    }
                    catch
                    {
                        // silently ignore errors.
                    }
                }
            }));

            return result;
        }

        public async Task SaveAsync()
        {
            await Task.Run(new Action(() =>
            {
                string dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    XmlWriterSettings settings = new XmlWriterSettings() { Indent = true };
                    XmlSerializer s = new XmlSerializer(typeof(Settings));
                    s.Serialize(stream, this);
                }
            }));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                Application.Current.MainWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }));
            }
        }
    }

}
