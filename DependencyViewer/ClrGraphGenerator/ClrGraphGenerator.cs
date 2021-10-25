using System;
using System.Collections.Generic;
using System.Text;
using System.Compiler;
using System.Windows;
using System.Windows.Controls;

namespace DependencyViewer {

    public class ClrGraphGenerator : GraphGenerator {
        IDictionary<TypeNode, GraphNode> typemap;
        AssemblyNode[] assemblies;
        GraphType type = GraphType.Type;
        GraphFlags flags = GraphFlags.Publics | GraphFlags.Internals | GraphFlags.Externals;
        MenuItem menu;
        IDictionary<string, bool> namespaces = new Dictionary<string, bool>();        
        Panel container;
        IList<GraphNode> newnodes = new List<GraphNode>();
        List<TypeNode> roots = new List<TypeNode>();
        List<TypeNode> context = new List<TypeNode>();
        IDictionary<GraphEdge, EdgeTip> tips;
        Filter filter = Filter.All;
        GraphGroup groupFlags;
        int callChainDepth = 3;
        bool noLimit = false;
        bool limitExternals = true;
        bool isMoreDetail = false;
        bool partial = false;

        public override void SaveState(ViewState state) {
            base.SaveState(state);
            if (namespaces != null) state["namespaces"] = new Dictionary<string, bool>(namespaces);
            state["GraphType"] = type;
            state["GraphFlags"] = flags;
            state["Filter"] = filter;
            if (roots != null) state["roots"] = new List<TypeNode>(roots);
            state["callChainDepth"] = callChainDepth;
            state["noLimit"] = noLimit;
            state["limitExternals"] = limitExternals;
            state["isMoreDetail"] = isMoreDetail;
            state["partial"] = partial;
        }

        public override void LoadState(ViewState state) {
            base.LoadState(state);
            namespaces = state["namespaces"] as Dictionary<string, bool>;
            type = (GraphType)state["GraphType"];
            flags = (GraphFlags)state["GraphFlags"];
            filter = (Filter)state["Filter"];
            roots = (List<TypeNode>)state["roots"];
            callChainDepth = (int)state["callChainDepth"];
            noLimit = (bool)state["noLimit"];
            limitExternals = (bool)state["limitExternals"];
            isMoreDetail = (bool)state["isMoreDetail"];
            partial = (bool)state["partial"];
            context.Clear();
            if (partial) {
                context.AddRange(roots);
            }
            UpdateMenu();
        }

        class EdgeTip {
            Dictionary<string, string> tips = new Dictionary<string, string>();
            StringBuilder sb = new StringBuilder();
            
            public EdgeTip(){
            }
            public void Add(string tip) {
                if (!tips.ContainsKey(tip)) {
                    tips[tip] = tip;
                    if (sb.Length > 0) sb.Append("\r\n");
                    sb.Append(tip);
                }
            }
            public override string ToString() {
                return sb.ToString();
            }        
        }

        enum GraphGroup
        {
            None, Namespace, Assembly
        }
    
        enum GraphType {
            Type, Dependencies, Containment, CallGraph
        }

        enum GraphFlags {
            Publics = 1,
            Internals = 2,
            Privates = 4,
            MethodBodies = 8,
            Externals = 16,
            Enums = 32,
        }

        public ClrGraphGenerator(){
        }

        public override string FileFilter {
            get {
                return "Assemblies (*.exe;*.dll)|*.exe;*.dll";
            }
        }

        public override string Label {
            get {
                return this.type == GraphType.Dependencies ? "Dependency View" : "Type View";
            }
        }

        public override void CreateViewMenu(MenuItem menu) {
            this.menu = menu;
            CreateMenuItem(menu, "_Types", type == GraphType.Type, GraphType.Type);
            CreateMenuItem(menu, "_Containment", type == GraphType.Containment, GraphType.Containment);
            CreateMenuItem(menu, "_Calls", type == GraphType.CallGraph, GraphType.CallGraph);
            CreateMenuItem(menu, "_Dependencies", type == GraphType.Dependencies, GraphType.Dependencies);
            menu.Items.Add(new Separator());
            CreateMenuItem(menu, "Group by _Namespace", groupFlags == GraphGroup.Namespace, GraphGroup.Namespace);
            CreateMenuItem(menu, "Group by _Assembly", groupFlags == GraphGroup.Assembly, GraphGroup.Assembly);
            menu.Items.Add(new Separator());
            CreateMenuItem(menu, "_Public", ShowPublics, GraphFlags.Publics);
            CreateMenuItem(menu, "_Internals", ShowInternals, GraphFlags.Internals);
            CreateMenuItem(menu, "_Privates", ShowPrivates, GraphFlags.Privates);
            CreateMenuItem(menu, "_Method Bodies", ShowMethodBodies, GraphFlags.MethodBodies);
            CreateMenuItem(menu, "_Externals", ShowExternals, GraphFlags.Externals);
            CreateMenuItem(menu, "_Enums", ShowEnums, GraphFlags.Enums);
            menu.Items.Add(new Separator());
            CreateMenuItem(menu, "N_amespaces...", false, "Namespaces");            
        }

        MenuItem CreateMenuItem(ItemsControl menu, string label, bool check, object flag) {
            MenuItem item = new MenuItem();
            item.Header = label;
            item.IsChecked = check;
            item.Tag = flag;
            item.Click += new RoutedEventHandler(OnClick);
            menu.Items.Add(item);
            return item;
        }

        MenuItem ctxCallGraph;
        MenuItem ctxTypeGraph;
        MenuItem ctxDependancyGraph;
        MenuItem ctxContainmentGraph;
        MenuItem ctxMoreDetail;
        MenuItem ctxLessDetail;
        MenuItem ctxNoLimit;
        MenuItem ctxLimitExternals;

        public override void CreateContextMenu(ContextMenu menu) {
            base.CreateContextMenu(menu);
            menu.Items.Insert(0, new Separator());

            ctxLimitExternals = InsertMenuItem(menu, "Limit _externals", this.limitExternals, "LimitExternals");
            ctxNoLimit = InsertMenuItem(menu, "_No depth limit", this.noLimit, "NoLimit");
            ctxMoreDetail = InsertMenuItem(menu, "_More detail", false, "More");
            ctxLessDetail = InsertMenuItem(menu, "_Less detail", false, "Less");            
            menu.Items.Insert(0, new Separator());

            ctxDependancyGraph = InsertMenuItem(menu, "Show _dependencies", false, "DependancyGraph");
            ctxCallGraph = InsertMenuItem(menu, "Show _calls", false, "CallGraph");
            ctxContainmentGraph = InsertMenuItem(menu, "Show c_ontainment", false, "ContainmentGraph");
            ctxTypeGraph = InsertMenuItem(menu, "Show _types", false, "TypeGraph");            
        }

        MenuItem InsertMenuItem(ItemsControl menu, string label, bool check, object flag) {
            MenuItem item = CreateMenuItem(menu, label, check, flag);
            menu.Items.Remove(item);
            menu.Items.Insert(0, item);
            return item;
        }

        public override void SetContextMenuState(GraphNode ns) {
            base.SetContextMenuState(ns);
            context.Clear();
            foreach (GraphNode n in this.SelectedNodes) {
                TypeNode t = n.UserData as TypeNode;
                if (t != null) context.Add(t);
            }
            if (ns != null && context.Count == 0) {
                TypeNode t = ns.UserData as TypeNode;
                if (t != null) context.Add(t);
            }

            UpdateMenu();
        }

        IList<NamespaceInfo> GetNamespaceList() {
            IList<NamespaceInfo> nslist = new List<NamespaceInfo>();            
            SortedList<string, string> sorted = new SortedList<string, string>();
            foreach (string key in namespaces.Keys) {
                sorted.Add(key, key);
            }
            foreach (string ns in sorted.Values) {
                nslist.Add(new NamespaceInfo(namespaces[ns], ns));
            }
            return nslist;
        }

        string GetNamespace(TypeNode t) {
            string name = t.Namespace != null ? t.Namespace.Name : "";
            return string.IsNullOrEmpty(name) ? "<no namespace>" : name;
        }

        void AddNamespace(TypeNode t) {
            if (t != null) {
                string nsname = GetNamespace(t);
                if (!namespaces.ContainsKey(nsname)) {
                    namespaces[nsname] = true;
                }
            }
        }

        bool ShowNamespace(string name) {
            if (!namespaces.ContainsKey(name)) return true; // then there's no filter for it.
            return namespaces[name];
        }

        bool ShowNamespace(TypeNode t) {
            string nsname = GetNamespace(t);
            return ShowNamespace(nsname);
        }

        bool SelectNamespaces() {
            NamespaceDialog dlg = new NamespaceDialog();
            dlg.Namespaces = GetNamespaceList();
            bool rc = dlg.ShowDialog() == true;
            if (rc) {
                // save new settings.
                namespaces.Clear();
                foreach (NamespaceInfo info in dlg.Namespaces) {
                    namespaces[info.Namespace] = info.Checked;
                }
            }
            return rc;
        }

        void OnClick(object sender, RoutedEventArgs e) {
            
            MenuItem item = (MenuItem)sender;
            OnBeforeChange();
            if (item.Tag is GraphType) {
                type = (GraphType)item.Tag;
            } else if (item.Tag is GraphFlags) {
                ToggleFlag((GraphFlags)item.Tag);
            } else if (item.Tag is GraphGroup) {
                ToggleGroup((GraphGroup)item.Tag);
            } else {
                switch ((item.Tag as string)) {
                    case "CallGraph":
                        CopyContext();
                        type = GraphType.CallGraph;
                        break;
                    case "DependancyGraph":
                        CopyContext();
                        type = GraphType.Dependencies;
                        break;
                    case "ContainmentGraph":
                        CopyContext();
                        type = GraphType.Containment;
                        break;
                    case "TypeGraph":
                        CopyContext();
                        type = GraphType.Type;
                        break;
                    case "More":
                        this.callChainDepth++;
                        break;
                    case "Less":
                        this.callChainDepth--;
                        break;
                    case "NoLimit":
                        this.noLimit = !this.noLimit;
                        break;
                    case "LimitExternals":
                        this.limitExternals = !this.limitExternals;
                        break;
                    case "Namespaces":
                        if (!SelectNamespaces())
                            return;
                        break;
                }
            }
            UpdateMenu();
            OnAfterChange();
        }

        void CopyContext() {
            roots.Clear();
            roots.AddRange(context);
            partial = true;
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
                    } else if (mi.Tag is GraphGroup) {
                        GraphGroup f = (GraphGroup)mi.Tag;
                        mi.IsChecked = (this.groupFlags & f) != 0;
                    }
                }
            }

            ctxTypeGraph.IsEnabled = ctxContainmentGraph.IsEnabled = ctxDependancyGraph.IsEnabled = ctxCallGraph.IsEnabled = context.Count > 0;
            ctxMoreDetail.IsEnabled = !noLimit && isMoreDetail;
            ctxLessDetail.IsEnabled = !noLimit && this.callChainDepth > 0;
            ctxNoLimit.IsChecked = this.noLimit;
            ctxLimitExternals.IsChecked = this.limitExternals;
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

        bool ToggleGroup(GraphGroup flag)
        {
            if ((groupFlags & flag) != 0)
            {
                groupFlags &= ~flag;
                return false;
            }
            else
            {
                // no 'or' because the settings are mutually exclusive.
                groupFlags = flag;
                return true;
            }
        }

        public bool ShowPublics {
            get { return (flags & GraphFlags.Publics) != 0; }
        }

        public bool ShowInternals {
            get { return (flags & GraphFlags.Internals) != 0; }
        }

        public bool ShowPrivates {
            get { return (flags & GraphFlags.Privates) != 0; }
        }

        public bool ShowMethodBodies {
            get { return (flags & GraphFlags.MethodBodies) != 0; }
        }

        public bool ShowExternals {
            get { return (flags & GraphFlags.Externals) != 0; }
        }
        public bool ShowEnums {
            get { return (flags & GraphFlags.Enums) != 0; }
        }
        
        public override void Prepare() {
            base.Prepare();
            assemblies = new AssemblyNode[FileNames.Length];
            int i = 0;
            foreach (string file in FileNames) {
                AssemblyNode a = AssemblyNode.GetAssembly(file);
                if (a == null) {
                    throw new Exception("File is not a managed assembly: " + file);
                }
                assemblies[i++] = a;
            }
        }

        public override void Reset() {
            base.Reset();
            partial = false;
            roots.Clear();
            callChainDepth = 3;
        }

        // Statically load the specified assemblies and return the type graphs or dependency graph.
        public override void Create(Panel container) {
            this.container = container;

            if (assemblies == null)
                throw new InvalidOperationException("Must call Prepare first");

            base.Create(container);
            typemap = new Dictionary<TypeNode, GraphNode>();
            newnodes = new List<GraphNode>();
            tips = new Dictionary<GraphEdge, EdgeTip>();
            namespaceGroups = new Dictionary<string, GraphNode>();
            assemblyGroups = new Dictionary<AssemblyNode, GraphNode>();

            // If context menu was used, then roots contains the focus of the diagram,
            // otherwise we want to illustrate everything, so here we populate the roots.
            if (!partial || this.roots.Count == 0) {
                partial = false;
                roots.Clear();
                foreach (AssemblyNode a in assemblies) {
                    TypeNodeList types = a.Types;
                    for (int j = 0, n = types.Count; j < n; j++) {
                        TypeNode t = types[j];
                        if (IncludeType(t)) {
                            this.roots.Add(t);
                        }
                    }
                }
            }
            
            // Load the root type hierarchies.
            foreach (TypeNode t in this.roots) {
                AddGraphType(t);
            }

            isMoreDetail = false;
            // Now if this is a partial graph then we need to visit the new child nodes to get a complete picture
            // but only up to a certain callChainDepth.
            if (partial) {
                int i = 1;
                int depth = callChainDepth;
                while (i < newnodes.Count && (noLimit || depth-- > 0)) {
                    for (int n = newnodes.Count; i < n; i++) {
                        GraphNode node = newnodes[i];
                        TypeNode t = node.UserData as TypeNode;
                        if (t != null && (!this.limitExternals || !IsExternal(t))) {
                            // do not drill into external implementation details..
                            AddGraphType(t);
                        }
                    }
                }
                isMoreDetail = (i < newnodes.Count);
            }            
        }

        private void AddGraphType(TypeNode t) {
            AddNamespace(t);
            if (FlagsMatch(t)) {
                GraphNode node = AddType(t);
                if (node != null) {
                    switch (type) {
                        case GraphType.Type:
                            AddBaseTypes(node, t);
                            break;
                        case GraphType.Containment:
                            filter = Filter.Field;
                            AddDependencies(node, t);
                            break;
                        case GraphType.Dependencies:
                            filter = Filter.All;
                            AddDependencies(node, t);
                            break;
                        case GraphType.CallGraph:
                            AddCallGraph(node, t);
                            break;
                    }
                }
            }
        }

        bool CanIncludeExternal(TypeNode t, TypeNode bt) {
            return !IsExternal(t) || !this.limitExternals || !IsExternal(bt);
        }


        private GraphNode AddBaseTypes(GraphNode node, TypeNode t) {
            AddInterfaces(t, node);
            TypeNode previous = t;
            for (TypeNode bt = t.BaseType; bt != null; bt = bt.BaseType) {
                AddNamespace(bt);
                if (IncludeType(bt) && FlagsMatch(bt) && CanIncludeExternal(previous, bt)) {
                    GraphNode basenode = AddNode(bt, bt.GetUnmangledNameWithTypeParameters());
                    GraphEdge edge = AddEdge(node, basenode, "Inherits");
                    if (edge != null) {
                        edge.Tip = "Inherits";
                        edge.EdgeType = "Inherits";
                    }
                    AddInterfaces(bt, basenode);
                    node = basenode;
                }
                previous = bt;
            }
            return node;
        }

        GraphNode AddType(TypeNode t) {
            if (IncludeType(t) && FlagsMatch(t)) {
                return AddNode(t, t.GetUnmangledNameWithTypeParameters());
            }
            return null;
        }

        void AddInterfaces(TypeNode t, GraphNode node) {
            InterfaceList list = t.Interfaces;
            if (list != null) {
                for (int i = 0, n = list.Count; i < n; i++) {
                    Interface iface = list[i];
                    AddNamespace(iface);
                    if (FlagsMatch(iface)) {
                        GraphNode basenode = AddNode(iface, iface.GetUnmangledNameWithTypeParameters());
                        GraphEdge edge = AddEdge(node, basenode, "Implements");
                        if (edge != null) {
                            edge.Tip = "Inherits";
                            edge.EdgeType = "Inherits";
                        }
                    }
                }
            }
        }

        Dictionary<Block, Block> guard = new Dictionary<Block, Block>();

        void AddDependencies(GraphNode node, TypeNode t) {
            guard.Clear();
            if (filter == Filter.All) {
                AddBaseTypes(node, t);
            }
            AddMembers(t, node);
        }

        private void AddDependency(GraphNode from, TypeNode to, string edgeType, string tip) {
            if (to == null) return;

            if (to.IsGeneric) {
                if (FlagsMatch(to)) {
                    AddDependencyNode(from, to, edgeType, tip);
                }
                // Template args might not be external, while the generic collection is, so needs to have separate
                // FlagsMatch test.
                TypeNodeList list = to.TemplateArguments;
                if (list != null) {
                    for (int i = 0, n = list.Count; i < n; i++) {
                        TypeNode targ = list[i];
                        AddDependency(from, targ, edgeType, "TemplateArgument");
                    }
                }
            } else {
                ArrayType at = to as ArrayType;
                if (at != null) {
                    // then look at the element type of the array.
                    AddDependency(from, at.ElementType, edgeType, "Array");
                } else {
                    Reference r = to as Reference;
                    if (r != null) {
                        to = r.ElementType; // dereference it!
                    }
                    if (FlagsMatch(to)) {
                        AddDependencyNode(from, to, edgeType, tip);
                    }
                }
            }
        }

        private void AddDependencyNode(GraphNode from, TypeNode to, string edgeType, string tip) {
            if (IncludeType(to) && CanIncludeExternal((TypeNode)from.UserData, to)) {
                AddNamespace(to);
                GraphNode refnode = AddNode(to, to.GetUnmangledNameWithoutTypeParameters());
                GraphEdge edge = AddEdge(from, refnode, edgeType);
                if (edge != null && !string.IsNullOrEmpty(tip)) {
                    EdgeTip et = null;
                    if (!tips.ContainsKey(edge)) {
                        et = new EdgeTip();
                        tips[edge] = et;
                    } else {
                        et = tips[edge];
                    }
                    et.Add(tip);
                    edge.Tip = et.ToString();
                }
            }
        }

        enum Filter { Field = 1, Property = 2, Method = 4, All=7 }

        bool ShowFields {
            get { return (filter & Filter.Field) != 0; }
        }
        bool ShowProperties {
            get { return (filter & Filter.Property) != 0; }
        }
        bool ShowMethods {
            get { return (filter & Filter.Method) != 0; }
        }

        private void AddMembers(TypeNode t, GraphNode node) {
            MemberList members = t.Members;
            for (int i = 0, n = members.Count; i < n; i++) {
                Member m = members[i] as Member;

                if (ShowFields) {
                    Field f = m as Field;
                    if (f != null && FlagsMatch(f) && f.Type != t) {
                        AddDependency(node, f.Type, "Field", f.Name.Name);
                        continue;
                    }
                }
                if (ShowProperties) {
                    Property p = m as Property;
                    if (p != null && FlagsMatch(p) && p.Type != t) {
                        AddDependency(node, p.Type, "Property", p.Name.Name);
                        continue;
                    }
                }
                if (ShowMethods) {
                    Method me = m as Method;
                    if (me != null && FlagsMatch(m)) {
                        AddMethod(me, t, node);
                        continue;
                    }
                }

            }
        }

        void AddMethod(Method m, TypeNode t, GraphNode node) {
            if (m.ReturnType != t) {
                AddDependency(node, m.ReturnType, "Method", m.Name.Name);
            }
            ParameterList list = m.Parameters;
            if (list != null) {
                for (int i = 0, n = list.Count; i < n; i++) {
                    Parameter p = list[i];
                    if (p != null && p.Name != null && p.Type != t) {
                        AddDependency(node, p.Type, "MethodParameter",  m.Name.Name + "(" + p.Name.Name + ")");
                    }
                }
            }
            if (ShowMethodBodies) {
                AddBlock(node, m.Body);
            }
        }

        void AddCallGraph(GraphNode node, TypeNode t) {
            filter = Filter.Method;
            
            // show only the stuff introduced through direct method calls from
            // selected nodes...
            AddMembers(t, node);
        }

        void AddBlock(GraphNode node, Block block) {
            if (block == null) return;
            if (guard.ContainsKey(block))
                return;
            guard[block] = block;
            if (block != null && block.Statements != null) {
                AddStatements(node, block.Statements);
            }
            guard.Remove(block);
        }

        void AddStatements(GraphNode node, StatementList list) {
            if (list == null) return;
            for (int i = 0, n = list.Count; i < n; i++) {
                Statement s = list[i];
                AssignmentStatement ass = s as AssignmentStatement;
                if (ass != null) {
                    AddExpression(node, ass.Source);
                    AddExpression(node, ass.Target);
                    continue;
                }                 
                ExpressionStatement es = s as ExpressionStatement;
                if (es != null) {
                    AddExpression(node, es.Expression);
                    continue;
                }
                Block b = s as Block;
                if (b != null && b.Statements != null) {
                    AddStatements(node, b.Statements);
                    continue;
                }

                Branch br = s as Branch;
                if (br != null && br.Target != null) {
                    AddExpression(node, br.Condition);
                    AddBlock(node, br.Target);
                    continue;
                }
                if (s.NodeType == NodeType.Nop || s.NodeType == NodeType.EndFinally)
                    continue;
                
                Throw th = s as Throw;
                if (th != null && th.Expression != null) {
                    AddExpression(node, th.Expression);
                    continue;
                }

                SwitchInstruction sw = s as SwitchInstruction;
                if (sw != null && sw.Expression != null) {
                    AddExpression(node, sw.Expression);
                    for (int j = 0, ic = sw.Targets.Count; j < ic; j++) {
                        AddBlock(node, sw.Targets[j]);
                    }
                    continue;
                }
            }
        }

        void AddExpression(GraphNode node, System.Compiler.Expression e) {
            if (e == null) return;
            if (e.NodeType == NodeType.Pop || e.NodeType == NodeType.Dup)
                return;

            Literal lit = e as Literal;
            if (lit != null) return;

            MethodCall c = e as MethodCall;
            if (c != null) {
                AddDependency(node, c.Type, "ReturnType","");                
                if (c.Operands != null) {
                    for (int i = 0, n = c.Operands.Count; i < n; i++) {
                        AddExpression(node, c.Operands[i]);
                    }
                }
                MemberBinding b = c.Callee as MemberBinding;
                if (b != null) { 
                    if (b.TargetObject != null) {
                        AddDependency(node, b.TargetObject.Type, "MethodCall", b.BoundMember.Name.Name);
                    }
                }
                AddDependency(node, c.Callee.Type, "MethodCallee","");                
                return;
            }
            Construct constr = e as Construct;
            if (constr != null) {
                AddDependency(node, constr.Type, "Construct","");
                return;
            }
            BlockExpression be = e as BlockExpression;
            if (be != null) {
                AddDependency(node, be.Type, "Block","");
                AddBlock(node, be.Block);
                return;
            }
            Parameter p = e as Parameter;
            if (p != null) {
                AddDependency(node, p.Type, "Parameter", p.Name.Name);
                return;
            }
            MemberBinding m = e as MemberBinding;
            if (m != null) {
                AddDependency(node, m.Type, "MemberBinding", m.BoundMember.Name.Name);
                return;
            }
            Local l = e as Local;
            if (l != null) {
                if (ShowFields) AddDependency(node, l.Type, "Local:", l.Name.Name);
                return;
            }
            UnaryExpression ue = e as UnaryExpression;
            if (ue != null) {
                AddExpression(node, ue.Operand);
                return;
            }
            Indexer indexer = e as Indexer;
            if (indexer != null) {
                AddDependency(node, indexer.Type, "Indexer","");
                AddExpression(node, indexer.Object);
                AddExpressionList(node, indexer.Operands);
                return;
            }
            BinaryExpression bin = e as BinaryExpression;
            if (bin != null) {
                AddDependency(node, bin.Type, "Expression", bin.NodeType.ToString());
                AddExpression(node, bin.Operand1);
                AddExpression(node, bin.Operand2);                
                return;
            }

            ConstructArray ca = e as ConstructArray;
            if (ca != null) {
                AddDependency(node, ca.ElementType, "ConstructArray","");
                AddExpressionList(node, ca.Operands);
                return;
            }
            AddressDereference ad = e as AddressDereference;
            if (ad != null) {
                AddDependency(node, ad.Type, "References", "Address dereference");
                return;
            }
            if (e.NodeType == NodeType.Arglist) {
                AddDependency(node, e.Type, "ArgList","");                
                return;
            }
            return;
        }

        void AddExpressionList(GraphNode node, ExpressionList list) {
            if (list != null) {
                for (int j = 0, n = list.Count; j < n; j++) {
                    AddExpression(node, list[j]);
                }
            }
        }

        bool FlagsMatch(Member m) {
            TypeNode t = m as TypeNode;
            bool external = IsExternal(t);
            if (!ShowEnums && (m is EnumNode))
                return false;
            if (!ShowExternals && external)
                return false;

            if (t != null && !ShowNamespace(t)) 
                return false;

            if (external) return ShowPublics; // must be public by definition.
          
            if (m.IsFamily && ShowInternals) return true;
            if (m.IsPublic && ShowPublics) return true;
            if (ShowPrivates) return true;

            return false;
        }

        private bool IncludeType(TypeNode t) {
            if (t != null && !IsPrimitive(t) && FlagsMatch(t)) {

                ArrayType at = t as ArrayType;
                if (at != null) {
                    // then look at the element type of the array.
                    return IncludeType(at.ElementType);
                }

                string unmangled = t.GetUnmangledNameWithTypeParameters();
                string name = t.FullName;
                // Exclude <PrivateImplementationDetails>, <Module> and the yield return
                // closures which all contain "<" and ">" in their class names.
                if (name.Contains("<") || name.Contains(">") || 
                    name.Contains("GeneratedInternalTypeHelper")) {
                    return false;
                }
                return true;
            }
            return false;
        }

        private bool IsPrimitive(TypeNode t) {
            if (t == SystemTypes.Object || t == SystemTypes.ValueType || t == SystemTypes.Enum ||
                t == SystemTypes.MulticastDelegate || t == SystemTypes.Delegate || t == SystemTypes.Void ||
                t == SystemTypes.Array || t.IsPrimitive ) {
                return true;
            }

            if (t.DeclaringModule.Location != null &&
                t.DeclaringModule.Location.EndsWith("mscorlib.dll", StringComparison.InvariantCultureIgnoreCase)) {
                return true;
            }
            return false;
        }

        internal string GetNodeType(TypeNode t) {
            if (t.DeclaringModule == null) return "External";
            if (IsExternal(t)) return "External";            
            if (t is EnumNode) return "Enum";
            return "Normal";
        }

        internal string GetAccessType(TypeNode t) {
            if (t.IsPublic) return "Public";
            if (t.IsFamily) return "Internal";
            return "Private";
        }

        bool IsExternal(TypeNode t) {
            if (t == null) return false;
            if (t.DeclaringModule == null) return true;
            AssemblyNode a = t.DeclaringModule.ContainingAssembly;
            foreach (string path in this.FileNames) {
                if (IsSamePath(a.Location, path))
                    return false;
            }
            return true;
        }


        internal GraphNode AddNode(TypeNode t, string label) {
            GraphNode n;
            if (typemap.ContainsKey(t)) {
                n = typemap[t];
                return n;
            }

            n = new GraphNode();
            n.Id = t.FullName;     
            n.Label = label;
            n.NodeType = GetNodeType(t);
            n.UserData = t;

            typemap[t] = n;
            GraphNode group = null;
            switch (groupFlags)
            {
                case GraphGroup.Namespace:
                    group = AddNamespaceGroup(t);
                    break;
                case GraphGroup.Assembly:
                    group = AddAssemblyGroup(t);
                    break;
                case GraphGroup.None:
                default:
                    break;
            }
            if (group != null)
            {
                group.Nodes.Add(n);
                n.Parent = group;
            }
            else
            {
                AddNode(n);
            }

            newnodes.Add(n);
            return n;
        }

        Dictionary<string, GraphNode> namespaceGroups = new Dictionary<string, GraphNode>();

        GraphNode AddNamespaceGroup(TypeNode t)
        {
            string ns = t.Namespace.ToString();
            if (string.IsNullOrEmpty(ns))
            {
                ns = "<empty namespace>";
            }
            if (namespaceGroups.ContainsKey(ns))
            {
                return namespaceGroups[ns];
            }
            else
            {
                GraphNode n = new GraphNode();
                n.Id = ns;
                n.Label = ns;
                n.NodeType = "Namespace";
                n.UserData = ns;
                namespaceGroups[ns] = n;
                AddNode(n);
                return n;
            }
        }

        Dictionary<AssemblyNode, GraphNode> assemblyGroups = new Dictionary<AssemblyNode, GraphNode>();

        GraphNode AddAssemblyGroup(TypeNode t)
        {
            AssemblyNode a = t.DeclaringModule.ContainingAssembly;
            if (assemblyGroups.ContainsKey(a))
            {
                return assemblyGroups[a];
            }
            else
            {
                GraphNode n = new GraphNode();
                n.Id = a.Name;
                n.Label = a.Name;
                n.NodeType = "Assembly";
                n.UserData = a;
                assemblyGroups[a] = n;
                AddNode(n);
                return n;
            }
        }
    }
}
