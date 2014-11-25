using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Globalization;
using System.Xml.Serialization;


namespace XmlNotepad
{
    /// <summary>
    /// This class represents a cached schema which may or may not be loaded yet.
    /// This allows delay loading of schemas.
    /// </summary>
    public class CacheEntry {
        string targetNamespace;
        Uri location;
        XmlSchema schema;
        bool disabled;
        CacheEntry next; // entry with same targetNamespace;

        public string TargetNamespace {
            get { return targetNamespace; }
            set { targetNamespace = value; }
        }

        public Uri Location {
            get { return location; }
            set { location = value; schema = null; }
        }

        public XmlSchema Schema {
            get { return schema; }
            set { schema = value; }
        }

        public CacheEntry Next {
            get { return next; }
            set { next = value; }
        }

        public bool Disabled {
            get { return disabled; }
            set { disabled = value; }
        }

        public CacheEntry FindByUri(Uri uri) {
            CacheEntry e = this;
            while (e != null) {
                if (e.location == uri) {
                    return e;
                }
                e = e.next;
            }
            return null;
        }

        // Remove the given cache entry and return the new head of the linked list.
        public CacheEntry RemoveUri(Uri uri) {
            CacheEntry e = this;
            CacheEntry previous = null;
            while (e != null) {
                if (e.location == uri) {
                    if (previous == null) {
                        return e.next; // return new head
                    }
                    previous.next = e.next; //unlink it
                    return this; // head is unchanged.
                }
                previous = e;
                e = e.next;
            }
            return this;
        }

        public void Add(CacheEntry newEntry) {
            CacheEntry e = this;
            while (e != null) {
                if (e == newEntry) {
                    return;
                }
                if (e.location == newEntry.location) {
                    e.schema = newEntry.schema;
                    e.disabled = newEntry.disabled;
                    return;
                }
                if (e.next == null) {
                    e.next = newEntry;
                    break;
                }
                e = e.next;
            }
        }

    }

    /// <summary>
    /// This class encapsulates an XmlSchema manager that loads schemas and associates them with
    /// the XML documents being edited. It also tracks changes to the schemas on disk and reloads
    /// them when necessary.
    /// </summary>
    public class SchemaCache : IXmlSerializable
    {
        // targetNamespace -> CacheEntry
        Dictionary<string, CacheEntry> namespaceMap = new Dictionary<string, CacheEntry>();
        // sourceUri -> CacheEntry
        Dictionary<Uri, CacheEntry> uriMap = new Dictionary<Uri, CacheEntry>();
        PersistentFileNames pfn = new PersistentFileNames();

        public SchemaCache() {
        }

        public void Clear() {
            namespaceMap.Clear();
            uriMap.Clear();
        }

        public IList<CacheEntry> GetSchemas() {
            List<CacheEntry> list = new List<CacheEntry>();
            foreach (CacheEntry ce in namespaceMap.Values) {
                CacheEntry e = ce;
                while (e != null) {
                    list.Add(e);
                    e = e.Next;
                }
            }
            return list;
        }

        public CacheEntry Add(string nsuri, Uri uri, bool disabled) {
            if (nsuri == null) nsuri = "";

            CacheEntry existing = null;
            CacheEntry e = null;

            if (namespaceMap.ContainsKey(nsuri)) {
                existing = namespaceMap[nsuri];
                e = existing.FindByUri(uri);                
            }
            if (e == null) {
                e = new CacheEntry();
                e.Location = uri;
                e.TargetNamespace = nsuri;

                if (existing != null) {
                    existing.Add(e);
                } else {
                    namespaceMap[nsuri] = e;
                }
            }
            e.Disabled = disabled;            

            uriMap[uri] = e;            

            return e;
        }

        public CacheEntry Add(XmlSchema s) {
            CacheEntry e = Add(s.TargetNamespace, new Uri(s.SourceUri), false);
            e.Schema = s;
            return e;
        }

        public void Remove(CacheEntry ce) {
            Remove(ce.Location);
        }

        public void Remove(Uri uri) {
            if (uriMap.ContainsKey(uri)) {
                CacheEntry e = uriMap[uri];
                uriMap.Remove(uri);
                string key = e.TargetNamespace;
                if (namespaceMap.ContainsKey(key)) {
                    CacheEntry head = namespaceMap[key];
                    CacheEntry newHead = head.RemoveUri(uri);
                    if (newHead == null) {
                        namespaceMap.Remove(key);
                    } else if (newHead != head) {
                        namespaceMap[key] = newHead;
                    }
                }
            }
        }

        public void Remove(string filename) {
            Uri uri = new Uri(filename);
            Remove(uri);
        }

        public void Remove(XmlSchema s) {
            Remove(s.SourceUri);
        }        

        public CacheEntry FindSchemasByNamespace(string targetNamespace) {
            if (namespaceMap.ContainsKey(targetNamespace)) {
                return namespaceMap[targetNamespace];                
            }
            return null;
        }

        public CacheEntry FindSchemaByUri(string sourceUri) {
            if (string.IsNullOrEmpty(sourceUri)) return null;
            return FindSchemaByUri(new Uri(sourceUri));
        }

        public CacheEntry FindSchemaByUri(Uri uri) {
            if (uriMap.ContainsKey(uri)) {
                return uriMap[uri];
            }
            return null;
        }

        public XmlResolver Resolver {
            get {
                return new SchemaResolver(this);
            }
        }

        public XmlSchemaType FindSchemaType(XmlQualifiedName qname) {
            string tns = qname.Namespace == null ? "" : qname.Namespace;
            CacheEntry e = this.FindSchemasByNamespace(tns);
            if (e == null) return null;
            while (e != null) {
                XmlSchema s = e.Schema;
                if (s != null) {
                    XmlSchemaObject so = s.SchemaTypes[qname];
                    if (so is XmlSchemaType)
                        return (XmlSchemaType)so;
                }
                e = e.Next;
            }
            return null;
        }

        public string[] GetValidValues(XmlSchemaType si) {
            if (si == null) return null;
            if (si is XmlSchemaSimpleType) {
                XmlSchemaSimpleType st = (XmlSchemaSimpleType)si;
                return GetValidValues(st);
            } else if (si is XmlSchemaComplexType) {
                XmlSchemaComplexType ct = (XmlSchemaComplexType)si;
                if (ct.ContentModel is XmlSchemaComplexContent) {
                    XmlSchemaComplexContent cc = (XmlSchemaComplexContent)ct.ContentModel;
                    if (cc.Content is XmlSchemaComplexContentExtension) {
                        XmlSchemaComplexContentExtension ce = (XmlSchemaComplexContentExtension)cc.Content;
                        return GetValidValues(GetTypeInfo(ce.BaseTypeName));
                    } else if (cc.Content is XmlSchemaComplexContentRestriction) {
                        XmlSchemaComplexContentRestriction cr = (XmlSchemaComplexContentRestriction)cc.Content;
                        return GetValidValues(GetTypeInfo(cr.BaseTypeName));
                    }
                } else if (ct.ContentModel is XmlSchemaSimpleContent) {
                    XmlSchemaSimpleContent sc = (XmlSchemaSimpleContent)ct.ContentModel;
                    if (sc.Content is XmlSchemaSimpleContentExtension) {
                        XmlSchemaSimpleContentExtension ce = (XmlSchemaSimpleContentExtension)sc.Content;
                        return GetValidValues(GetTypeInfo(ce.BaseTypeName));
                    } else if (sc.Content is XmlSchemaSimpleContentRestriction) {
                        XmlSchemaSimpleContentRestriction cr = (XmlSchemaSimpleContentRestriction)sc.Content;
                        return GetValidValues(GetTypeInfo(cr.BaseTypeName));
                    }
                }
            }
            return null;
        }

        XmlSchemaType GetTypeInfo(XmlQualifiedName qname) {
            return this.FindSchemaType(qname);
        }

        string[] GetValidValues(XmlSchemaSimpleType st) {
            if (st == null) return null;

            List<string> result = new List<string>();
            if (st.Datatype != null) {
                switch (st.Datatype.TypeCode) {
                    case XmlTypeCode.Language:
                        foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures)) {
                            result.Add(ci.Name);
                        }
                        result.Sort();
                        break;
                    case XmlTypeCode.Boolean:
                        result.Add("0");
                        result.Add("1");
                        result.Add("true");
                        result.Add("false");
                        break;
                }
            }

            if (st.Content is XmlSchemaSimpleTypeList) {
                XmlSchemaSimpleTypeList ce = (XmlSchemaSimpleTypeList)st.Content;
                string[] sa = GetValidValues(ce.ItemType);
                if (sa != null) result.AddRange(sa);
            } else if (st.Content is XmlSchemaSimpleTypeUnion) {
                XmlSchemaSimpleTypeUnion cr = (XmlSchemaSimpleTypeUnion)st.Content;
                if (cr.BaseMemberTypes != null) {
                    foreach (XmlSchemaSimpleType bt in cr.BaseMemberTypes) {
                        string[] sa = GetValidValues(bt);
                        if (sa != null) result.AddRange(sa);
                    }
                }
            } else if (st.Content is XmlSchemaSimpleTypeRestriction) {
                XmlSchemaSimpleTypeRestriction cr = (XmlSchemaSimpleTypeRestriction)st.Content;
                string[] baseEnums = GetValidValues(FindSchemaType(cr.BaseTypeName));
                if (baseEnums != null) result.AddRange(baseEnums);
                foreach (XmlSchemaFacet f in cr.Facets) {
                    if (f is XmlSchemaEnumerationFacet) {
                        XmlSchemaEnumerationFacet ef = (XmlSchemaEnumerationFacet)f;
                        result.Add(ef.Value);
                    }
                }                
            }
            return result.Count == 0 ? null : result.ToArray();
        }

        #region IXmlSerializable Members

        public XmlSchema GetSchema() {
            return null;
        }

        public void ReadXml(XmlReader r) {
            this.Clear();
            if (r.IsEmptyElement) return;
            while (r.Read() && r.NodeType != XmlNodeType.EndElement) {
                if (r.NodeType == XmlNodeType.Element) {
                    string nsuri = r.GetAttribute("nsuri");
                    bool disabled = false;
                    string s = r.GetAttribute("disabled");
                    if (!string.IsNullOrEmpty(s)) {
                        bool.TryParse(s, out disabled);
                    }
                    string filename = r.ReadString();
                    this.Add(nsuri, pfn.GetAbsoluteFilename(filename), disabled);
                }
            }
        }

        public void WriteXml(XmlWriter w) {
            try {
                foreach (CacheEntry e in this.GetSchemas()) {
                    string path = pfn.GetPersistentFileName(e.Location);
                    if (path != null) {
                        w.WriteStartElement("Schema");
                        string uri = e.TargetNamespace;
                        if (uri == null) uri = "";
                        w.WriteAttributeString("nsuri", uri);
                        if (e.Disabled) {
                            w.WriteAttributeString("disabled", "true");
                        }
                        w.WriteString(path);
                        w.WriteEndElement();
                    }
                }
            } catch (Exception x) {
                Console.WriteLine(x.Message);
            }
        }

        #endregion
    }

    public class SchemaResolver : XmlUrlResolver {
        SchemaCache cache;
        ValidationEventHandler handler;

        public SchemaResolver(SchemaCache cache) {
            this.cache = cache;
        }

        public ValidationEventHandler Handler {
            get { return handler; }
            set { handler = value; }
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {
            CacheEntry ce = cache.FindSchemaByUri(absoluteUri);
            if (ce != null && ce.Schema != null) return ce.Schema;
            Stream stm = base.GetEntity(absoluteUri, role, typeof(Stream)) as Stream;
            if (stm != null) {
                XmlSchema s = XmlSchema.Read(stm, handler);
                s.SourceUri = absoluteUri.AbsoluteUri;
                if (ce != null) {
                    ce.Schema = s;
                } else {
                    cache.Add(s);
                }
                return s;
            }
            return null;
        }
    }
}
