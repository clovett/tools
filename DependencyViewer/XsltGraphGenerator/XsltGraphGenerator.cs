using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Windows;
using System.Windows.Controls;

namespace DependencyViewer {
    public class XsltGraphGenerator : GraphGenerator {
        #region Private Members
        private const ObjectType DefaultFilter = ObjectType.Mode | ObjectType.NamedTemplate | ObjectType.AttributeSet | ObjectType.Attribute;

        private const string XsltNamespace = "http://www.w3.org/1999/XSL/Transform";
        private static readonly ObjectName DefaultModeName = new ObjectName(string.Empty, "#default", string.Empty, ObjectType.Mode);

        // XML whitespace characters, http://www.w3.org/TR/REC-xml#NT-S
        private static readonly char[] WhitespaceChars = new char[] { ' ', '\t', '\n', '\r' };

        // Resolver and reader settings for stylesheet modules
        private XmlResolver xmlResolver;
        private XmlReaderSettings xmlReaderSettings;

        // Helper XmlWriter for validating QNames
        private readonly XmlWriter helperWriter;

        // Filled out on Prepare call
        private Dictionary<Uri, ModuleInfo> modules;
        private Dictionary<ObjectName, ProtoTemplateInfo> protoTemplates;

        // Filled out on Create call
        private Dictionary<Uri, GraphNode> moduleNodes;
        private Dictionary<ObjectName, GraphNode> protoTemplateNodes;

        private GraphType graphType = GraphType.Auto;
        private ObjectType objectFilter = DefaultFilter;
        private MenuItem menu;
        #endregion

        private enum GraphType {
            Auto = 0,
            Modules = 1,
            Templates = 2,
            AttributeSets = 3,
        }

        [Flags]
        private enum ObjectType {
            None = 0,
            Module = 1,
            Mode = 2,
            NamedTemplate = 4,
            GlobalVariable = 8,
            AttributeSet = 0x10,
            Attribute = 0x20,
        }

        // GraphType -> ObjectType filter
        private static readonly ObjectType[] GraphTypeFilter = {
            /* Auto          */   ObjectType.None,
            /* Modules       */   ObjectType.Module,
            /* Templates     */   ObjectType.Mode | ObjectType.NamedTemplate | ObjectType.GlobalVariable,
            /* AttributeSets */   ObjectType.AttributeSet | ObjectType.Attribute
        };

        public XsltGraphGenerator()
        {
            this.xmlResolver = new XmlUrlResolver();
            this.xmlReaderSettings = new XmlReaderSettings();
            this.xmlReaderSettings.ProhibitDtd = false;
            this.xmlReaderSettings.XmlResolver = this.xmlResolver;

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            this.helperWriter = XmlWriter.Create(System.IO.Stream.Null, writerSettings);
        }

        public override string Label {
            get { return this.graphType.ToString(); }
        }


        public override string FileFilter {
            get {
                return "XSLT Stylesheets (*.xsl;*.xslt)|*.xsl;*.xslt";
            }
        }

        #region state persistence

        public override void SaveState(ViewState state) {
            base.SaveState(state);
            state["GraphType"] = graphType;
            state["ObjectType"] = objectFilter;
        }

        public override void LoadState(ViewState state) {
            base.LoadState(state);
            graphType = (GraphType)state["GraphType"];
            objectFilter = (ObjectType)state["ObjectType"];
            UpdateMenu();
        }

        #endregion

        #region View Menu - Populate, Update, and Handle Clicks
        public override void CreateViewMenu(MenuItem menu) {
            CreateMenuItem(menu, "_Module Includes", GraphType.Modules);
            CreateMenuItem(menu, "_Template Calls", GraphType.Templates);
            CreateMenuItem(menu, "_Attribute Sets Definitions", GraphType.AttributeSets);

            menu.Items.Add(new Separator());

            MenuItem filterMenu = new MenuItem();
            filterMenu.Header = "_Filter";
            CreateMenuItem(filterMenu, "_Modes", ObjectType.Mode);
            CreateMenuItem(filterMenu, "_Named Templates", ObjectType.NamedTemplate);
            CreateMenuItem(filterMenu, "_Global Variables", ObjectType.GlobalVariable);
            CreateMenuItem(filterMenu, "_Attributes", ObjectType.Attribute);
            menu.Items.Add(filterMenu);

            this.menu = menu;
            UpdateMenu();
        }

        private void CreateMenuItem(MenuItem menu, string label, object flag) {
            MenuItem item = new MenuItem();
            item.Header = label;
            item.Tag = flag;
            item.Click += new RoutedEventHandler(OnClick);
            menu.Items.Add(item);
        }

        private void OnClick(object sender, RoutedEventArgs e) {
            OnBeforeChange();
            MenuItem item = (MenuItem)sender;
            if (item.Tag is GraphType) {
                this.graphType = (GraphType)item.Tag;
            }
            else if (item.Tag is ObjectType) {
                this.objectFilter ^= (ObjectType)item.Tag;
            }
            UpdateMenu();
            OnAfterChange();
        }

        private void UpdateMenu() {
            UpdateMenu(this.menu);
        }

        private void UpdateMenu(MenuItem menu) {
            foreach (object obj in menu.Items) {
                MenuItem item = obj as MenuItem;
                if (item != null) {
                    if (item.Tag is GraphType) {
                        GraphType type = (GraphType)item.Tag;
                        item.IsChecked = (this.graphType == type);
                    }
                    else if (item.Tag is ObjectType) {
                        ObjectType type = (ObjectType)item.Tag;
                        item.IsEnabled = (GraphTypeFilter[(int)this.graphType] & type) != 0;
                        item.IsChecked = (this.objectFilter & type) != 0;
                    }
                    else {
                        UpdateMenu(item);
                    }
                }
            }
        }
        #endregion

        #region Prepare - Load Stylesheets and Gather Information
        public override void Prepare() {
            base.Prepare();
            this.modules = new Dictionary<Uri, ModuleInfo>();
            this.protoTemplates = new Dictionary<ObjectName, ProtoTemplateInfo>();
            foreach (string file in this.FileNames) {
                AnalyzeModule(new Uri(file));
            }
        }

        private void AnalyzeModule(Uri uri) {
            using (XmlReader r = XmlReader.Create(uri.AbsoluteUri, this.xmlReaderSettings)) {
                XPathDocument doc = new XPathDocument(r);
                ModuleInfo module = new ModuleInfo();
                this.modules[uri] = module;
                XPathNavigator nav = doc.CreateNavigator();

                if (nav.MoveToChild(XPathNodeType.Element) &&
                    (nav.LocalName == "stylesheet" || nav.LocalName == "transform") &&
                    nav.NamespaceURI == XsltNamespace
                ) {
                    bool atTop = true;
                    for (bool result = nav.MoveToChild(XPathNodeType.Element); result; result = nav.MoveToNext(XPathNodeType.Element)) {
                        bool isImport = false;
                        if (nav.NamespaceURI == XsltNamespace) {
                            switch (nav.LocalName) {
                                case "import":
                                    // The xsl:import element children must precede all other element children of an xsl:stylesheet element
                                    if (!atTop)
                                        break;

                                    isImport = true;
                                    goto case "include";

                                case "include":
                                    AnalyzeInclude(nav, module, isImport);
                                    break;

                                case "param":
                                case "variable":
                                    AnalyzeVariable(nav);
                                    break;

                                case "template":
                                    AnalyzeTemplate(nav);
                                    break;

                                case "attribute-set":
                                    AnalyzeAttributeSet(nav);
                                    break;
                            }
                        }
                        atTop = isImport;
                    }
                }
            }
        }

        private void AnalyzeInclude(XPathNavigator nav, ModuleInfo module, bool isImport) {
            string href = nav.GetAttribute("href", string.Empty);
            Uri resolved = this.xmlResolver.ResolveUri(this.xmlResolver.ResolveUri(null, nav.BaseURI), href);
            module.AddInclude(new IncludeInfo(resolved, isImport));

            if (!this.modules.ContainsKey(resolved)) {
                AnalyzeModule(resolved);
            }
        }

        private void AnalyzeVariable(XPathNavigator nav) {
            ObjectName name = ResolveQNameFromAttribute(nav, "name", ObjectType.GlobalVariable);
            if (name != null) {
                ProtoTemplateInfo variable = AddProtoTemplate(name, /*replace:*/true);
                FindCallees(nav, variable, null);
            }
        }

        private void AnalyzeTemplate(XPathNavigator nav) {
            ObjectName mode = ResolveQNameFromAttribute(nav, "mode", ObjectType.Mode);
            ObjectName name = ResolveQNameFromAttribute(nav, "name", ObjectType.NamedTemplate);

            // Having 'match' attribute without 'mode' attribute means the default mode
            if (mode == null && !HasAttribute(nav, "mode") && HasAttribute(nav, "match"))
                mode = DefaultModeName;

            ProtoTemplateInfo facet1 = (mode != null) ? AddProtoTemplate(mode, /*replace:*/false) : null;
            ProtoTemplateInfo facet2 = (name != null) ? AddProtoTemplate(name, /*replace:*/true ) : null;
            FindCallees(nav, facet1, facet2);
        }

        private void AnalyzeAttributeSet(XPathNavigator nav) {
            ObjectName name = ResolveQNameFromAttribute(nav, "name", ObjectType.AttributeSet);
            if (name != null) {
                ProtoTemplateInfo attSet = AddProtoTemplate(name, /*replace:*/false);
                foreach (ObjectName useAttSet in ResolveQNamesFromAttribute(nav, "use-attribute-sets", ObjectType.AttributeSet)) {
                    attSet.AddCallee(useAttSet);
                }

                XPathNavigator child = nav.Clone();
                for (bool result = child.MoveToChild(XPathNodeType.Element); result; result = child.MoveToNext(XPathNodeType.Element)) {
                    if (child.NamespaceURI == XsltNamespace && child.LocalName == "attribute") {
                        // Limitation: If the 'name' attribute contains an attribute value template, ignore it.
                        // Also ignore the 'namespace' attribute for now.
                        ObjectName attName = ResolveQNameFromAttribute(child, "name", ObjectType.Attribute);
                        if (attName != null && !HasAttribute(child, "namespace")) {
                            AddProtoTemplate(attName, /*replace:*/false);
                            attSet.AddCallee(attName);
                        }
                    }
                }
            }
        }

        private ObjectName ResolveQNameFromAttribute(XPathNavigator nav, string attrName, ObjectType type) {
            // GetAttribute returns an empty string if an attribute is not found
            string qname = nav.GetAttribute(attrName, string.Empty);
            if (qname.Length == 0)
                return null;

            return ResolveQName(nav, qname, type);
        }

        private IList<ObjectName> ResolveQNamesFromAttribute(XPathNavigator nav, string attrName, ObjectType type) {
            // GetAttribute returns an empty string if an attribute is not found
            string attrValue = nav.GetAttribute(attrName, string.Empty);
            if (attrValue.Length == 0)
                return ObjectName.EmptyList;

            string[] qnames = attrValue.Split(WhitespaceChars, StringSplitOptions.RemoveEmptyEntries);
            List<ObjectName> objectNames = new List<ObjectName>(qnames.Length);

            foreach (string qname in qnames) {
                ObjectName name = ResolveQName(nav, qname, type);
                if (name != null)
                    objectNames.Add(name);
            }
            return objectNames;
        }

        private ObjectName ResolveQName(XPathNavigator nav, string qname, ObjectType type) {
            // Validate QName
            try {
                this.helperWriter.WriteName(qname);
            }
            catch {
                return null;
            }

            int colonPos = qname.IndexOf(':');
            if (colonPos <= 0) {
                // Unprefixed QName
                return new ObjectName(string.Empty, qname, string.Empty, type);
            }
            else {
                // QName has a prefix, resolve it
                string prefix = qname.Substring(0, colonPos);
                string namespaceUri = nav.LookupNamespace(prefix);
                if (namespaceUri != null) {
                    return new ObjectName(prefix, qname.Substring(colonPos + 1), namespaceUri, type);
                }
                return null;
            }
        }

        private static bool HasAttribute(XPathNavigator nav, string attrName) {
            if (!nav.MoveToAttribute(attrName, string.Empty))
                return false;

            nav.MoveToParent();
            return true;
        }

        private ProtoTemplateInfo AddProtoTemplate(ObjectName name, bool replace) {
            ProtoTemplateInfo result;

            if (replace || !this.protoTemplates.TryGetValue(name, out result)) {
                result = this.protoTemplates[name] = new ProtoTemplateInfo();
            }
            return result;
        }

        private void FindCallees(XPathNavigator nav, ProtoTemplateInfo facet1, ProtoTemplateInfo facet2) {
            if (facet1 == null && facet2 == null)
                return;

            foreach (XPathNavigator callTemplate in nav.SelectDescendants("call-template", XsltNamespace, /*matchSelf:*/false)) {
                ObjectName callee = ResolveQNameFromAttribute(callTemplate, "name", ObjectType.NamedTemplate);
                if (callee == null)
                    continue;

                if (facet1 != null)
                    facet1.AddCallee(callee);

                if (facet2 != null)
                    facet2.AddCallee(callee);
            }

            foreach (XPathNavigator callTemplate in nav.SelectDescendants("apply-templates", XsltNamespace, /*matchSelf:*/false)) {
                ObjectName callee = ResolveQNameFromAttribute(callTemplate, "mode", ObjectType.Mode) ?? DefaultModeName;
                if (callee == null)
                    continue;

                if (facet1 != null)
                    facet1.AddCallee(callee);

                if (facet2 != null)
                    facet2.AddCallee(callee);
            }
        }
        #endregion

        #region Create - Create Graph Using Gathered Information
        public override void Create(Panel container) {
            if (this.modules == null)
                throw new InvalidOperationException("Must call Prepare first");

            base.Create(container);
            this.moduleNodes = new Dictionary<Uri, GraphNode>();
            this.protoTemplateNodes = new Dictionary<ObjectName, GraphNode>();

            if (this.graphType == GraphType.Auto) {
                // A simple heuristic
                this.graphType = this.modules.Count > 2 ? GraphType.Modules : GraphType.Templates;
                UpdateMenu();
            }

            switch (this.graphType) {
                case GraphType.Modules:
                    CreateModuleGraph();
                    break;
                case GraphType.Templates:
                case GraphType.AttributeSets:
                    CreateProtoTemplateGraph();
                    break;
            }
        }

        private void CreateModuleGraph() {
            foreach (Uri uri in this.modules.Keys) {
                GraphNode module = GetModuleNode(uri);
                foreach (IncludeInfo include in this.modules[uri].Includes)
                    AddEdge(module, GetModuleNode(include.Uri), "Includes");
            }
        }

        private GraphNode GetModuleNode(Uri uri) {
            GraphNode node;

            if (this.moduleNodes.TryGetValue(uri, out node))
                return node;

            node = new GraphNode();
            node.Id = this.modules[uri].Id;
            node.Label = GetFileName(uri);
            node.NodeType = "Module";

            this.moduleNodes[uri] = node;
            AddNode(node);
            return node;
        }

        private static string GetFileName(Uri uri) {
            return uri.Segments[uri.Segments.Length - 1];
        }

        private void CreateProtoTemplateGraph() {
            ObjectType combinedFilter = this.objectFilter & GraphTypeFilter[(int)this.graphType];

            // Copy names to a temporary array to allow modifying the dictionary within the foreach statement
            ObjectName[] protoTemplateNames = new ObjectName[this.protoTemplates.Keys.Count];
            this.protoTemplates.Keys.CopyTo(protoTemplateNames, 0);

            foreach (ObjectName caller in protoTemplateNames) {
                if (ShowProtoTemplate(caller, combinedFilter)) {
                    GraphNode callerNode = GetProtoTemplateNode(caller);
                    foreach (ObjectName callee in this.protoTemplates[caller].Callees) {
                        if (ShowProtoTemplate(callee, combinedFilter)) {
                            // Ensure that a node is created for the callee
                            AddProtoTemplate(callee, /*replace:*/false);
                            GraphNode calleeNode = GetProtoTemplateNode(callee);
                            AddEdge(callerNode, calleeNode, "Calls");
                        }
                    }
                }
            }
        }

        private bool ShowProtoTemplate(ObjectName name, ObjectType filter) {
            return (name.Type & filter) != 0;
        }

        private GraphNode GetProtoTemplateNode(ObjectName name) {
            GraphNode node;

            if (this.protoTemplateNodes.TryGetValue(name, out node))
                return node;

            node = new GraphNode();
            node.Id = this.protoTemplates[name].Id;
            string label = name.ToString();

            switch (name.Type) {
                case ObjectType.Mode:
                    node.NodeType = "Element";
                    node.Label = "Mode: " + label;
                    break;
                case ObjectType.NamedTemplate:
                    node.NodeType = "Template";
                    node.Label = label;
                    break;
                case ObjectType.GlobalVariable:
                    node.NodeType = "AttributeGroup";
                    node.Label = "$" + label;
                    break;
                case ObjectType.AttributeSet:
                    node.NodeType = "AttributeGroup";
                    node.Label = label;
                    break;
                case ObjectType.Attribute:
                    node.NodeType = "Attribute";
                    node.Label = label;
                    break;
            }

            this.protoTemplateNodes[name] = node;
            AddNode(node);
            return node;
        }
        #endregion Create

        #region Informational Classes
        private class IncludeInfo {
            private Uri uri;
            private bool isImport;

            public Uri Uri {
                get { return this.uri; }
            }

            public bool IsImport {
                get { return this.isImport; }
            }

            public IncludeInfo(Uri uri, bool isImport) {
                this.uri = uri;
                this.isImport = isImport;
            }
        }

        private class ModuleInfo {
            private static int idCounter = 0;
            private static readonly IList<IncludeInfo> EmptyList = new List<IncludeInfo>(0).AsReadOnly();

            private readonly string id;
            private List<IncludeInfo> includes;

            public string Id {
                get { return this.id; }
            }

            public IList<IncludeInfo> Includes {
                get { return this.includes ?? EmptyList; }
            }

            public ModuleInfo() {
                this.id = (ModuleInfo.idCounter++).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            public void AddInclude(IncludeInfo include) {
                if (this.includes == null)
                    this.includes = new List<IncludeInfo>();

                // Duplicates are ok
                this.includes.Add(include);
            }
        }

        private class ProtoTemplateInfo {
            private static int idCounter = 0;
            private readonly string id;
            private List<ObjectName> callees;

            public string Id {
                get { return this.id; }
            }

            public IList<ObjectName> Callees {
                get { return this.callees ?? ObjectName.EmptyList; }
            }

            public ProtoTemplateInfo() {
                this.id = (ProtoTemplateInfo.idCounter++).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            public void AddCallee(ObjectName callee) {
                if (this.callees == null)
                    this.callees = new List<ObjectName>();

                // Duplicates are ok
                this.callees.Add(callee);
            }
        }

        /// <summary>
        /// Tuple (prefix, localName, namespaceUri, type).
        /// </summary>
        private class ObjectName {
            public static readonly IList<ObjectName> EmptyList = new List<ObjectName>(0).AsReadOnly();

            private readonly string prefix;
            private readonly string localName;
            private readonly string namespaceUri;
            private readonly ObjectType type;

            public ObjectName(string prefix, string localName, string namespaceUri, ObjectType type) {
                Debug.Assert(prefix != null && localName != null && namespaceUri != null);
                this.prefix = prefix;
                this.localName = localName;
                this.namespaceUri = namespaceUri;
                this.type = type;
            }

            public string Prefix {
                get { return this.prefix; }
            }

            public string LocalName {
                get { return this.localName; }
            }

            public string NamespaceUri {
                get { return this.namespaceUri; }
            }

            public ObjectType Type {
                get { return this.type; }
            }

            /// <summary>
            /// Override GetHashCode() so that the QualifiedName can be used as a key in the hashtable.
            /// </summary>
            public override int GetHashCode() {
                return this.localName.GetHashCode();
            }

            /// <summary>
            /// Override Equals() so that the QualifiedName can be used as a key in the hashtable.
            /// </summary>
            /// <remarks>Does not compare their prefixes (if any).</remarks>
            public override bool Equals(object obj) {
                if (this == null)
                    throw new NullReferenceException();

                return this == (obj as ObjectName);
            }

            /// <summary>
            /// Implement operator == to prevent accidental reference comparisons.
            /// </summary>
            /// <remarks>Does not compare their prefixes (if any).</remarks>
            public static bool operator ==(ObjectName a, ObjectName b) {
                if ((object)a == (object)b) {
                    return true;
                }
                if ((object)a == null || (object)b == null) {
                    return false;
                }
                return a.localName == b.localName && a.namespaceUri == b.namespaceUri && a.type == b.type;
            }

            /// <summary>
            /// Implement operator != to prevent accidental referentce comparisons.
            /// </summary>
            /// <remarks>Does not compare their prefixes (if any).</remarks>
            public static bool operator !=(ObjectName a, ObjectName b) {
                return !(a == b);
            }

            /// <summary>
            /// Return the qualified name in the form prefix:localName.
            /// </summary>
            public override string ToString() {
                if (this.prefix.Length == 0)
                    return this.localName;
                else
                    return this.prefix + ':' + this.localName;
            }
        }
        #endregion
    }
}
