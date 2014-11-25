using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;

namespace XmlNotepad {

    public abstract class ErrorHandler {
        public abstract void HandleError(Severity sev, string reason, string filename, int line, int col, object data);
    }

    public enum IntellisensePosition { OnNode, AfterNode, FirstChild }
    
    public class Checker {
        XmlCache cache;
        XmlSchemaValidator validator;
        XmlSchemaInfo info;
        ErrorHandler eh;
        MyXmlNamespaceResolver nsResolver;
        Uri baseUri;
        Dictionary<XmlNode, XmlSchemaInfo> typeInfo = new Dictionary<XmlNode, XmlSchemaInfo>();
        XmlSchemaAttribute[] expectedAttributes;
        XmlSchemaParticle[] expectedParticles;
        XmlElement node;
        Hashtable parents;
        IntellisensePosition position;

        // Construct a checker for getting expected information about the given element.
        public Checker(XmlElement node, IntellisensePosition position) {
            this.node = node;
            this.position = position;
            parents = new Hashtable();
            XmlNode p = node.ParentNode;
            while (p != null) {
                parents[p] = p;
                p = p.ParentNode;
            }            
        }

        public Checker(ErrorHandler eh) {
            this.eh = eh;
        }

        public XmlSchemaAttribute[] GetExpectedAttributes() {
            return expectedAttributes;
        }

        public XmlSchemaParticle[] GetExpectedParticles() {
            return expectedParticles;
        }

        public void Validate(XmlCache xcache) {
            this.cache = xcache;
            if (string.IsNullOrEmpty(cache.FileName)) {
                baseUri = null;
            } else {
                baseUri = new Uri(new Uri(xcache.FileName), new Uri(".", UriKind.Relative));
            }
            ValidationEventHandler handler = new ValidationEventHandler(OnValidationEvent);
            SchemaResolver resolver = xcache.SchemaResolver as SchemaResolver;
            resolver.Handler = handler;
            XmlDocument doc = xcache.Document;
            this.info = new XmlSchemaInfo();
            this.nsResolver = new MyXmlNamespaceResolver(doc.NameTable);
            XmlSchemaSet set = new XmlSchemaSet();
            // Make sure the SchemaCache is up to date with document.
            SchemaCache sc = xcache.SchemaCache;
            foreach (XmlSchema s in doc.Schemas.Schemas()) {
                sc.Add(s);
            }

            if (LoadSchemas(doc, set, resolver)) {
                set.ValidationEventHandler += handler;
                set.Compile();
            }
            
            this.validator = new XmlSchemaValidator(doc.NameTable, set, nsResolver, 
                XmlSchemaValidationFlags.AllowXmlAttributes |
                XmlSchemaValidationFlags.ProcessIdentityConstraints |
                XmlSchemaValidationFlags.ProcessInlineSchema);

            this.validator.ValidationEventHandler += handler;
            this.validator.XmlResolver = resolver;            
            this.validator.Initialize();
            
            this.nsResolver.Context = doc;
            ValidateContent(doc);
            this.nsResolver.Context = doc;

            this.validator.EndValidation();
            xcache.TypeInfoMap = typeInfo; // save schema type information for intellisense.
        }
        
        bool LoadSchemas(XmlDocument doc, XmlSchemaSet set, SchemaResolver resolver) {
            XmlElement root = doc.DocumentElement;
            if (root == null) return false;
            // Give Xsi schemas highest priority.
            bool result = LoadXsiSchemas(doc, set, resolver);

            SchemaCache sc = this.cache.SchemaCache;
            foreach (XmlAttribute a in root.Attributes) {
                if (a.NamespaceURI == "http://www.w3.org/2000/xmlns/") {
                    string nsuri = a.Value;
                    result |= LoadSchemasForNamespace(set, resolver, sc, nsuri, a);
                }
            }
            if (string.IsNullOrEmpty(root.NamespaceURI)){
                result |= LoadSchemasForNamespace(set, resolver, sc, "", root);
            }
            return result;
        }

        private bool LoadSchemasForNamespace(XmlSchemaSet set, SchemaResolver resolver, SchemaCache sc, string nsuri, XmlNode ctx) {
            bool result = false;
            if (set.Schemas(nsuri).Count == 0) {
                CacheEntry ce = sc.FindSchemasByNamespace(nsuri);
                while (ce != null) {
                    if (!ce.Disabled){
                        if (ce.Schema == null) {
                            // delay loaded!
                            LoadSchema(set, resolver, ctx, nsuri, ce.Location.AbsoluteUri);
                        } else {
                            set.Add(ce.Schema);
                        }
                        result = true;
                    }
                    ce = ce.Next;
                }
            }
            return result;
        }

        bool LoadXsiSchemas(XmlDocument doc, XmlSchemaSet set, SchemaResolver resolver) {
            if (doc.DocumentElement == null) return false;
            bool result = false;
            // Give 
            foreach (XmlAttribute a in doc.DocumentElement.Attributes) {
                if (a.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance" ) {
                    if (a.LocalName == "noNamespaceSchemaLocation") {
                        string path = a.Value;
                        if (!string.IsNullOrEmpty(path)) {
                            result = LoadSchema(set, resolver, a, "", a.Value);
                        }
                    } else if (a.LocalName == "schemaLocation") {
                        string[] words = a.Value.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0, n = words.Length; i+1 < n; i++) {
                            string nsuri = words[i];
                            string location = words[++i];
                            result |= LoadSchema(set, resolver, a, nsuri, location);
                        }
                    }
                }
            }
            return result;
        }

        bool LoadSchema(XmlSchemaSet set, SchemaResolver resolver, XmlNode ctx, string nsuri, string filename) {
            try {
                Uri resolved = baseUri == null ? new Uri(filename) : new Uri(baseUri, filename);
                XmlSchema s = resolver.GetEntity(resolved, "", typeof(XmlSchema)) as XmlSchema;
                if ((s.TargetNamespace+"") != (nsuri+"")) {
                    ReportError(Severity.Warning, "Schema must define same target namespace", ctx);
                } else if (!set.Contains(s)) {
                    set.Add(s);
                    return true;
                }
            } catch (Exception e) {
                ReportError(Severity.Warning, "Error loading shema. " + e.Message, ctx);
            }
            return false;
        }

        void ReportError(Severity sev, string msg, XmlNode ctx) {
            if (eh == null) return;
            int line = 0, col = 0;
            string filename = "";
            LineInfo li = cache.GetLineInfo(ctx);
            if (li != null) {
                line = li.LineNumber;
                col = li.LinePosition;
                filename = GetRelative(li.BaseUri);
            }
            eh.HandleError(sev, msg, filename, line, col, ctx);
        }

        void ValidateContent(XmlNode container) {            
            foreach (XmlNode n in container.ChildNodes) {
                // If we are validating up to a given node for intellisense info, then
                // we can prune out any nodes that are not connected to the same parent chain.
                if (parents == null || parents.Contains(n.ParentNode)) {
                    ValidateNode(n);
                }
                if (n == this.node) {
                    break; // we're done!
                }
            }
        }

        void ValidateNode(XmlNode node) {
            XmlElement e = node as XmlElement;
            if (e != null) {
                ValidateElement(e);
                return;
            }
            XmlText t = node as XmlText;
            if (t != null) {
                ValidateText(t);
                return;
            }
            XmlCDataSection cd = node as XmlCDataSection;
            if (cd != null) {
                ValidateText(cd);
                return;
            }
            XmlWhitespace w = node as XmlWhitespace;
            if (w != null) {
                ValidateWhitespace(w);
                return;
            }
        }

        XmlSchemaInfo GetInfo() {
            XmlSchemaInfo i = this.info;
            XmlSchemaInfo copy = new XmlSchemaInfo();
            copy.ContentType = i.ContentType;
            copy.IsDefault = i.IsDefault;
            copy.IsNil = i.IsNil;
            copy.MemberType = i.MemberType;
            copy.SchemaAttribute = i.SchemaAttribute;
            copy.SchemaElement = i.SchemaElement;
            copy.SchemaType = i.SchemaType;
            copy.Validity = i.Validity;
            return copy;
        }

        void ValidateElement(XmlElement e) {
            this.nsResolver.Context = e;
            if (this.node == e && position == IntellisensePosition.OnNode) {
                this.expectedParticles = validator.GetExpectedParticles();
            }
            validator.ValidateElement(e.LocalName, e.NamespaceURI, this.info);
            if (this.info.SchemaType != null) {
                typeInfo[e] = GetInfo();
            }
            foreach (XmlAttribute a in e.Attributes) {
                if (!XmlHelpers.IsXmlnsNode(a)) {
                    ValidateAttribute(a);
                }
            }
            if (this.node == e) {
                this.expectedAttributes = validator.GetExpectedAttributes();
            }
            this.nsResolver.Context = e;
            validator.ValidateEndOfAttributes(this.info);
            if (this.node == e && position == IntellisensePosition.FirstChild) {
                this.expectedParticles = validator.GetExpectedParticles();
            }
            if (this.node != e) {
                ValidateContent(e);
            }
            this.nsResolver.Context = e;            
            validator.ValidateEndElement(this.info);
            if (this.node == e && position == IntellisensePosition.AfterNode) {
                this.expectedParticles = validator.GetExpectedParticles();
            }
            
        }

        void ValidateText(XmlCharacterData text) {
            this.nsResolver.Context = text; 
            validator.ValidateText(new XmlValueGetter(GetText));
        }

        object GetText() {
            return this.nsResolver.Context.InnerText;
        }

        void ValidateWhitespace(XmlWhitespace w) {
            this.nsResolver.Context = w; 
            validator.ValidateWhitespace(w.InnerText);
        }

        void ValidateAttribute(XmlAttribute a) {
            this.nsResolver.Context = a; 
            validator.ValidateAttribute(a.LocalName, a.NamespaceURI, a.Value, this.info);
            typeInfo[a] = GetInfo();
        }

        void OnValidationEvent(object sender, ValidationEventArgs e) {
            if (eh != null) {
                string filename = null;
                int line = 0;
                int col = 0;
                XmlNode node = this.nsResolver.Context;
                Severity sev = e.Severity == XmlSeverityType.Error ? Severity.Error : Severity.Warning;
                XmlSchemaException se = e.Exception;
                if (e.Exception != null) {
                    filename = se.SourceUri;
                    LineInfo li = cache.GetLineInfo(node);
                    if (li != null) {
                        line = li.LineNumber;
                        col = li.LinePosition;
                        filename = GetRelative(li.BaseUri);
                    }
                }
                eh.HandleError(sev, e.Message, filename, line, col, node);
            }
        }

        string GetRelative(string s) {
            if (baseUri == null) return s;
            Uri uri = new Uri(s);
            return this.baseUri.MakeRelative(uri);
        }
    }

    internal class MyXmlNamespaceResolver : System.Xml.IXmlNamespaceResolver {
        System.Xml.XmlNameTable nameTable;
        private XmlNode context;
        private string emptyAtom;

        public MyXmlNamespaceResolver(System.Xml.XmlNameTable nameTable) {
            this.nameTable = nameTable;
            this.emptyAtom = nameTable.Add(string.Empty);
        }

        public XmlNode Context {
            get {
                return this.context;
            }
            set {
                this.context = value;
            }
        }

        public System.Xml.XmlNameTable NameTable {
            get {
                return this.nameTable;
            }
        }

        private string Atomized(string s) {
            if (s == null) return null;
            if (s.Length == 0) return this.emptyAtom;
            return this.nameTable.Add(s);
        }

        public string LookupPrefix(string namespaceName, bool atomizedName) {
            string result = null;
            if (context != null) {
                result = context.GetPrefixOfNamespace(namespaceName);
            }
            return Atomized(result);
        }

        public string LookupPrefix(string namespaceName) {
            string result = null;
            if (context != null) {
                result = context.GetPrefixOfNamespace(namespaceName);
            }
            return Atomized(result);
        }

        public string LookupNamespace(string prefix, bool atomizedName) {
            return LookupNamespace(prefix);
        }

        public string LookupNamespace(string prefix) {
            string result = null;
            if (context != null) {
                result = context.GetNamespaceOfPrefix(prefix);
            }            
            return Atomized(result);
        }

        public IDictionary<string, string> GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (this.context != null) {
                foreach (XmlAttribute a in this.context.SelectNodes("namespace::*")) {
                    string nspace = a.InnerText;
                    string prefix = a.Prefix;
                    if (prefix == "xmlns") {
                        prefix = "";
                    }
                    dict[prefix] = nspace;
                }
            }
            return dict;
        }

    }

}
