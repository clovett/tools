using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Diagnostics;

namespace DependencyViewer {

    public class XsdGraphGenerator : GraphGenerator {
        IDictionary<XmlSchemaObject, GraphNode> typemap;
        IDictionary<string, GraphNode> refmap;
        XmlSchemaSet set;
        GraphType type = GraphType.Type;
        GraphFlags flags = GraphFlags.ComplexTypes | GraphFlags.SimpleTypes | GraphFlags.Elements | GraphFlags.Attributes;
        MenuItem menu;

        enum GraphType {
            Type,
            Dependencies,
            Files
        }

        enum GraphFlags {
            Elements = 1,
            Attributes = 2,
            ComplexTypes = 4,
            SimpleTypes = 8,
            Groups = 16,
            AttributeGroups = 32,
        }

        public XsdGraphGenerator() {
        }

        public override void SaveState(ViewState state) {
            base.SaveState(state);
            state["GraphType"] = type;
            state["GraphFlags"] = flags;
        }

        public override void LoadState(ViewState state) {
            base.LoadState(state);
            type = (GraphType)state["GraphType"];
            flags = (GraphFlags)state["GraphFlags"];
            UpdateMenu();
        }

        
        public override string Label {
            get {
                return this.type.ToString();
            }
        }

        public override string FileFilter {
            get {
                return "XML Schemas (*.xsd)|*.xsd";
            }
        }

        public override void CreateViewMenu(MenuItem menu) {
            this.menu = menu;
            CreateMenuItem("_Types", type == GraphType.Type, GraphType.Type);
            CreateMenuItem("_Files", type == GraphType.Files, GraphType.Files);
            CreateMenuItem("_Dependencies", type == GraphType.Dependencies, GraphType.Dependencies);
            menu.Items.Add(new Separator());
            CreateMenuItem("_Elements", ShowElements, GraphFlags.Elements);
            CreateMenuItem("_Attributes", ShowAttributes, GraphFlags.Attributes);
            CreateMenuItem("_Complex Types", ShowComplexTypes, GraphFlags.ComplexTypes);
            CreateMenuItem("_Simple Types", ShowSimpleTypes, GraphFlags.SimpleTypes);
            CreateMenuItem("_Groups", ShowGroups, GraphFlags.Groups);
            CreateMenuItem("_Attribute Groups", ShowAttributeGroups, GraphFlags.AttributeGroups);
        }

        void CreateMenuItem(string label, bool check, object flag) {
            MenuItem item = new MenuItem();
            item.Header = label;
            item.IsChecked = check;
            item.Tag = flag;
            item.Click += new RoutedEventHandler(OnClick);
            menu.Items.Add(item);
        }

        void OnClick(object sender, RoutedEventArgs e) {
            OnBeforeChange();
            MenuItem item = (MenuItem)sender;
            if (item.Tag is GraphType) {
                type = (GraphType)item.Tag;
            } else {
                ToggleFlag((GraphFlags)item.Tag);
            }
            UpdateMenu();
            OnAfterChange();
        }


        void UpdateMenu() {
            foreach (object item in menu.Items) {
                MenuItem mi = item as MenuItem;
                if (mi != null) {
                    if (mi.Tag is GraphType) {
                        GraphType t = (GraphType)mi.Tag;
                        mi.IsChecked = (this.type == t);
                    } else if (mi.Tag is GraphFlags) {
                        GraphFlags f = (GraphFlags)mi.Tag;
                        mi.IsChecked = (this.flags & f) != 0;
                    }
                }
            }
        }

        bool ToggleFlag(GraphFlags flag) {
            if ((flags & flag) != 0) {
                flags &= ~flag;
                return false;
            } else {
                flags |= flag;
                return true;
            }
        }

        public bool ShowComplexTypes {
            get { return (flags & GraphFlags.ComplexTypes) != 0; }
        }

        public bool ShowSimpleTypes {
            get { return (flags & GraphFlags.SimpleTypes) != 0; }
        }

        public bool ShowElements {
            get { return (flags & GraphFlags.Elements) != 0; }
        }

        public bool ShowAttributes {
            get { return (flags & GraphFlags.Attributes) != 0; }
        }

        public bool ShowGroups {
            get { return (flags & GraphFlags.Groups) != 0; }
        }

        public bool ShowAttributeGroups {
            get { return (flags & GraphFlags.AttributeGroups) != 0; }
        }

        public override void Prepare() {
            base.Prepare();
            set = new XmlSchemaSet();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            ValidationEventHandler handler = new ValidationEventHandler(OnValidationError);
            foreach (string file in this.FileNames) {
                using (XmlReader r = XmlReader.Create(file, settings)) {
                    XmlSchema s = XmlSchema.Read(r, handler);
                    if (s != null) set.Add(s);
                }
            }
            set.ValidationEventHandler += handler;
            set.Compile();            
        }

        void OnValidationError(object sender, ValidationEventArgs e) {
        }

        // Statically load the specified assemblies and return the type graphs or dependency graph.
        public override void Create(Panel container) {
            if (set == null)
                throw new InvalidOperationException("Must call Prepare first");

            base.Create(container);
            typemap = new Dictionary<XmlSchemaObject, GraphNode>();
            refmap = new Dictionary<string, GraphNode>();
            
            switch (type) {
                case GraphType.Type:
                    AddTypes();
                    break;
                case GraphType.Dependencies:
                    AddDependencies();
                    break;
                case GraphType.Files:
                    AddFiles();
                    break;       
            }
        }

        bool IncludeType(XmlSchemaType t) {
            return t != null && t.SourceUri != null;
        }

        bool IsAnonymous(XmlSchemaType t) {
            return string.IsNullOrEmpty(t.QualifiedName.Name);
        }

        void AddTypes() {
            if (this.ShowElements) {
                foreach (XmlSchemaElement e in set.GlobalElements.Values) {
                    GraphNode node = AddType(e);
                    if (node != null) {
                        XmlSchemaType bt = e.ElementSchemaType;
                        AddBaseTypes(node, bt);
                    }
                }
            }
            if (this.ShowAttributes) {
                foreach (XmlSchemaAttribute a in set.GlobalAttributes.Values) {
                    GraphNode node = AddType(a);
                    if (node != null ) {
                        XmlSchemaType bt = a.AttributeSchemaType;
                        AddBaseTypes(node, bt);
                    }
                }
            }

            if (ShowGroups) {
                foreach (XmlSchema s in set.Schemas()) {
                    foreach (XmlSchemaGroup g in s.Groups.Values) {
                        AddGroup(g);
                    }
                }
            }
            if (ShowAttributeGroups) {
                foreach (XmlSchema s in set.Schemas()) {
                    foreach (XmlSchemaAttributeGroup g in s.AttributeGroups.Values) {
                        AddAttributeGroup(g);
                    }
                }
            }
            foreach (XmlSchemaType t in set.GlobalTypes.Values) {
                GraphNode node = AddType(t);
                if (node != null) {
                    XmlSchemaType bt = t.BaseXmlSchemaType;
                    AddBaseTypes(node, bt);
                }
            }
        }

        private void AddBaseTypes(GraphNode node, XmlSchemaType bt) {
            while (IncludeType(bt) && !IsAnonymous(bt)) {
                GraphNode basenode = AddType(bt);
                if (basenode != null) {
                    AddEdge(node, basenode, "Inherits");
                    node = basenode;
                    bt = bt.BaseXmlSchemaType;
                } else {
                    break;
                }
            }
        }

        GraphNode AddType(XmlSchemaObject so) {
            if ((this.ShowSimpleTypes && so is XmlSchemaSimpleType) ||
                (this.ShowComplexTypes && so is XmlSchemaComplexType) ||
                (this.ShowElements && so is XmlSchemaElement) ||
                (this.ShowAttributes && so is XmlSchemaAttribute) ||
                (this.ShowGroups && (so is XmlSchemaGroup || so is XmlSchemaGroupRef)) ||
                (this.ShowAttributeGroups && (so is XmlSchemaAttributeGroup || so is XmlSchemaAttributeGroupRef))) {
                return AddNode(so);
            }
            return null;
        }

        void AddDependencies() {
            if (ShowElements) {
                foreach (XmlSchemaElement e in set.GlobalElements.Values) {
                    AddElement(e);
                }
            }
            if (ShowAttributes) {
                foreach (XmlSchemaAttribute a in set.GlobalAttributes.Values) {
                    AddAttribute(a);
                }
            }
            if (ShowGroups) {
                foreach (XmlSchema s in set.Schemas()) {
                    foreach (XmlSchemaGroup g in s.Groups.Values) {
                        AddGroup(g);
                    }
                }
            }
            if (ShowAttributeGroups) {
                foreach (XmlSchema s in set.Schemas()) {
                    foreach (XmlSchemaAttributeGroup g in s.AttributeGroups.Values) {
                        AddAttributeGroup(g);
                    }
                }
            }
            foreach (XmlSchemaType t in set.GlobalTypes.Values) {
                GraphNode node = AddType(t);
                if (node != null && IncludeType(t)) {                    
                    XmlSchemaComplexType ct = t as XmlSchemaComplexType;
                    if (ct != null) {
                        if (ShowComplexTypes) AddComplexType(node, ct); 
                    } else {
                        if (ShowSimpleTypes) AddSimpleType(node, t as XmlSchemaSimpleType);
                    }
                }
            }
        }

        private GraphNode AddAttribute(XmlSchemaAttribute a) {
            GraphNode node = AddType(a);
            if (node != null) {
                XmlSchemaType bt = a.AttributeSchemaType;
                if (IncludeType(bt)) {
                    GraphNode basenode = AddNode(bt);
                    if (basenode != null) AddEdge(node, basenode, "AttributeType");
                }
                AddSimpleType(node, bt as XmlSchemaSimpleType);
            }
            return node;
        }

        private GraphNode AddGroup(XmlSchemaGroup g) {
            GraphNode node = AddType(g);
            if (node != null) {
                AddParticle(node, g.Particle);
            }
            return node;
        }


        private GraphNode AddAttributeGroup(XmlSchemaAttributeGroup g) {
            GraphNode node = AddType(g);
            if (node != null) {
                foreach (XmlSchemaObject o in g.Attributes) {
                    XmlSchemaAttribute a = o as XmlSchemaAttribute;
                    if (a != null && ShowAttributes) {
                        GraphNode an = AddType(a);
                        if (an != null) {
                            AddEdge(node, an, "AttributeType");
                        }
                    }
                    XmlSchemaAttributeGroupRef gr = o as XmlSchemaAttributeGroupRef;
                    if (gr != null) {
                        GraphNode gn = AddType(gr);
                        if (gn != null) {
                            AddEdge(node, gn, "AttriuteGroup");
                        }
                    }
                }
            }
            return node;
        }

        private GraphNode AddElement(XmlSchemaElement e) {
            GraphNode node = AddType(e);
            if (node != null) {
                XmlSchemaType bt = e.ElementSchemaType;
                if (IncludeType(bt)) {
                    XmlSchemaComplexType ct = bt as XmlSchemaComplexType;
                    if (ct != null) {
                        if (ShowComplexTypes) AddComplexType(node, ct);
                    } else {
                        if (ShowSimpleTypes) AddSimpleType(node, bt as XmlSchemaSimpleType);
                    }
                }
            }
            return node;
        }

        Dictionary<XmlSchemaComplexType, XmlSchemaComplexType> guard = new Dictionary<XmlSchemaComplexType, XmlSchemaComplexType>(); 

        void AddComplexType(GraphNode from, XmlSchemaComplexType ct) {

            if (ct == null || ct.SourceUri == null || guard.ContainsKey(ct)) 
                return;
            guard[ct] = ct;
            if (!IsAnonymous(ct)) {
                GraphNode basenode = AddNode(ct);
                AddEdge(from, basenode, "ComplexType");
                from = basenode;
            }

            if (ShowAttributes) AddAttributes(from, ct.Attributes);
            AddParticle(from, ct.ContentTypeParticle);
            guard.Remove(ct);
        }

        void AddSimpleType(GraphNode from, XmlSchemaSimpleType st) {
            if (st == null || st.SourceUri == null)
                return;

            if (!IsAnonymous(st)) {
                GraphNode basenode = AddNode(st);
                AddEdge(from, basenode, "SimpleType");
                from = basenode;
            }

            XmlSchemaSimpleTypeContent cc = st.Content;
            if (cc != null) {
                XmlSchemaSimpleTypeList list = cc as XmlSchemaSimpleTypeList;
                if (list != null) {
                    AddSimpleType(from, list.ItemType);
                    return;
                }
                XmlSchemaSimpleTypeUnion union = cc as XmlSchemaSimpleTypeUnion;
                if (union != null) {
                    if (union.MemberTypes != null) {
                        foreach (XmlQualifiedName name in union.MemberTypes) {
                            XmlSchemaSimpleType ut = set.GlobalTypes[name] as XmlSchemaSimpleType;
                            AddSimpleType(from, ut);
                        }
                    }
                    return;
                }
                XmlSchemaSimpleTypeRestriction r = cc as XmlSchemaSimpleTypeRestriction;
                if (r != null) {
                    XmlSchemaSimpleType bt = r.BaseType;
                    AddSimpleType(from, bt);
                }
            }
        }

        void AddParticle(GraphNode from, XmlSchemaParticle p) {
            XmlSchemaElement e = p as XmlSchemaElement;
            if (e != null && ShowElements) {
                XmlQualifiedName name = e.RefName;
                if (name != null) {
                    GraphNode anode = AddNode(e);
                    AddEdge(from, anode, "ElementRef");
                }
                XmlSchemaType t = e.ElementSchemaType;
                if (IncludeType(t)) {
                    if (IsAnonymous(t)) {
                        //GraphNode enode = AddElement(e);
                        //AddEdge(from, enode);
                    } else {
                        GraphNode anode = AddNode(t);
                        AddEdge(from, anode, "ElementType");                    
                    }
                }
                return;
            }
            
            XmlSchemaGroupRef gr = p as XmlSchemaGroupRef;
            if (gr != null && ShowGroups) {
                XmlQualifiedName name = gr.RefName;
                if (name != null) {
                    GraphNode anode = AddNode(gr);
                    AddEdge(from, anode, "GroupRef");
                }
            }

            XmlSchemaGroupBase g = p as XmlSchemaGroupBase;
            if (g != null) {
                foreach (XmlSchemaParticle sp in g.Items) {
                    AddParticle(from, sp);
                }
                return;
            }
        }

        void AddAttributes(GraphNode from, XmlSchemaObjectCollection c) {
            foreach (XmlSchemaObject o in c) {
                XmlSchemaAttribute a = o as XmlSchemaAttribute;
                if (a != null) {
                    XmlQualifiedName name = a.RefName;
                    if (name != null) {
                        GraphNode anode = AddNode(a);
                        AddEdge(from, anode, "Attribute");
                    }
                }
            }
        }

        private XmlQualifiedName GetName(XmlSchemaObject so) {
            XmlSchemaType t = so as XmlSchemaType;
            if (t != null) return t.QualifiedName;
            XmlSchemaElement e = so as XmlSchemaElement;
            if (e != null) return e.QualifiedName;
            XmlSchemaAttribute a = so as XmlSchemaAttribute;
            if (a != null) return a.QualifiedName;
            XmlSchemaAttributeGroup ag = so as XmlSchemaAttributeGroup;
            if (ag != null) return ag.QualifiedName;
            XmlSchemaGroup g = so as XmlSchemaGroup;
            if (g != null) return g.QualifiedName;
            XmlSchema s = so as XmlSchema;
            if (s != null) return new XmlQualifiedName(GetSchemaLabel(s));
            return null;
        }


        private XmlQualifiedName GetRefName(XmlSchemaObject so) {
            XmlSchemaElement e = so as XmlSchemaElement;
            if (e != null) return e.RefName;
            XmlSchemaAttribute a = so as XmlSchemaAttribute;
            if (a != null) return a.RefName;
            XmlSchemaGroupRef gr = so as XmlSchemaGroupRef;
            if (gr != null) return gr.RefName;
            XmlSchemaAttributeGroupRef agr = so as XmlSchemaAttributeGroupRef;
            if (agr != null) return agr.RefName;
            return null;
        }

        string GetFileName(string path) {
            if (string.IsNullOrEmpty(path)) return null;
            Uri uri = new Uri(path);
            return uri.Segments[uri.Segments.Length - 1];

        }

        private string GetNodeType(XmlSchemaObject so) {
            XmlSchemaSimpleType st = so as XmlSchemaSimpleType;
            if (st != null) return "SimpleType";
            XmlSchemaComplexType ct = so as XmlSchemaComplexType;
            if (ct != null) return "ComplexType";
            XmlSchemaElement e = so as XmlSchemaElement;
            if (e != null) return "Element";
            XmlSchemaAttribute a = so as XmlSchemaAttribute;
            if (a != null) return "Attribute";
            XmlSchemaGroup g = so as XmlSchemaGroup;
            if (g != null) return "Group";
            XmlSchemaAttributeGroup ag = so as XmlSchemaAttributeGroup;
            if (ag != null) return "AttributeGroup";
            return "Normal";
        }

        private void AddDependency(GraphNode from, XmlSchemaObject so, string category) {
            GraphNode refnode = AddNode(so);
            AddEdge(from, refnode, category);
        }

        internal GraphNode AddNode(XmlSchemaObject so) {
            bool refType = false;
            XmlQualifiedName name = GetRefName(so);
            if (name == null || string.IsNullOrEmpty(name.Name)) {
                name = GetName(so);
            } else {
                refType = true;
            }
            if (name == null || string.IsNullOrEmpty(name.Name)) return null;
            
            if (typemap.ContainsKey(so)) {
                return typemap[so];
            }
            string id = so.GetType().Name + ":" + name.ToString();
            if (refType && refmap.ContainsKey(id)) {
                return refmap[id];
            }
                
            string label = name.Name;
            if (string.IsNullOrEmpty(label)) {
                return null;
            } else {
                GraphNode n = new GraphNode();
                n.Id = id;
                n.Label = label;
                n.NodeType = GetNodeType(so);
                typemap[so] = n;

                AddNode(n);
                if (refType) {
                    refmap[id] = n;
                }
                return n;
            }
        }

        string GetTargetNamespace(XmlSchema s) {
            string nsuri = s.TargetNamespace;
            if (nsuri == null) nsuri = string.Empty;
            return nsuri;
        }

        IDictionary<string, GraphNode> nsmap;

        void AddFiles() {
            nsmap = new Dictionary<string, GraphNode>();
            // Add unique namespace subgraphs.
            foreach (XmlSchema s in set.Schemas()) {
                string nsuri = GetTargetNamespace(s);
                if (!nsmap.ContainsKey(nsuri)) {
                    GraphNode node = new GraphNode();
                    string id = string.IsNullOrEmpty(nsuri) ? "empty" : nsuri;
                    node.Id = id;
                    node.Label = nsuri;
                    node.NodeType = "Namespace";
                    AddNode(node);
                    nsmap[nsuri] = node;
                }
            }
            // Now add individual schemas in each subgraph.
            foreach (XmlSchema s in set.Schemas()) {
                GraphNode root = AddSchema(s);
            }

            _includes = new Dictionary<Uri, XmlSchema>();

            // Now add edges
            foreach (XmlSchema s in set.Schemas()) {
                GraphNode root = AddSchema(s);
                AddIncludes(root, s);
            }
        }

        string GetSchemaLabel(XmlSchema s) {
            string fname = GetFileName(s.SourceUri);
            if (!string.IsNullOrEmpty(fname)) {
                return fname;
            }
            fname = GetTargetNamespace(s);
            if (fname == "http://www.w3.org/XML/1998/namespace") {
                return "xml.xsd";
            }
            return fname;
        }

        GraphNode AddSchema(XmlSchema s) {
            GraphNode node = null;

            // We do NOT call the base.AddNode here because we do not want a separate GraphCanvas for the leaf nodes.
            if (typemap.ContainsKey(s)) {
                node = typemap[s];
            } else {
                string label = GetSchemaLabel(s);

                node = new GraphNode();
                node.Id = label;
                node.Label = label;
                node.NodeType = "Schema";
                typemap[s] = node;
            }

            // Add it to subgraph.
            string nsuri = GetTargetNamespace(s);
            GraphNode nsgraph = nsmap[nsuri];
            if (!nsgraph.Nodes.Contains(node)) {
                nsgraph.Nodes.Add(node);
                node.Parent = nsgraph;
            }

            Debug.Assert(node.Parent == nsgraph);
            return node;
        }

        IDictionary<Uri, XmlSchema> _includes;

        void AddIncludes(GraphNode parent, XmlSchema s) {
            if (s.SourceUri != null)
            {
                Uri uri = new Uri(s.SourceUri);
                if (_includes.ContainsKey(uri))
                {
                    return;
                }
                _includes[uri] = s;
            }
            foreach (XmlSchemaExternal e in s.Includes) {
                XmlSchemaImport import = e as XmlSchemaImport;
                GraphNode nsgraph = nsmap[GetTargetNamespace(s)];
                    
                if (import != null) {
                    // Connect subgraphs
                    if (nsmap.ContainsKey(import.Namespace)) {
                        GraphNode importGraph = nsmap[import.Namespace];
                        GraphEdge edge = AddEdge(nsgraph, importGraph, "Imports");
                        if (edge != null) edge.EdgeType = "Import";
                    }
                } else  {
                    // redefine and include are similar - we want edges between the two schemas.
                    XmlSchema include = e.Schema;
                    if (include != null) {
                        GraphNode child = AddSchema(include);
                        if (child != null) {
                            Debug.Assert(parent.Parent != null && parent.Parent == child.Parent);
                            GraphEdge edge = AddEdge(parent, child, "Includes");
                            if (edge != null) {
                                nsgraph.Edges.Add(edge);
                                edge.EdgeType = e is XmlSchemaRedefine ? "Redefine" : "Include";
                            }
                            AddIncludes(child, include);
                        }
                    }
                }
            }
        }
    }
}
