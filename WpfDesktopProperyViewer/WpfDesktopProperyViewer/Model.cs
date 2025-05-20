using LovettSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WpfDesktopProperyViewer
{
    public class Model : NotifyingObject
    {
        public Model()
        {
            Entities = new ObservableCollection<Entity>();
        }

        [XmlIgnore]
        public ObservableCollection<Entity> Entities { get; set; }

        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        const string ModelFileName = "settings.xml";
        const string ModelPath = @"Microsoft\WpfDesktopPropertyViewer";

        public static string ModelStorageFolder
        {
            get
            {
                string appSetttingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ModelPath);
                Directory.CreateDirectory(appSetttingsPath);
                return appSetttingsPath;
            }
        }

        internal static Model Load()
        {
            var store = new IsolatedStorage<Model>();
            Model result = null;
            try
            {
                Debug.WriteLine("Loading settings from : " + ModelStorageFolder);
                result = store.LoadFromFile(ModelStorageFolder, ModelFileName);
            }
            catch
            {
            }
            if (result == null)
            {
                result = new Model();
            }
            return result;
        }

        bool saving;

        public void Save()
        {
            var store = new IsolatedStorage<Model>();
            if (!saving)
            {
                saving = true;
                try
                {
                    Debug.WriteLine("Saving settings to : " + ModelStorageFolder);
                    store.SaveToFile(System.IO.Path.Combine(ModelStorageFolder, ModelFileName), this);
                }
                finally
                {
                    saving = false;
                }
            }
        }
    }

    public class  Entity : NotifyingObject
    {
        private string _name;
        private string _value;

        public Entity() { }

        public string Name
        {
            get { return (string)_name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        public string Value
        {
            get { return (string)_value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    NotifyPropertyChanged("Value");
                }
            }
        }
    }
}
