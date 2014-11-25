using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.ComponentModel;
using System.Collections;
using System.Reflection;

namespace XmlNotepad {
    public class XmlIntellisenseProvider : IIntellisenseProvider, IDisposable {
        Hashtable typeCache = new Hashtable();
        XmlCache model;
        XmlTreeNode node;
        XmlNode xn;
        Checker checker;
        ISite site;

        const string vsIntellisense = "http://schemas.microsoft.com/Visual-Studio-Intellisense";

        public XmlIntellisenseProvider(XmlCache model, ISite site) {
            this.model = model;
            this.site = site;
        }

        public virtual Uri BaseUri {
            get { return this.model != null ? this.model.Location : null;  }
        }

        public virtual bool IsNameEditable { get { return true; } }

        public virtual bool IsValueEditable { get { return true; } }

        public void SetContextNode(TreeNode node) {
            this.ContextNode = node;
            OnContextChanged();
        }

        public TreeNode ContextNode {
            get { return node; }
            set { node = value as XmlTreeNode; }
        }

        public virtual void OnContextChanged() {
            this.checker = null;
           
            // Get intellisense for elements and attributes
            if (this.node.NodeType == XmlNodeType.Element ||
                this.node.NodeType == XmlNodeType.Attribute ||
                this.node.NodeType == XmlNodeType.Text ||
                this.node.NodeType == XmlNodeType.CDATA) {
                XmlTreeNode elementNode = GetClosestElement(this.node);
                if (elementNode != null && elementNode.NodeType == XmlNodeType.Element) {
                    this.xn = elementNode.Node;
                    if (xn is XmlElement) {
                        this.checker = new Checker((XmlElement)xn,
                            elementNode == this.node.Parent ? IntellisensePosition.FirstChild :
                            (this.node.Node == null ? IntellisensePosition.AfterNode : IntellisensePosition.OnNode)
                            );
                        this.checker.Validate(model);
                    }
                }
            }
        }

        static XmlTreeNode GetClosestElement(XmlTreeNode treeNode) {
            XmlTreeNode element = treeNode.Parent as XmlTreeNode;
            if (treeNode.Parent != null) {
                foreach (XmlTreeNode child in treeNode.Parent.Nodes) {
                    if (child.Node != null && child.NodeType == XmlNodeType.Element) {
                        element = child;
                    }
                    if (child == treeNode)
                        break;
                }
            }
            return element;
        }

        public virtual XmlSchemaType GetSchemaType() {
            XmlSchemaInfo info = GetSchemaInfo();
            return info != null ? info.SchemaType : null;
        }

        XmlSchemaInfo GetSchemaInfo() {
            XmlTreeNode tn = node;
            if (tn.NodeType == XmlNodeType.Text ||
                tn.NodeType == XmlNodeType.CDATA) {
                tn = (XmlTreeNode)tn.Parent;
            }
            if (tn == null) return null;
            XmlNode xn = tn.Node;
            if (xn != null && model != null) {
                XmlSchemaInfo info = model.GetTypeInfo(xn);
                return info;
            }
            return null;
        }

        public virtual string GetDefaultValue() {
            XmlSchemaInfo info = GetSchemaInfo();
            if (info != null) {
                if (info.SchemaAttribute != null) {
                    return info.SchemaAttribute.DefaultValue;
                } else if (info.SchemaElement != null) {
                    return info.SchemaElement.DefaultValue;
                }   
            }
            return null;
        }

        public virtual string[] GetExpectedValues() {
            XmlSchemaType type = GetSchemaType();
            if (type != null) {
                return model.SchemaCache.GetValidValues(type);
            }
            return null;
        }

        public virtual string[] GetExpectedNames() {
            if (checker != null) {
                ArrayList list = new ArrayList();
                if (node.NodeType == XmlNodeType.Attribute) {
                    XmlSchemaAttribute[] expected = checker.GetExpectedAttributes();
                    if (expected != null) {
                        foreach (XmlSchemaAttribute a in expected) {
                            list.Add(GetQualifiedName(a));
                        }
                    }
                } else {
                    XmlSchemaParticle[] particles = checker.GetExpectedParticles();
                    if (particles != null) {
                        foreach (XmlSchemaParticle p in particles) {
                            if (p is XmlSchemaElement) {
                                list.Add(GetQualifiedName((XmlSchemaElement)p));
                            } else {
                                // todo: expand XmlSchemaAny particles.
                            }
                        }
                    }
                }
                return (string[])list.ToArray(typeof(string));
            }
            return null;
        }

        static string GetUnhandledAttribute(XmlAttribute[] attributes, string localName, string nsuri) {
            if (attributes != null) {
                foreach (XmlAttribute a in attributes) {
                    if (a.LocalName == localName && a.NamespaceURI == nsuri) {
                        return a.Value;
                    }
                }
            }
            return null;
        }

        public string GetIntellisenseAttribute(string name) {
            string value = null;
            XmlSchemaInfo info = GetSchemaInfo();
            if (info != null) {
                if (info.SchemaElement != null) {
                    value = GetUnhandledAttribute(info.SchemaElement.UnhandledAttributes, name, vsIntellisense);
                }
                if (info.SchemaAttribute != null) {
                    value = GetUnhandledAttribute(info.SchemaAttribute.UnhandledAttributes, name, vsIntellisense);
                }
                if (value == null && info.SchemaType != null) {
                    value = GetUnhandledAttribute(info.SchemaType.UnhandledAttributes, name, vsIntellisense);
                }
            }
            return value;
        }

        public virtual IXmlBuilder Builder {
            get {
                string typeName = GetIntellisenseAttribute("builder");
                if (!string.IsNullOrEmpty(typeName)) {
                    return ConstructType(typeName) as IXmlBuilder;
                }

                // Some default builders.
                XmlSchemaType type = GetSchemaType();
                if (type != null) {
                    switch (type.TypeCode) {
                        case XmlTypeCode.AnyUri:
                            IXmlBuilder builder = ConstructType("XmlNotepad.UriBuilder") as IXmlBuilder;
                            if (builder != null) builder.Owner = this;
                            return builder;
                    }
                }
                return null;
            }
        }

        public virtual IXmlEditor Editor {
            get {
                string typeName = GetIntellisenseAttribute("editor");
                if (!string.IsNullOrEmpty(typeName)) {
                    return ConstructType(typeName) as IXmlEditor;
                }

                // Some default editors.
                XmlSchemaType type = GetSchemaType();
                if (type != null) {
                    switch (type.TypeCode) {
                        case XmlTypeCode.Date:
                        case XmlTypeCode.DateTime:
                        case XmlTypeCode.Time:
                            IXmlEditor editor = ConstructType("XmlNotepad.DateTimeEditor") as IXmlEditor;
                            if (editor != null) editor.Owner = this;
                            return editor;
                    }
                }
                return null;
            }
        }

        object ConstructType(string typeName) {
            // Cache the objects so they can preserve user state.
            if (typeCache.ContainsKey(typeName))
                return typeCache[typeName];

            Type t = Type.GetType(typeName);
            if (t != null) {
                ConstructorInfo ci = t.GetConstructor(new Type[0]);
                if (ci != null) {
                    object result = ci.Invoke(new Object[0]);
                    if (result != null) {
                        typeCache[typeName] = result;
                        return result;
                    }
                }
            }
            return null;
        }

        public string GetQualifiedName(XmlSchemaAttribute a) {
            string name = a.Name;
            string nsuri = null;
            if (a.QualifiedName != null) {
                name = a.QualifiedName.Name;
                nsuri = a.QualifiedName.Namespace;
            } else if (a.RefName != null) {
                name = a.QualifiedName.Name;
                nsuri = a.QualifiedName.Namespace;
            } else {
                nsuri = GetSchema(a).TargetNamespace;
            }
            if (!string.IsNullOrEmpty(nsuri) && this.xn != null) {
                string prefix = this.xn.GetPrefixOfNamespace(nsuri);
                if (!string.IsNullOrEmpty(prefix)) {
                    return prefix + ":" + name;
                }
            }
            return name;
        }

        public string GetQualifiedName(XmlSchemaElement e) {
            string name = e.Name;
            string nsuri = null;
            if (e.QualifiedName != null) {
                name = e.QualifiedName.Name;
                nsuri = e.QualifiedName.Namespace;
            } else if (e.RefName != null) {
                name = e.QualifiedName.Name;
                nsuri = e.QualifiedName.Namespace;
            } else {
                nsuri = GetSchema(e).TargetNamespace;
            }
            if (!string.IsNullOrEmpty(nsuri) && this.xn != null) {
                string prefix = this.xn.GetPrefixOfNamespace(nsuri);
                if (!string.IsNullOrEmpty(prefix)) {
                    return prefix + ":" + name;
                }
            }
            return name;
        }

        XmlSchema GetSchema(XmlSchemaObject o) {
            if (o == null) return null;
            if (o is XmlSchema) return (XmlSchema)o;
            return GetSchema(o.Parent);
        }

        ~XmlIntellisenseProvider() {
            Dispose(false);
        }

        #region IDisposable Members

        public void Dispose() {
            Dispose(true);
        }

        #endregion

        protected virtual void Dispose(bool disposing) {
            if (this.typeCache != null) {
                foreach (object value in this.typeCache.Values) {
                    IDisposable d = value as IDisposable;
                    if (d != null) d.Dispose();
                }
                this.typeCache = null;
            }
        }
    }


}
