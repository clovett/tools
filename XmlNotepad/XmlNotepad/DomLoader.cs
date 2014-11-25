using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;

namespace XmlNotepad {
    /// <summary>
    /// This class keeps track of DOM node line locations so you can do error reporting.
    /// </summary>
    class DomLoader {
        Dictionary<XmlNode, LineInfo> lineTable = new Dictionary<XmlNode, LineInfo>();
        XmlDocument doc;
        XmlReader reader;
        const string xsiUri = "http://www.w3.org/2001/XMLSchema-instance";

        public DomLoader() {
        }

        void AddToTable(XmlNode node) {
            lineTable[node] = new LineInfo(reader);
        }

        public LineInfo GetLineInfo(XmlNode node) {
            if (node != null && lineTable.ContainsKey(node)) {
                return lineTable[node];
            }
            return null;
        }

        public XmlDocument Load(XmlReader r) {
            this.lineTable = new Dictionary<XmlNode, LineInfo>();
            this.doc = new XmlDocument();
            this.reader = r;            
            AddToTable(this.doc);
            LoadDocument();
            return doc;
        }

        private void LoadDocument() {
            bool preserveWhitespace = false;
            XmlReader r = this.reader;
            XmlNode parent = this.doc;
            XmlElement element;
            while (r.Read()) {
                XmlNode node = null;                
                switch (r.NodeType) {
                    case XmlNodeType.Element:
                        bool fEmptyElement = r.IsEmptyElement;
                        element = doc.CreateElement(r.Prefix, r.LocalName, r.NamespaceURI);
                        AddToTable(element);
                        element.IsEmpty = fEmptyElement;
                        ReadAttributes(r, element);

                        if (!fEmptyElement) {
                            parent.AppendChild(element);
                            parent = element; 
                            continue;
                        } 
                        node = element;
                        break;

                    case XmlNodeType.EndElement:
                        if (parent.ParentNode == null) {
                            // syntax error in document.
                            IXmlLineInfo li = (IXmlLineInfo)r;
                            throw new XmlException(string.Format("Unexpected end tag '{0}' at line {1} column {2}",
                                r.LocalName, li.LineNumber, li.LinePosition), null, li.LineNumber, li.LinePosition);
                        }
                        parent = parent.ParentNode;
                        continue;

                    case XmlNodeType.EntityReference:
                        if (r.CanResolveEntity) {
                            r.ResolveEntity();
                        }
                        continue;

                    case XmlNodeType.EndEntity:
                        continue;

                    case XmlNodeType.Attribute:
                        node = LoadAttributeNode();
                        break;

                    case XmlNodeType.Text:
                        node = doc.CreateTextNode(r.Value);
                        AddToTable(node);
                        break;

                    case XmlNodeType.SignificantWhitespace:
                        node = doc.CreateSignificantWhitespace(r.Value);
                        AddToTable(node);
                        break;

                    case XmlNodeType.Whitespace:
                        if (preserveWhitespace) {
                            node = doc.CreateWhitespace(r.Value);
                            AddToTable(node);
                            break;
                        } else {
                            continue;
                        }
                    case XmlNodeType.CDATA:
                        node = doc.CreateCDataSection(r.Value);
                        AddToTable(node);
                        break;

                    case XmlNodeType.XmlDeclaration:
                        node = LoadDeclarationNode();
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        node = doc.CreateProcessingInstruction(r.Name, r.Value);
                        AddToTable(node);
                        if (r.Name == "xml-stylesheet") {
                            this.xsltFileName = ((XmlProcessingInstruction)node).Data.Split(' ')[0];
                            this.xsltFileName = this.xsltFileName.Split('=')[1].Replace("\"","");
                        }
                        break;

                    case XmlNodeType.Comment:
                        node = doc.CreateComment(r.Value);
                        AddToTable(node);
                        break;

                    case XmlNodeType.DocumentType:
                        continue;

                    default:
                        UnexpectedNodeType(r.NodeType);
                        break;
                }
                
                Debug.Assert(node != null);
                Debug.Assert(parent != null);
                if (parent != null) {
                    parent.AppendChild(node);
                }
            }        
        }

        private void ReadAttributes(XmlReader r, XmlElement element) {
            if (r.MoveToFirstAttribute()) {
                XmlAttributeCollection attributes = element.Attributes;
                do {
                    XmlAttribute attr = LoadAttributeNode();
                    attributes.Append(attr); // special case for load
                }
                while (r.MoveToNextAttribute());
                r.MoveToElement();
            }
        }
        string xsltFileName=null;
        public string XsltFileName {
            get { return this.xsltFileName; }
        }
        private XmlAttribute LoadAttributeNode() {
            Debug.Assert(reader.NodeType == XmlNodeType.Attribute);

            XmlReader r = reader;
            XmlAttribute attr = doc.CreateAttribute(r.Prefix, r.LocalName, r.NamespaceURI);
            AddToTable(attr);
            XmlNode parent = attr;
            
            while (r.ReadAttributeValue()) {
                XmlNode node = null;
                switch (r.NodeType) {
                    case XmlNodeType.Text:
                        node = doc.CreateTextNode(r.Value);
                        AddToTable(node);
                        break;
                    case XmlNodeType.EntityReference:
                        if (r.CanResolveEntity) {
                            r.ResolveEntity();                      
                        }
                        continue;
                    case XmlNodeType.EndEntity:
                        continue;
                    default:
                        UnexpectedNodeType(r.NodeType); 
                        break;
                }
                Debug.Assert(node != null);
                parent.AppendChild(node);
            }
            if (attr.NamespaceURI == xsiUri) {
                HandleXsiAttribute(attr);
            }
            return attr;
        }

        void HandleXsiAttribute(XmlAttribute a) {
            switch (a.LocalName) {
                case "schemaLocation":
                    LoadSchemaLocations(a.Value);
                    break;
                case "noNamespaceSchemaLocation":
                    LoadSchema(a.Value);
                    break;
            }
        }

        void LoadSchemaLocations(string pairs) {
            string[] words = pairs.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0, n = words.Length; i < n; i++) {                
                if (i + 1 < n) {
                    i++;
                    string url = words[i];
                    LoadSchema(url);
                }
            }
        }

        void LoadSchema(string fname) {
            try {
                Uri resolved = new Uri(new Uri(reader.BaseURI), fname);
                this.doc.Schemas.Add(null, resolved.AbsoluteUri);
            } catch (Exception) {
            }
        }

        private XmlDeclaration LoadDeclarationNode() {
            Debug.Assert(reader.NodeType == XmlNodeType.XmlDeclaration);

            //parse data
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0",null,null);
            AddToTable(decl);

            // Try first to use the reader to get the xml decl "attributes". Since not all readers are required to support this, it is possible to have
            // implementations that do nothing
            while (reader.MoveToNextAttribute()) {
                switch (reader.Name) {
                    case "version":                        
                        break;
                    case "encoding":
                        decl.Encoding = reader.Value;
                        break;
                    case "standalone":
                        decl.Standalone = reader.Value;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            return decl;
        }        

        void UnexpectedNodeType(XmlNodeType type) {
            IXmlLineInfo li = (IXmlLineInfo)reader;
            throw new XmlException(string.Format("Unexpected node type '{0}'", type.ToString()), null,
                li.LineNumber, li.LinePosition);
        }
        
    }

    public class LineInfo : IXmlLineInfo {
        int line, col;
        string baseUri;
        IXmlSchemaInfo info;

        internal LineInfo(int line, int col) {
            this.line = line;
            this.col = col;
        }
        internal LineInfo(XmlReader reader) {
            IXmlLineInfo li = (IXmlLineInfo)reader;
            this.line = li.LineNumber;
            this.col = li.LinePosition;
            this.baseUri = reader.BaseURI;
            this.info = reader.SchemaInfo;
        }
        public bool HasLineInfo() {
            return true;
        }

        public int LineNumber {
            get { return this.line; }
        }

        public int LinePosition {
            get { return this.col; }
        }

        public string BaseUri {
            get { return this.baseUri; }
        }

        public IXmlSchemaInfo SchemaInfo {
            get { return this.info; }
        }
    }
}
