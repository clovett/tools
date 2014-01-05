using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Specialized;
using System.Collections;
using System.Reflection;
using System.Xml.Serialization;
using System.Diagnostics;

#if WINDOWS_PHONE
using System.IO.IsolatedStorage;
#endif

namespace OutlookSync.Model
{
    /// <summary>
    /// This class represents the serialized state of the unified contacts list.
    /// </summary>
    public class UnifiedStore 
    {
        SortedDictionary<string, UnifiedContact> index = new SortedDictionary<string, UnifiedContact>();

        public UnifiedStore()
        {
            this.Contacts = new ObservableCollection<UnifiedContact>();
            this.Contacts.CollectionChanged += Contacts_CollectionChanged;
        }

#if WINDOWS_PHONE
        public static async Task<UnifiedStore> LoadAsync(string fileName)
        {
            UnifiedStore result = null;
            try
            {
                await Task.Run(new Action(() =>
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (store.FileExists(fileName))
                        {
                            using (FileStream myFileStream = store.OpenFile(fileName, FileMode.Open))
                            {
                                // Call the Deserialize method and cast to the object type.
                                using (XmlReader reader = XmlReader.Create(myFileStream))
                                {
                                    result = ReadStore(new UnifiedStoreSerializer(reader));
                                }
                            }
                        }
                    }
                }));
            }
            catch
            {
                // silently rebuild data file if it got corrupted.
                result = new UnifiedStore();
            }

            if (result == null)
            {
                result = new UnifiedStore();
            }
            return result;
        }
#else
        public static async Task<UnifiedStore> LoadAsync(string fileName)
        {
            UnifiedStore result = new UnifiedStore();
            await Task.Run(new Action(() =>
            {
                if (File.Exists(fileName))
                {
                    try
                    {
                        using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            using (XmlReader reader = XmlReader.Create(stream))
                            {
                                result = ReadStore(new UnifiedStoreSerializer(reader));
                            }
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
#endif

        private static UnifiedStore ReadStore(UnifiedStoreSerializer reader)
        {
            // this doesn't use XmlSerializer, because we have another dimension to the data that XmlSerializer doesn't support
            // namely, the "VersionNumbers" on all properties.
            UnifiedStore store = new UnifiedStore();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "Contacts")
                    {
                        store.ReadContacts(reader);
                    }
                }
            }

            return store;
        }

        private void ReadContacts(UnifiedStoreSerializer reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }
            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "UnifiedContact")
                    {
                        UnifiedContact contact = new UnifiedContact();
                        contact.Read(reader);

                        string id = contact.OutlookEntryId;
                        if (!string.IsNullOrEmpty(id))
                        {
                            if (!index.ContainsKey(id))
                            {
                                this.Contacts.Add(contact);
                            }
                        }
                        else
                        {
                            // crap, this item got corrupted, so ignore it.
                        }
                    }
                }
            }
        }

        public string ToXml()
        {
            StringWriter sw = new StringWriter();
            XmlWriterSettings settings = new XmlWriterSettings() { Indent = true, NewLineHandling = NewLineHandling.None };
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                WriteStore(writer, this);
            }
            return sw.ToString();
        }

#if WINDOWS_PHONE
        public async Task SaveAsync(string fileName)
        {
            await Task.Run(new Action(() =>
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists(fileName))
                    {
                        store.DeleteFile(fileName);
                    }

                    using (Stream stream = store.OpenFile(fileName, FileMode.CreateNew))
                    {
                        XmlWriterSettings settings = new XmlWriterSettings() { Indent = true, NewLineHandling = NewLineHandling.None };
                        using (XmlWriter writer = XmlWriter.Create(stream, settings))
                        {
                            WriteStore(writer, this);
                        }
                    }
                }
            }));
        }
#else
        public async Task SaveAsync(string fileName)
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
                    XmlWriterSettings settings = new XmlWriterSettings() { Indent = true, NewLineHandling = NewLineHandling.None };

                    using (XmlWriter writer = XmlWriter.Create(stream, settings))
                    {
                        WriteStore(writer, this);
                    }
                }
            }));
        }
#endif

        private void WriteStore(XmlWriter writer, UnifiedStore unifiedStore)
        {
            writer.WriteStartElement("Data");

            if (this.Contacts != null)
            {                
                writer.WriteStartElement("Contacts");
                foreach (var pair in this.index)
                {
                    var contact = pair.Value;
                    contact.Write(writer);
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public ObservableCollection<UnifiedContact> Contacts { get; set; }

        public UnifiedContact FindOutlookEntry(string id)
        {
            UnifiedContact c;
            index.TryGetValue(id, out c);
            return c;
        }

        void Contacts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (UnifiedContact c in e.OldItems)
                {
                    index.Remove(c.OutlookEntryId);
                }
            }
            if (e.NewItems != null)
            {
                foreach (UnifiedContact c in e.NewItems)
                {
                    index[c.OutlookEntryId] = c;
                }
            }
        }

    }

    class UnifiedStoreSerializer
    {
        XmlReader reader;
        XmlNamespaceManager mgr;

        public UnifiedStoreSerializer(XmlReader reader)
        {
            this.reader = reader;

            mgr = new System.Xml.XmlNamespaceManager(reader.NameTable);
        }

        public bool IsEmptyElement { get { return reader.IsEmptyElement; } }

        public XmlNodeType NodeType { get { return reader.NodeType; } }

        public bool Read() { return reader.Read(); }

        public string LocalName { get { return reader.LocalName; } }

        internal object ReadElementContentAs(Type type)
        {
            // bugbug: in Silverlight, the ReadElementContentAsString advances past the end tag which
            // totally fucks the whole serialization process, so we use a different approach here.

            if (reader.IsEmptyElement)
            {
                return null;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                string value = ReadContent();
                if (type.IsEnum)
                {
                    try
                    {
                        return Enum.Parse(type, value);
                    }
                    catch
                    {
                        return null;
                    }
                }
                else if (type == typeof(string))
                {
                    return value;
                }
                else if (type == typeof(DateTime))
                {
                    DateTime? result = null;
                    // serialized as date time offset.
                    DateTime dt = DateTime.MinValue;
                    if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, out dt) && dt != DateTime.MinValue)
                    {
                        result = dt;
                    }
                    return result;
                }
                else if (type == typeof(DateTimeOffset))
                {
                    DateTimeOffset? result = null;
                    // serialized as date time offset.
                    DateTimeOffset dt = DateTimeOffset.MinValue;
                    if (!string.IsNullOrEmpty(value) && DateTimeOffset.TryParse(value, out dt) && dt != DateTimeOffset.MinValue)
                    {
                        result = dt;
                    }
                    return result;
                }
                else if (type == typeof(int))
                {
                    int i = 0;
                    int.TryParse(value, out i);
                    return i;
                }
                else
                {
                    // etc
                    throw new NotImplementedException("Deserialization of type '" + type.Name + "' is not yet implemented");
                }
            }
            else
            {
                throw new Exception("Expecting reader to be positioned on an Element before calleing ReadElementContentAs");
            }
        }

        string ReadContent()
        {
            StringBuilder sb = new StringBuilder();
            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
            {                
                sb.Append(reader.Value);
            }
            return sb.ToString();
        }

        public IList ReadList(string listType)
        {
            string elementType = GetListElementType(listType);
            IList list = PropertyListConstructor.CreateGenericList(elementType);
            if (reader.IsEmptyElement)
            {
                return list;
            }

            Type listElementType = list.GetType().GenericTypeArguments[0];

            bool elementsArePropertyBags = false;
            ConstructorInfo ci = null;
            if (typeof(PropertyBag).IsAssignableFrom(listElementType))
            {
                elementsArePropertyBags = true;
                ci = listElementType.GetConstructor(new Type[0]);
                if (ci == null)
                {
                    throw new Exception(string.Format("Element type '{0}' has no default constructor", elementType));
                }
            }

            while (reader.Read() && reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                if (reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    if (reader.LocalName == elementType)
                    {
                        if (elementsArePropertyBags)
                        {
                            PropertyBag bag = (PropertyBag)ci.Invoke(new object[0]);
                            bag.Read(this);
                            list.Add(bag);
                        }
                        else
                        {
                            object typedValue = reader.ReadElementContentAs(listElementType, new System.Xml.XmlNamespaceManager(reader.NameTable));
                            list.Add(typedValue);
                        }

                    }
                }
            }
            return list;
        }

        string GetListElementType(string typeName)
        {
            int i = typeName.IndexOf('<');
            if (i > 0)
            {
                i++;
                int j = typeName.IndexOf('>', i);
                if (j > i)
                {
                    return typeName.Substring(i, j - i);
                }
            }
            return null;
        }

        internal string GetAttribute(string name)
        {
            return reader.GetAttribute(name);
        }
    }

    static class PropertyListConstructor
    {
        public static IList CreateGenericList(string elementTypeName)
        {
            if (elementTypeName == "String")
            {
                return new PropertyList<string>();
            }

            Type elementType = typeof(PropertyListConstructor).Assembly.GetType("OutlookSync." + elementTypeName);
            if (elementType == null)
            {
                elementType = typeof(PropertyListConstructor).Assembly.GetType("OutlookSync.Model." + elementTypeName);
            }
            if (elementType == null)
            {
                throw new Exception("Unknown type: " + elementTypeName);
            }
            Type[] typeArgs = new Type[] { elementType };
            MethodInfo mi = typeof(PropertyListConstructor).GetMethod("CreateList", BindingFlags.NonPublic | BindingFlags.Static);
            object result = mi.MakeGenericMethod(typeArgs).Invoke(null, new object[0]);
            return (IList)result;
        }

        private static IList CreateList<T>()
        {
            return new PropertyList<T>();
        }
    }


}
