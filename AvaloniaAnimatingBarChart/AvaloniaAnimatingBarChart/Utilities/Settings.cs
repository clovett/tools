﻿using Avalonia;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace AvaloniaAnimatingBarChart.Utilities
{
    public enum AppTheme
    {
        Light,
        Dark
    }


    public class Settings : NotifyingObject, IXmlSerializable
    {
        const string SettingsFileName = "settings.xml";
        const string SettingsPath = @"LovettSoftware\AvaloniaAnimatingBarChart";

        string fileName;
        PixelPoint windowLocation;
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
                string appSetttingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SettingsPath);
                Directory.CreateDirectory(appSetttingsPath);
                return appSetttingsPath;
            }
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

        public PixelPoint WindowLocation
        {
            get { return this.windowLocation; }
            set
            {
                if (this.windowLocation != value)
                {
                    this.windowLocation = value;
                    NotifyPropertyChanged("WindowLocation");
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
                    NotifyPropertyChanged("WindowSize");
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
                    NotifyPropertyChanged("Theme");
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
                    NotifyPropertyChanged("LastFile");
                }
            }
        }

        internal static Settings Load()
        {
            var store = new IsolatedStorage<Settings>();
            Settings result = null;
            try
            {
                Debug.WriteLine("Loading settings from : " + SettingsFolder);
                result = store.LoadFromFile(SettingsFolder, SettingsFileName);
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
            return result;
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

        public void Save()
        {
            var store = new IsolatedStorage<Settings>();
            if (!saving)
            {
                saving = true;
                try
                {
                    Debug.WriteLine("Saving settings to : " + SettingsFolder);
                    store.SaveToFile(System.IO.Path.Combine(SettingsFolder, SettingsFileName), this);
                }
                finally
                {
                    saving = false;
                }
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.Read();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                switch(reader.LocalName)
                {
                    case "WindowLocation":
                        try
                        {
                            var location = reader.ReadElementContentAsString();
                            this.windowLocation = PixelPoint.Parse(location);
                        } catch (FormatException) { }
                        break;
                    case "WindowSize":
                        try
                        {
                            var size = reader.ReadElementContentAsString();
                            this.windowSize = Size.Parse(size);
                        }
                        catch (Exception) { }
                        break;
                    case "Theme":
                        try
                        {
                            var s = reader.ReadElementContentAsString();
                            this.theme = Enum.Parse<AppTheme>(s);
                        }
                        catch (Exception) { }
                        break;
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString("WindowLocation", this.windowLocation.ToString());
            writer.WriteElementString("WindowSize", this.windowSize.ToString());
            writer.WriteElementString("Theme", this.theme.ToString());
        }
    }
}
