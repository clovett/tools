using System;
using System.Collections.Generic;
using IList = System.Collections.IList;
using System.ComponentModel;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace DependencyViewer {
    public class SettingChangedEventArgs : EventArgs {
        string name;
        object value;
        public SettingChangedEventArgs(string name, object value) {
            this.name = name;
            this.value = value;
        }
        public string Name { get { return this.name; } }
        public object NewValue { get { return this.value; } }
    }

    public class Settings {
        IDictionary<string, object> settings = new Dictionary<string, object>();

        public event EventHandler<SettingChangedEventArgs> Changed;

        public bool HasValue(string name) {
            return settings.ContainsKey(name);
        }

        public object this[string name] {
            get {
                if (settings.ContainsKey(name)) {
                    return settings[name];
                }
                return null;
            }
            set {
                settings[name] = value;
                OnChanged(name, value);
            }
        }

        void OnChanged(string name, object value)
        {
            if (Changed != null)
            {
                Changed(this, new SettingChangedEventArgs(name, value));
            }
        }

        public void Load(string filename) {
            if (File.Exists(filename)) {
                try {
                    using (XmlReader r = XmlReader.Create(filename)) {
                        r.Read();
                        r.MoveToElement();
                        if (r.Name == "Settings") {
                            while (r.Read() && !r.EOF) {
                                if (r.NodeType == XmlNodeType.Element) {
                                    string key = r.Name;
                                    Type t = typeof(string);
                                    if (settings.ContainsKey(key)) {
                                        t = settings[key].GetType();
                                    }
                                    Type listType = typeof(IList);
                                    if (listType.IsAssignableFrom(t)) {
                                        LoadList(r, settings[key] as IList);
                                        OnChanged(key, settings[key]);
                                    } else {
                                        r.Read();
                                        string value = r.ReadString();
                                        this[key] = ConvertToObject(t, value);
                                    }
                                }
                            }
                        }
                    }
                } catch {
                    // ignore errors.
                }
            }
        }

        void LoadList(XmlReader r, IList list) {
            Type elementType = typeof(string);
            Type listType = list.GetType();
            if (listType.IsGenericType) {
                Type[] targs = listType.GetGenericArguments();
                Debug.Assert(targs.Length == 1);
                if (targs.Length == 1) {
                    elementType = targs[0];
                } 
            }
            list.Clear();
            if (r.IsEmptyElement)
            {
                return;
            }
            while (r.Read() && !r.EOF && r.NodeType != XmlNodeType.EndElement) {
                if (r.NodeType == XmlNodeType.Element) {
                    // <item>value</item>
                    string value = r.ReadString();
                    object tv = ConvertToObject(elementType, value);
                    list.Add(tv);
                    r.Read(); // consume item end tag.
                }
            }
                            
        }

        public void Save(string filename) {
            SortedList<string, object> sorted = new SortedList<string, object>();
            foreach (string key in settings.Keys) {
                sorted.Add(key, settings[key]);
            }

            string path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            using (XmlTextWriter w = new XmlTextWriter(filename, Encoding.UTF8)) {
                w.Formatting = Formatting.Indented;
                w.WriteStartElement("Settings");
                foreach (KeyValuePair<string,object> key in sorted) {
                    w.WriteStartElement(key.Key);
                    object value = key.Value;
                    if (value is IList) {
                        IList list = (IList)value;
                        foreach (object o in list) {
                            w.WriteStartElement("item");
                            w.WriteString(ConvertToString(o));
                            w.WriteEndElement();
                        }
                    } else {
                        w.WriteString(ConvertToString(value));
                    }
                    w.WriteEndElement();
                }
                w.Close();
            }
        }

        string ConvertToString(object v) {
            TypeConverter c = TypeDescriptor.GetConverter(v.GetType());
            if (c != null) {
                return c.ConvertToInvariantString(v);
            }
            return v.ToString();
        }

        object ConvertToObject(Type t, string v) {
            TypeConverter c = TypeDescriptor.GetConverter(t);
            if (c != null) {
                return c.ConvertFromInvariantString(v);
            }
            return v;
        }
    }
}
