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
        Dictionary<string, UnifiedContact> contactIndex = new Dictionary<string, UnifiedContact>();
        Dictionary<string, UnifiedAppointment> appointmentOutlookIndex = new Dictionary<string, UnifiedAppointment>();
        Dictionary<string, UnifiedAppointment> appointmentPhoneIndex = new Dictionary<string, UnifiedAppointment>();

        public UnifiedStore()
        {
            this.Contacts = new ObservableCollection<UnifiedContact>();
            this.Contacts.CollectionChanged += Contacts_CollectionChanged;

            this.Appointments = new ObservableCollection<UnifiedAppointment>();
            this.Appointments.CollectionChanged += Appointments_CollectionChanged;
        }


        static int _syncTime;

        public static void UpdateSyncTime()
        {
            _syncTime = 0;
        }

        public static int SyncTime
        {
            get
            {
                if (_syncTime == 0)
                {
                    TimeSpan from2013 = DateTime.UtcNow - new DateTime(2013, 1, 1);
                    // this should fit in a 32 bit integer, and be able to count 
                    // time out about 150 years which should be plenty for this app :-) 
                    _syncTime = (int)from2013.TotalSeconds;
                }
                return _syncTime;
            }
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
                                Debug.WriteLine("Loading store: " + myFileStream.Length + " bytes");
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
                    switch (reader.LocalName)
                    {
                        case "Contacts":
                            store.ReadContacts(reader);
                            break;
                        case "Appointments":
                            store.ReadAppointments(reader);
                            break;
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
                            if (!contactIndex.ContainsKey(id))
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

        private void ReadAppointments(UnifiedStoreSerializer reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }
            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "UnifiedAppointment")
                    {
                        UnifiedAppointment appointment = new UnifiedAppointment();
                        appointment.Read(reader);

                        string id = appointment.PhoneId;                        
                        if (id != null && !appointmentPhoneIndex.ContainsKey(id))
                        {
                            this.Appointments.Add(appointment);
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

        private class ContactComparer : IComparer<UnifiedContact>
        {
            public int Compare(UnifiedContact x, UnifiedContact y)
            {
                if (x.DisplayName == y.DisplayName)
                {
                    return 0;
                }
                else if (x.DisplayName == null)
                {
                    return -1;
                }
                else if (y.DisplayName == null)
                {
                    return 1;
                }
                else
                {
                    return string.Compare(x.DisplayName, y.DisplayName);
                }
            }
        }
        private class AppointmentComparer : IComparer<UnifiedAppointment>
        {
            public int Compare(UnifiedAppointment x, UnifiedAppointment y)
            {
                if (x.Start == y.Start)
                {
                    if (x.End == y.End)
                    {
                        if (x.Subject == y.Subject)
                        {
                            return 0;
                        }
                        else if (x.Subject == null) 
                        {
                            return -1;
                        }
                        else if (y.Subject == null)
                        {
                            return 1;
                        }
                        return string.Compare(x.Subject, y.Subject);
                    }
                    DateTimeOffset.Compare(x.End, y.End);
                }
                return DateTimeOffset.Compare(x.Start, y.Start);
            }
        }
        

        private void WriteStore(XmlWriter writer, UnifiedStore unifiedStore)
        {
            writer.WriteStartElement("Data");

            if (this.Contacts != null)
            {                
                writer.WriteStartElement("Contacts");
                
                // sorting the output makes it easier to debug
                SortedSet<UnifiedContact> sorted = new SortedSet<UnifiedContact>(this.Contacts, new ContactComparer());
                foreach (UnifiedContact contact in sorted)
                {
                    contact.Write(writer);
                }
                writer.WriteEndElement();
            }

            if (this.Appointments != null)
            {
                writer.WriteStartElement("Appointments");

                // sorting the output makes it easier to debug
                SortedSet<UnifiedAppointment> asorted = new SortedSet<UnifiedAppointment>(this.Appointments, new AppointmentComparer());
                foreach (var appointment in asorted)
                {
                    appointment.Write(writer);
                }

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public ObservableCollection<UnifiedContact> Contacts { get; set; }

        public UnifiedContact FindContactById(string id)
        {
            UnifiedContact c;
            contactIndex.TryGetValue(id, out c);
            return c;
        }

        public ObservableCollection<UnifiedAppointment> Appointments { get; set; }

        public UnifiedAppointment FindAppointmentById(string id)
        {
            UnifiedAppointment a = null;
            if (!string.IsNullOrEmpty(id))
            {
                appointmentOutlookIndex.TryGetValue(id, out a);
            }
            return a;
        }

        public UnifiedAppointment FindAppointmentByPhoneId(string phoneId)
        {
            UnifiedAppointment a;
            appointmentPhoneIndex.TryGetValue(phoneId, out a);
            return a;
        }

        void Contacts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (UnifiedContact c in e.OldItems)
                {
                    contactIndex.Remove(c.OutlookEntryId);
                }
            }
            if (e.NewItems != null)
            {
                foreach (UnifiedContact c in e.NewItems)
                {
                    contactIndex[c.OutlookEntryId] = c;
                }
            }
        }

        void Appointments_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (UnifiedAppointment a in e.OldItems)
                {
                    if (a.Id != null)
                    {
                        appointmentOutlookIndex.Remove(a.Id);
                    }
                    if (a.PhoneId != null)
                    {
                        appointmentPhoneIndex.Remove(a.PhoneId);
                    }
                }
            }
            if (e.NewItems != null)
            {
                foreach (UnifiedAppointment a in e.NewItems)
                {
                    if (a.Id != null)
                    {
                        appointmentOutlookIndex[a.Id] = a;
                    }
                    if (a.PhoneId != null)
                    {
                        appointmentPhoneIndex[a.PhoneId] = a;
                    }
                }
            }
        }


        /// <summary>
        /// This method is called to see if there is any existing appointment matching the given one
        /// just so we avoid creating too many duplicate entries...
        /// </summary>
        /// <param name="fromPhone"></param>
        internal UnifiedAppointment FindMatchingAppointment(UnifiedAppointment fromPhone)
        {
            foreach (UnifiedAppointment a in this.Appointments)
            {
                if (a.Start == fromPhone.Start && a.End == fromPhone.End && a.Subject == fromPhone.Subject)
                {
                    return a;
                }
            }
            return null;
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
                else if (type == typeof(bool))
                {
                    bool b = false;
                    bool.TryParse(value, out b);
                    return b;
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
