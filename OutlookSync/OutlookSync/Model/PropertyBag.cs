using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OutlookSync
{

    public class PropertyBag
    {
        Dictionary<string, PropertyValue> bag = new Dictionary<string, PropertyValue>();

        public object GetValue(string key)
        {
            PropertyValue v = null;
            bag.TryGetValue(key, out v);
            if (v != null)
            {
                return v.Value;
            }
            return null;
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

        public void SetValue(string key, object value)
        {
            PropertyValue v = null;
            bag.TryGetValue(key, out v);
            if (v != null)
            {
                if (value is IList)
                {
                    // just set it, (todo: add list diff later)
                    v.Value = value;
                    v.VersionNumber++; // record the change
                }
                else if (value is PropertyBag)
                {
                    // todo: add property bag diffing later.
                    v.Value = value;
                    v.VersionNumber++; 
                }
                else
                {
                    string a = (v.Value == null) ? "" : v.Value.ToString();
                    string b = (value == null) ? "" : value.ToString();
                    if (a != b)
                    {
                        v.Value = value;
                        v.VersionNumber++; // record the change
                    }
                }
            }
            else
            {
                v = new PropertyValue() { Value = value };
                bag[key] = v;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            SetValue(key, (object)value);
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
                    string version = reader.GetAttribute("version");
                    string type = reader.GetAttribute("type");
                    if (type.StartsWith("List"))
                    {
                        object list = reader.ReadList(type);
                        SetValue(name, list);
                    }
                    else
                    {
                        object typedValue = ConstructInstance(type);
                        PropertyBag bag = typedValue as PropertyBag;
                        if (bag != null)
                        {
                            bag.Read(reader);
                            this.SetValue(name, bag);
                        }
                        else
                        {
                            typedValue = reader.ReadElementContentAs(typedValue.GetType());
                            this.SetValue(name, typedValue);
                        }
                    }
                }
            }
        }

        private object ConstructInstance(string type)
        {
            if (type == "String") return "";
            if (type == "Int32") return 0;

            Type t = this.GetType().Assembly.GetType("OutlookSync." + type);
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

                if (value.Value != null)
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
                                    writer.WriteStartElement(elementType);
                                    writer.WriteString(item.ToString());
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
            if (wrapper)
            {
                writer.WriteEndElement();
            }
        }

        private object Convert(string value, string type)
        {
            switch (type)
            {
                case "string":
                    return value;
                case "datetime":
                    DateTime dt = DateTime.MinValue;
                    DateTime.TryParse(value, out dt);
                    return dt;
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
    }

    public class PropertyValue
    {
        public object Value { get; set; }
        public int VersionNumber { get; set; }
    }

}
