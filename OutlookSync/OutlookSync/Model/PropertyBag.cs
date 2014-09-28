using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using OutlookSync.Utilities;

namespace OutlookSync.Model
{
    interface IPropertyParentable
    {
        PropertyValue Parent { get; set; }
    }

    public class PropertyList<T> : IList<T>, IList, IPropertyParentable
    {
        private List<T> list;

        public PropertyList()
        {
            list = new List<T>();
        }

        public PropertyList(IEnumerable<T> items)
        {
            list = new List<T>(items);
        }

        public PropertyValue Parent { get; set; }

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        void OnChanged()
        {
            if (Parent != null)
            {
                Parent.VersionNumber = UnifiedStore.SyncTime;
            }
        }

        public void Insert(int index, T item)
        {
            OnChanged();
            list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            OnChanged();
            list.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                OnChanged();
                list[index] = value;
            }
        }

        public void Add(T item)
        {
            OnChanged();
            list.Add(item);
        }

        public void Clear()
        {
            OnChanged();
            list.Clear();
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return list.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            OnChanged();
            return list.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public int Add(object value)
        {
            OnChanged();
            list.Add((T)value);
            return list.IndexOf((T)value);
        }

        public bool Contains(object value)
        {
            return list.Contains((T)value);
        }

        public int IndexOf(object value)
        {
            return list.IndexOf((T)value);
        }

        public void Insert(int index, object value)
        {
            OnChanged();
            list.Insert(index, (T)value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            OnChanged();
            list.Remove((T)value);
        }

        object IList.this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                OnChanged();
                list[index] = (T)value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            list.ToArray().CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return null;  }
        }
    }

    public class PropertyBag
    {
        SortedDictionary<string, PropertyValue> bag = new SortedDictionary<string, PropertyValue>();

        Dictionary<string, string> propertyRenames = new Dictionary<string, string>();

        public virtual int? GetKey()
        {
            return null;
        }

        /// <summary>
        /// Record a property name change so that serialization can take care of the migration for us.
        /// The Read() method will automatically convert the "from" named property to the new "to" name.
        /// The Write method will ignore it and write out the new property name.
        /// </summary>
        /// <param name="oldName">The old property name that we might find in the serialized store.</param>
        /// <param name="newName">The new name to change the serialized property to.</param>
        public void RenameProperty(string oldName, string newName)
        {
            propertyRenames[newName] = newName;
        }

        public virtual bool IsEmpty { get { return bag.Count == 0; } }

        public object GetValue(string key)
        {
            PropertyValue v = GetPropertyValue(key);
            if (v != null)
            {
                return v.Value;
            }
            return null;
        }

        public PropertyValue GetPropertyValue(string key)
        {
            PropertyValue v = null;
            bag.TryGetValue(key, out v);
            return v;
        }

        public T GetValue<T>(string key)
        {
            object v = GetValue(key);
            if (v != null)
            {
                return (T)v;
            }
            return default(T);
        }

        public bool HasValue(string key)
        {
            return bag.ContainsKey(key);
        }

        /// <summary>
        /// After synchronization is complete we can remove all null or default valued properties to compress the store size.
        /// But, for this to be safe we need to be sure all phones are synchronized, not just one of them, otherwise we'll
        /// forget it was a 'delete' when we synchronize the second phone.  Either that or we need to create a separate
        /// UnifiedStore for each phone on the PC, which is probably a good idea anyway...
        /// </summary>
        public void Compress()
        {
            foreach (KeyValuePair<string, PropertyValue> pair in bag.ToArray())
            {
                string key = pair.Key;
                bool remove = false;
                PropertyValue pv = pair.Value;
                object v = pv.Value;
                if (v == null)
                {
                    remove = true;
                }
                else
                {
                    // check for "default" values.
                    if (v is bool)
                    {
                        if ((bool)v == false)
                        {
                            remove = true;
                        }
                    }
                    else if (v is int)
                    {
                        if ((int)v == 0)
                        {
                            remove = true;
                        }
                    }
                    else if (v is DateTime)
                    {
                        if ((DateTime)v == new DateTime())
                        {
                            remove = true;
                        }
                    }
                    else if (v is string)
                    {
                        if (string.IsNullOrEmpty((string)v))
                        {
                            remove = true;
                        }
                    }
                    else if (v is Enum)
                    {
                        int i = System.Convert.ToInt32(v);
                        if (i == 0)
                        {
                            remove = true;
                        }
                    }
                    else if (v is PropertyBag)
                    {
                        PropertyBag b = (PropertyBag)v;
                        if (b.bag.Count == 0) 
                        {
                            remove = true;
                        }
                    }
                    else if (v is IList)
                    {
                        IList list = (IList)v;
                        if (list.Count == 0)
                        {
                            remove = true;
                        }
                    }
                    else
                    {
                        // any other types to check here?
                        Log.WriteLine("Should we be compressing property values of type: {0}", v.GetType().FullName);
                    }
                }

                if (remove)
                {
                    // ok, remove it!
                    bag.Remove(key);
                }
            }

        }

        /// <summary>
        /// This method does not create a null property value if there wasn't one there already.
        /// To do that call InnerSetValue with rememberNull set to false.  This method really
        /// deletes the property so nothing is serialized, and no version number updates occur.
        /// </summary>
        /// <param name="key">The property value to clear</param>
        public void ClearValue(string key)
        {
            if (bag.ContainsKey(key))
            {
                bag.Remove(key);
            }
        }

        internal void InnerSetValue(string key, object value, bool rememberNull = false)
        {
            PropertyValue v = GetPropertyValue(key);
            if (v != null)
            {
                if (value == null)
                {
                    // nulling out the value then to remember a delete.
                    if (v.Value != null)
                    {
                        v.Value = value;
                        v.VersionNumber = UnifiedStore.SyncTime;
                    }
                }
                else if (value is IList)
                {
                    if (v.Value != null)
                    {
                        Log.WriteLine("Why wasn't this list diffed?");
                    }
                    v.Value = value;
                    v.VersionNumber = UnifiedStore.SyncTime;
                }
                else if (value is PropertyBag)
                {
                    if (v.Value != null)
                    {
                        Log.WriteLine("Why wasn't this property bag diffed?");
                    }
                    v.Value = value;
                    v.VersionNumber = UnifiedStore.SyncTime;
                }
                else
                {
                    string a = UnifyNewLines((string)v.Value);
                    string b = UnifyNewLines(value.ToString());
                    if (a != b)
                    {
                        v.Value = value;
                        v.VersionNumber = UnifiedStore.SyncTime;
                    }
                }
            }
            else if (value == null && !rememberNull)
            {
                // we don't have the property, and the caller doesn't want one, so
                // there is no delete to remember here, just skip it!
            }
            else
            {
                if (value is string && String.IsNullOrEmpty((string)value))
                {
                    // don't add empty strings
                    return;
                }
                v = new PropertyValue() { Value = value, VersionNumber = UnifiedStore.SyncTime };
                bag[key] = v;
            }
        }

        private string UnifyNewLines(string s)
        {
            if (s == null) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0, n = s.Length; i<n; i++)
            {
                char c = s[i];
                if (c == '\r')
                {
                    if (i + 1 < n && s[i+1] == '\n')
                    {
                        // convert "\r\n" into "\n";
                        continue;
                    }
                    else
                    {
                        sb.Append('\n');
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public void SetValue<T>(string key, T value)
        {
            InnerSetValue(key, (object)value, false);
        }

        internal void Read(UnifiedStoreSerializer reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }

            while (reader.Read() && reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                if (reader.NodeType == System.Xml.XmlNodeType.Element)
                {
                    string name = reader.LocalName;

                    string newName = null;
                    if (propertyRenames.TryGetValue(name, out newName))
                    {
                        name = newName;
                    }

                    string version = reader.GetAttribute("version");
                    string type = reader.GetAttribute("type");
                    if (type.StartsWith("List"))
                    {
                        IList list = reader.ReadList(type);
                        SetValue(name, list);
                        IPropertyParentable ipp = list as IPropertyParentable;
                        if (ipp != null)
                        {
                            ipp.Parent = GetPropertyValue(name);
                        }
                    }
                    else if (type == "null")
                    {
                        // we got a null, so we are trying to remember a delete operation
                        this.InnerSetValue(name, null, true);
                    }
                    else 
                    {
                        object typedValue = ConstructInstance(type);
                        PropertyBag bag = typedValue as PropertyBag;
                        if (bag != null)
                        {
                            bag.Read(reader);
                            this.InnerSetValue(name, bag);
                        }
                        else
                        {
                            typedValue = reader.ReadElementContentAs(typedValue.GetType());
                            this.InnerSetValue(name, typedValue);
                        }
                    }

                    int v = 0;
                    if (int.TryParse(version, out v))
                    {
                        if (v > UnifiedStore.SyncTime)
                        {
                            // fix bogus timestamps from before.
                            v = UnifiedStore.SyncTime;
                        }
                        this.GetPropertyValue(name).VersionNumber = v;
                    }
                }
            }
        }

        private object ConstructInstance(string type)
        {
            if (type == "String") return "";
            if (type == "Int32") return 0;
            if (type == "Boolean") return false;
            if (type == "DateTime") return DateTime.MinValue;
            if (type == "DateTimeOffset") return DateTimeOffset.MinValue;

            Type t = this.GetType().Assembly.GetType("OutlookSync." + type);

            if (t == null)
            {
                t = this.GetType().Assembly.GetType("OutlookSync.Model." + type);
            }
            if (t != null)
            {
                if (t.IsEnum)
                {
                    var values = t.GetEnumValues();
                    var e = values.GetEnumerator();
                    e.MoveNext();
                    return e.Current;
                }
                ConstructorInfo ci = t.GetConstructor(new Type[0]);
                return ci.Invoke(new object[0]);
            }

            throw new Exception("Unknown type: " + type);

        }

        internal string ToXml()
        {
            StringWriter buffer = new StringWriter();
            using (XmlWriter writer = XmlWriter.Create(buffer))
            {
                this.Write(writer);
            }
            return buffer.ToString();
        }


        internal void Write(System.Xml.XmlWriter writer, bool wrapper = true)
        {
            if (wrapper)
            {
                string elementName = this.GetType().Name;
                writer.WriteStartElement(elementName);
            }
            foreach (var pair in bag)
            {
                var key = pair.Key;
                PropertyValue value = pair.Value;

                if (value.Value != null || value.VersionNumber > 0)
                {
                    if (value.Value == null)
                    {
                        // write a null value.
                        writer.WriteStartElement(key);
                        writer.WriteAttributeString("version", value.VersionNumber.ToString());
                        writer.WriteAttributeString("type", "null");
                        writer.WriteEndElement();
                    }
                    else
                    {
                        string typeName = value.Value.GetType().Name;

                        IList list = value.Value as IList;
                        if (list != null)
                        {
                            if (list.Count > 0)
                            {
                                writer.WriteStartElement(key);
                                writer.WriteAttributeString("version", value.VersionNumber.ToString());
                                string elementType = list.GetType().GenericTypeArguments[0].Name;
                                writer.WriteAttributeString("type", "List<" + elementType + ">");

                                foreach (object item in list)
                                {
                                    PropertyBag childBag = item as PropertyBag;
                                    if (childBag != null)
                                    {
                                        childBag.Write(writer);
                                    }
                                    else if (item != null)
                                    {
                                        WriteScalar(writer, elementType, item);
                                        writer.WriteEndElement();
                                    }
                                }
                                writer.WriteEndElement();
                            }
                        }
                        else
                        {
                            writer.WriteStartElement(key);
                            writer.WriteAttributeString("version", value.VersionNumber.ToString());
                            writer.WriteAttributeString("type", typeName);
                            if (value.Value is PropertyBag)
                            {
                                PropertyBag childBag = (PropertyBag)value.Value;
                                childBag.Write(writer, false);
                            }
                            else
                            {
                                writer.WriteString(value.Value.ToString());
                            }
                            writer.WriteEndElement();
                        }
                    }
                }

            }
            if (wrapper)
            {
                writer.WriteEndElement();
            }
        }

        private static void WriteScalar(System.Xml.XmlWriter writer, string elementType, object item)
        {
            writer.WriteStartElement(elementType);
            if (item is DateTime?)
            {
                DateTime? dt = (DateTime?)item;
                if (dt.HasValue)
                {
                    writer.WriteValue(new DateTimeOffset(dt.Value));
                }
                else
                {
                    writer.WriteString("");
                }
            }
            else
            {
                writer.WriteString(item.ToString());
            }
        }

        private object Convert(string value, string type)
        {
            switch (type)
            {
                case "null":
                    return null;
                case "string":
                    return value;
                case "datetime":
                    DateTimeOffset? date = null;
                    DateTimeOffset dt = DateTimeOffset.MinValue;
                    if (!string.IsNullOrEmpty(value) && DateTimeOffset.TryParse(value, out dt))
                    {
                        date = dt;
                    }
                    return date;
                case "int":
                    int i = 0;
                    int.TryParse(value, out i);
                    return i;
                case "bool":
                    bool b = false;
                    bool.TryParse(value, out b);
                    return b;
                default:
                    throw new Exception("Unexpected type: " + type);
            }
        }


        /// <summary>
        /// Merges the information from other contact into our local values.
        /// </summary>
        /// <param name="other">The contact as received from Outlook Sync Server</param>
        /// <returns>True if any of our local versions are more recent than the ones received from Outlook Server</returns>
        internal bool Merge(PropertyBag other)
        {
            bool localMoreRecent = false;
            HashSet<string> found = new HashSet<string>();
            foreach (KeyValuePair<string, PropertyValue> pair in bag)
            {
                string key = pair.Key;
                found.Add(key);

                PropertyValue pv = pair.Value;

                PropertyValue opv = null;
                if (other.bag.TryGetValue(key, out opv))
                {
                    localMoreRecent |= MergeValues(pv, opv);
                }
                else if (pv != null && (pv.Value != null || pv.VersionNumber > 0))
                {
                    // outlook doesn't have it at all, which means it was added locally (we never null things out on delete).
                    localMoreRecent = true;                    
                }
            }

            // check for new properties.
            foreach (KeyValuePair<string, PropertyValue> pair in other.bag)
            {
                string key = pair.Key;
                PropertyValue pv = pair.Value;
                if (!found.Contains(key))
                {
                    this.SetValue(key, pv.Value);
                }
            }
            
            // update our version info.
            this.VersionNumber = GetHighestVersionNumber();

            return localMoreRecent;
        }

        private bool MergeValues(PropertyValue pv, PropertyValue opv)
        {
            bool localMoreRecent = false;
            bool otherMoreRecent = false;

            if (pv.VersionNumber > opv.VersionNumber)
            {
                // local value is more recent
                localMoreRecent = true;
            }
            else if (opv.VersionNumber > pv.VersionNumber)
            {
                // other value is more recent
                otherMoreRecent = true;
                pv.VersionNumber = opv.VersionNumber;
            }
            else
            {
                // if it is a ListValue or PropertyBag value the we need to drill down to find any changes inside.
            }

            bool rc = localMoreRecent;

            if (pv.Value == null && opv.Value == null)
            {
                // nothing to do.
            }
            else if (pv.Value is IList || opv.Value is IList)
            {
                IList listValue = pv.Value as IList;
                IList otherList = opv.Value as IList;
                if (otherList == null || otherList.Count == 0)
                {
                    // was the list deleted on Outlook?
                    if (otherMoreRecent)
                    {
                        // clobber our local value then.
                        pv.Value = otherList;
                    }
                }
                else if (listValue == null || listValue.Count == 0)
                {
                    if (otherMoreRecent)
                    {
                        // set our local value then (note: we are splicing the object graphs here, but it should be ok).
                        // bugbug: opv.VersionNumber > pv.VersionNumber should have been true in this case?
                        pv.Value = otherList;
                    }
                    else
                    {
                        // we deleted the local value, so keep it that way
                    }
                }
                else
                {
                    rc |= MergeLists(listValue, otherList, otherMoreRecent);
                }
            }
            else if (pv.Value is PropertyBag || opv.Value is PropertyBag)
            {
                PropertyBag localBag = pv.Value as PropertyBag;
                PropertyBag otherBag = opv.Value as PropertyBag;
                if (otherBag == null)
                {
                    // was the list deleted on Outlook?
                    if (otherMoreRecent)
                    {
                        // delete our local value then.
                        pv.Value = null;
                    }
                }
                else if (localBag == null)
                {
                    if (otherMoreRecent)
                    {
                        // set our local value then (note: we are splicing the object graphs here, but it should be ok).
                        pv.Value = otherBag;
                    }
                }
                else
                {
                    rc |= localBag.Merge(otherBag);
                }
            }
            else
            {
                // must be a scalar
                if (pv.Value != null)
                {
                    object v = pv.Value;
                    Debug.Assert(v is int || v is string || v is DateTime? || v is DateTimeOffset? || v.GetType().IsEnum || v is bool, "Unexpected property value type: " + v.GetType().FullName);
                }

                if (otherMoreRecent)
                {
                    pv.Value = opv.Value;
                }

            }

            return rc;
        }

        /// <summary>
        /// Merge the list values
        /// </summary>
        /// <returns>True if we found a newer local value</returns>
        private bool MergeLists(IList listValue, IList otherList, bool otherMoreRecent)
        {
            bool rc = false;
            bool propertyBags = false;
            Dictionary<int, object> listValues = ConvertList(listValue, out propertyBags);

            bool otherPropertyBags = false;
            Dictionary<int, object> otherValues = ConvertList(otherList, out otherPropertyBags);

            Debug.Assert(propertyBags == otherPropertyBags, "Expecting both lists to contain consistent value types");

            HashSet<int> ourKeys = new HashSet<int>();

            foreach (var pair in listValues)
            {
                int key = pair.Key;
                ourKeys.Add(key);

                object localValue = pair.Value;
                object otherValue = null;

                if (!otherValues.TryGetValue(key, out otherValue))
                {
                    // keep our local value then
                    if (otherMoreRecent)
                    {
                        // perhaps it was deleted in outlook
                        listValue.Remove(localValue);
                    }
                    else
                    {
                        // local list is different!
                        rc = true;
                    }
                }
                else
                {

                    if (localValue is PropertyBag || otherValue is PropertyBag)
                    {
                        PropertyBag localBag = localValue as PropertyBag;
                        PropertyBag otherBag = otherValue as PropertyBag;
                        if (otherBag == null || localBag == null)
                        {
                            if (otherMoreRecent)
                            {
                                // perhaps it was deleted in outlook
                                listValue.Remove(localValue);
                            }
                        }
                        else
                        {
                            rc |= localBag.Merge(otherBag);
                        }
                    }
                    else
                    {
                        // must be a scalar, should be the same then.
                        if (localValue != null)
                        {
                            Debug.Assert(localValue is int || localValue is string || localValue is DateTime? || localValue is DateTimeOffset? || localValue.GetType().IsEnum || localValue is bool, "Unexpected property value type: " + localValue.GetType().FullName);
                        }

                        string a = localValue == null ? "" : localValue.ToString();
                        string b = otherValue == null ? "" : otherValue.ToString();
                        if (a != b)
                        {
                            Log.WriteLine("scalar list was updated, but the list version wasn't changed???");
                            if (otherMoreRecent)
                            {
                                // perhaps it was edited in outlook
                                int index = listValue.IndexOf(localValue);
                                if (index >= 0)
                                {
                                    if (otherValue != null)
                                    {
                                        listValue.Insert(index, otherValue);
                                    }
                                }
                                else
                                {
                                    listValue.Add(otherValue);
                                }

                                listValue.Remove(localValue);
                                
                            }
                        }
                    }
                }
            }

            // add any new items that we don't have locally.
            // bugbug: but are we sure it was added on outlook and not deleted locally ?
            foreach (var pair in otherValues)
            {
                int key = pair.Key;
                if (!ourKeys.Contains(key))
                {
                    object otherValue = pair.Value;
                    // we are splicing the object graph together here, but it should be ok.
                    listValue.Add(otherValue);
                }
            }

            return rc;
        }

        private Dictionary<int, object> ConvertList(IList list, out bool propertyBags)
        {
            propertyBags = false;
            int i = 0;
            Dictionary<int, object> listValues = new Dictionary<int, object>();
            foreach (object o in list)
            {
                PropertyBag bag = o as PropertyBag;
                if (bag != null)
                {
                    propertyBags = true;
                    int? key = bag.GetKey();
                    if (!key.HasValue)
                    {
                        listValues[i++] = o;
                    }
                    else
                    {
                        listValues[key.Value] = o;
                    }
                }
                else
                {
                    listValues[i++] = o;
                }
            }
            return listValues;
        }

        public int VersionNumber { get; set; }

        /// <summary>
        /// Get the highest version number on any item in this bag or in any child lists.
        /// </summary>
        /// <returns></returns>
        public int GetHighestVersionNumber()
        {
            int result = 0;

            foreach (KeyValuePair<string, PropertyValue> pair in bag)
            {
                string key = pair.Key;
                
                PropertyValue pv = pair.Value;
                if (pv != null)
                {
                    result = Math.Max(result, pv.VersionNumber);

                    IList list = pv.Value as IList;
                    if (list != null)
                    {
                        foreach (object item in list)
                        {
                            PropertyBag childBag = item as PropertyBag;
                            if (childBag != null)
                            {
                                int childVersion = childBag.GetHighestVersionNumber();
                                result = Math.Max(result, childVersion);
                            }
                            else
                            {
                                // must be a scalar
                            }
                        }
                    }
                    else
                    {
                        PropertyBag childBag = pv.Value as PropertyBag;
                        if (childBag != null)
                        {
                            int childVersion = childBag.GetHighestVersionNumber();
                            result = Math.Max(result, childVersion);
                        }
                        else
                        {
                            // must be a scalar
                        }
                    }
                }
            }

            VersionNumber = result;
            return result;
        }
    }

    public class PropertyValue
    {
        private object value;

        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public int VersionNumber { get; set; }
    }

}

