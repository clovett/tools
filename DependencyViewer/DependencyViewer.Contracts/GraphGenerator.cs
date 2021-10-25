using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows;

namespace DependencyViewer {

    public class EdgeMap : Dictionary<GraphNode, IDictionary<GraphNode, GraphEdge>> {
    }
    
    public abstract class GraphGenerator  {        
        GraphDirection direction;
        string[] fileNames;
        EdgeMap edges;
        EdgeMap inverseEdges;
        IDictionary<string, string> disconnected;
        IDictionary<string, string> hidden;
        IList<GraphNode> nodes;
        Panel container;
        EditingContext context;
        Regex fileFilterRegex;
        double complexityRatio = 1; // show all edges by default.
        bool showEdgeLabels = true;

        protected GraphGenerator() {
        }

        public EditingContext Context {
            get {
                return context;
            }
            set {
                context = value;
                context.SelectionChanged += new SelectionChangedEventHandler(OnSelectionChanged);
            }
        }


        public event EventHandler BeforeChange;
        public event EventHandler AfterChange;

        protected virtual void OnBeforeChange() {
            if (BeforeChange != null) BeforeChange(this, EventArgs.Empty);
        }

        protected virtual void OnAfterChange() {
            if (AfterChange != null) AfterChange(this, EventArgs.Empty);
        }

        public EdgeMap Edges { get { return this.edges; } }

        public IEnumerable<GraphNode> Nodes { get { return this.nodes; } }

        public virtual string FileFilter {
            get {
                return "All Files (*.*)|*.*";
            }
        }

        public bool ShowEdgeLabels {
            get { return this.showEdgeLabels; }
            set { 
                this.showEdgeLabels = value;
                this.edgeLabelsMenu.IsChecked = value;
            }
        }

        public double ComplexityRatio {
            get { return this.complexityRatio; }
            set { 
                this.complexityRatio = value;
                this.moreComplexityMenu.IsEnabled = complexityRatio < 1;
            }
        }

        public bool FilterMatches(string filename) {
            if (this.fileFilterRegex == null) {
                string[] parts = this.FileFilter.Split('|');    // split description from actual list of file types
                if (parts.Length != 2) {
                    this.fileFilterRegex = new Regex("^(?!)$"); // never match
                } else {
                    this.fileFilterRegex = ConstructRegexForWildcards(parts[1]);
                }
            }
            return this.fileFilterRegex.IsMatch(filename);
        }

        // Construct a regular expression for a semicolon-separated list of DOS wildcards.
        private static Regex ConstructRegexForWildcards(string wildcards) {
            StringBuilder sb = new StringBuilder(wildcards.Length * 2 + 4);
            sb.Append("^(");
            foreach (char ch in wildcards) {
                switch (ch) {
                    // See "Character Escapes" at http://msdn2.microsoft.com/en-us/library/4edbef7e.aspx
                    case '.':
                    case '$':
                    case '^':
                    case '{':
                    case '[':
                    case '(':
                    case '|':
                    case ')':
                    case '+':
                    case '\\':
                        sb.Append('\\');
                        goto default;
                    case '*':
                        sb.Append(".*");
                        break;
                    case '?':
                        sb.Append('.');
                        break;
                    case ';':
                        sb.Append('|');
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            sb.Append(")$");
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase);
        }

        public string[] FileNames {
            get { return this.fileNames; }
            set { this.fileNames = value; }
        }

        public GraphDirection Direction {
            get { return direction; }
            set { direction = value; }
        }

        // can run this on background thread.
        public virtual void Prepare() {
        }

        public abstract string Label { get; }

        // this must be called on the UI thread.
        public virtual void Create(Panel container) {
            this.container = container;
            
            edges = new EdgeMap();
            inverseEdges = new EdgeMap();
            nodes = new List<GraphNode>();
        }

        protected GraphEdge AddEdge(GraphNode node, GraphNode target, string edgeType) {
            if (node == target) return null;
            if (node.Parent != target.Parent)
            {
                // we cannot currently vizualize edges across groups, so instead we turn
                // this into a inter-group link.  Would be nice to also be able to show
                // the node-to-node link some day when the user selects one of these nodes.
                GraphNode fromGroup = node.Parent;
                GraphNode toGroup = target.Parent;
                foreach (GraphEdge e in fromGroup.Edges)
                {
                    if (e.Target == toGroup)
                    {
                        // already added!
                        e.Weight++; 
                        return e; // reuse this edge
                    }
                }
                return AddEdge(fromGroup, toGroup, edgeType);
            }

            if (this.IsDisconnected(node) || this.IsDisconnected(target) ||
                this.IsHidden(node) || this.IsHidden(target))
                return null; // ignore disconnected nodes!

            GraphEdge edge = GetEdge(node, target);
            if (edge != null) {
                // already added!
                edge.Weight++;
                return edge;
            }

            edge = new GraphEdge();
            edge.Source = node;
            edge.Target = target;
            edge.EdgeType = edgeType;

            AddEdge(this.edges, node, target, edge);
            AddEdge(this.inverseEdges, target, node, edge);

            if (node.Parent != null)
            {
                node.Parent.Edges.Add(edge);
            }
            
            return edge;
        }

        GraphEdge GetEdge(GraphNode source, GraphNode target) {            
            if (edges.ContainsKey(source)) {
                IDictionary<GraphNode, GraphEdge> outEdges = this.edges[source];
                if (outEdges.ContainsKey(target)) {
                    return outEdges[target];
                }
            }
            if (this.inverseEdges.ContainsKey(source))
            {
                IDictionary<GraphNode, GraphEdge> inEdges = this.inverseEdges[source];
                if (inEdges.ContainsKey(target))
                {
                    GraphEdge e = inEdges[target];
                    if (string.IsNullOrEmpty(e.EdgeType))
                    {
                        e.BiDirectional = true;
                        return e;
                    }
                }
            }
            return null;
        }

        void AddEdge(EdgeMap map, GraphNode source, GraphNode target, GraphEdge edge) {
            IDictionary<GraphNode, GraphEdge> list;
            if (map.ContainsKey(source)) {
                list = map[source];
                if (list.ContainsKey(target)) {
                    return;
                }
            } else {
                list = new Dictionary<GraphNode, GraphEdge>();
                map[source] = list;
            }

            list[target] = edge;                       
        }

        protected void AddNode(GraphNode node) {
            if (IsHidden(node)) return;
            nodes.Add(node);
        }

        IDictionary<string, Uri> cache = new Dictionary<string, Uri>();
        public Uri GetUri(string path) {
            if (!cache.ContainsKey(path)) {
                cache[path] = new Uri(path);
            }
            return cache[path];
        }

        public bool IsSamePath(string a, string b) {
            Uri u = GetUri(a);
            Uri w = GetUri(b);
            return u == w;
        }

        public virtual void CreateViewMenu(MenuItem menu) {            
        }

        public virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            //throw new NotImplementedException();
        }

        MenuItem hideMenu;
        MenuItem showMenu;
        MenuItem disconnect;
        MenuItem reconnect;
        MenuItem lessComplexityMenu;
        MenuItem moreComplexityMenu;
        MenuItem edgeLabelsMenu;

        public virtual void CreateContextMenu(ContextMenu menu) {
            disconnect = CreateMenuItem(menu, "_Disconnect", "Disconnect selected nodes from the graph",
                new RoutedEventHandler(OnDisconnect));
            reconnect = CreateMenuItem(menu, "_Reconnect", "Reconnect selected nodes back into the graph",
                new RoutedEventHandler(OnReconnect));
            menu.Items.Add(new Separator());
            hideMenu = CreateMenuItem(menu, "_Show selected nodes only", "Show only the selected nodes",
                new RoutedEventHandler(OnShow));
            showMenu = CreateMenuItem(menu, "_Hide selected nodes", "Hide the selected nodes",
                new RoutedEventHandler(OnHide));
            menu.Items.Add(new Separator());
            
            CreateMenuItem(menu, "Sh_ow selected graphs only", "Show only those nodes directly or indirectly connected to the selected nodes",
                new RoutedEventHandler(OnShowGraph));
            CreateMenuItem(menu, "H_ide selected graphs", "Hide those nodes directly or indirectly connected to the selected nodes",
                new RoutedEventHandler(OnHideGraph));
            menu.Items.Add(new Separator());
            lessComplexityMenu = CreateMenuItem(menu, "Show _less complexity", "Show less edges",
                new RoutedEventHandler(OnShowLessEdges));
            moreComplexityMenu = CreateMenuItem(menu, "Show _more complexity", "Show more edges",
                new RoutedEventHandler(OnShowMoreEdges));
            edgeLabelsMenu = CreateMenuItem(menu, "Show _edge labels", "Show or hide the edge labels",
                new RoutedEventHandler(OnShowEdgeLabels));
            CreateMenuItem(menu, "Rese_t", "Reset visibility of all nodes and edges back to default",
                new RoutedEventHandler(OnReset));

            // initialize menu state.
            this.ComplexityRatio = this.complexityRatio;
            this.ShowEdgeLabels = this.showEdgeLabels;
        }

        GraphNode contextSelected;

        public virtual void SetContextMenuState(GraphNode ns) {
            contextSelected = ns;
            if (ns != null) {
                reconnect.IsEnabled = this.IsDisconnected(ns);
                disconnect.IsEnabled = !reconnect.IsEnabled;
                hideMenu.IsEnabled = showMenu.IsEnabled = true;
            } else {
                reconnect.IsEnabled = disconnect.IsEnabled = false;
                hideMenu.IsEnabled = showMenu.IsEnabled = false;
            }
        }

        MenuItem CreateMenuItem(ContextMenu menu, string label, string tooltip, RoutedEventHandler handler) {
            MenuItem item = new MenuItem();
            item.Header = label;
            item.ToolTip = tooltip;
            item.Click += handler;
            menu.Items.Add(item);
            return item;
        }

        public IEnumerable<GraphNode> SelectedNodes {
            get {
                List<GraphNode> snapshot = new List<GraphNode>(context.SelectedNodes);
                if (contextSelected != null && !snapshot.Contains(contextSelected)) {
                    snapshot.Add(contextSelected);
                }
                return snapshot;
            }
        }

        void OnShowEdgeLabels(object sender, RoutedEventArgs e) {
            OnBeforeChange();
            this.ShowEdgeLabels = !this.showEdgeLabels;
            OnAfterChange();
        }

        void OnShowMoreEdges(object sender, RoutedEventArgs e) {
            OnBeforeChange();
            double ratio = this.ComplexityRatio * 4.0 / 3.0;
            if (ratio > 1) ratio = 1;
            this.ComplexityRatio = ratio;
            OnAfterChange();
        }

        void OnShowLessEdges(object sender, RoutedEventArgs e) {
            OnBeforeChange();
            double ratio = this.complexityRatio * 3.0 / 4.0;
            this.ComplexityRatio = ratio;
            OnAfterChange();
        }

        void OnShow(object sender, RoutedEventArgs e) {
            ShowAll(SelectedNodes);
        }

        void OnHide(object sender, RoutedEventArgs e) {
            HideAll(SelectedNodes);
        }

        void OnDisconnect(object sender, RoutedEventArgs e) {
            Disconnect(SelectedNodes);
        }

        void OnReconnect(object sender, RoutedEventArgs e) {
            Reconnect(SelectedNodes);
        }

        void OnReset(object sender, RoutedEventArgs e) {
            Reset();
        }

        void OnShowGraph(object sender, RoutedEventArgs e) {
            ShowGraph(SelectedNodes);
        }

        void OnHideGraph(object sender, RoutedEventArgs e) {
            HideGraph(SelectedNodes);
        }

        public void Disconnect(IEnumerable<GraphNode> e) {

            OnBeforeChange();
            if (disconnected == null) disconnected = new Dictionary<string, string>();
            if (e != null) {
                foreach (GraphNode n in e) {
                    disconnected[n.Id] = n.Id;
                }
            }
            OnAfterChange();
        }

        public void Reconnect(IEnumerable<GraphNode> e) {
            if (e != null) {
                OnBeforeChange();
                foreach (GraphNode n in e) {
                    if (IsDisconnected(n)) {
                        disconnected.Remove(n.Id);
                    }
                }
                OnAfterChange();
            }
        }

        public bool IsDisconnected(GraphNode n) {
            return (disconnected != null && disconnected.ContainsKey(n.Id));
        }

        public virtual void Reset() {
            if ((disconnected != null && disconnected.Count > 0 ||
                (hidden != null && hidden.Count > 0)) || this.complexityRatio < 1 || !this.ShowEdgeLabels) {
                OnBeforeChange();
                if (disconnected != null) disconnected.Clear();
                if (hidden != null) hidden.Clear();
                this.ComplexityRatio = 1;
                this.ShowEdgeLabels = true;
                OnAfterChange();
            }
        }

        public void ShowGraph(IEnumerable<GraphNode> e) {
            OnBeforeChange();
            // show only those nodes and edges connected to this node.
            IDictionary<GraphNode, GraphNode> connected = new Dictionary<GraphNode, GraphNode>();
            if (e != null) {
                foreach (GraphNode n in e) {
                    AddConnected(connected, n);
                }
            }
            
            foreach (GraphNode m in this.Nodes) {
                if (!connected.ContainsKey(m))
                    Hide(m);
            }
            OnAfterChange();
        }

        public void HideGraph(IEnumerable<GraphNode> e) {
            OnBeforeChange();
            // hide all nodes and edges connected to this node.
            IDictionary<GraphNode, GraphNode> connected = new Dictionary<GraphNode, GraphNode>();
            if (e != null) {
                foreach (GraphNode n in e) {
                    AddConnected(connected, n);
                }
            }
            foreach (GraphNode m in connected.Keys) {
                Hide(m);
            }
            OnAfterChange();
        }

        void AddConnected(IDictionary<GraphNode, GraphNode> connected, GraphNode n ) {
            connected[n] = n;

            if (this.edges.ContainsKey(n)) {
                IDictionary<GraphNode, GraphEdge> list = this.edges[n];
                foreach (GraphEdge e in list.Values) {
                    if (!connected.ContainsKey(e.Target)) 
                        AddConnected(connected, e.Target);
                }
            }
                    
            if (this.inverseEdges.ContainsKey(n)) {
                IDictionary<GraphNode, GraphEdge> list = inverseEdges[n];
                foreach (GraphEdge e in list.Values) {
                    if (!connected.ContainsKey(e.Source))
                        AddConnected(connected, e.Source);
                }
            }
        }

        public void HideAll(IEnumerable<GraphNode> e) {
            OnBeforeChange();
            foreach (GraphNode n in e) {
                Hide(n);
            }
            OnAfterChange();
        }

        public void ShowAll(IEnumerable<GraphNode> e) {
            OnBeforeChange();
            HideAll();
            foreach (GraphNode n in e) {
                Show(n);
            }
            OnAfterChange();
        }

        public void HideAll() {
            foreach (GraphNode m in this.Nodes) {
                Hide(m);
            }
        }

        public void Hide(GraphNode n) {
            if (hidden == null) hidden = new Dictionary<string, string>();
            hidden[n.Id] = n.Id;
        }

        public void Show(GraphNode n) {
            if (hidden != null && hidden.ContainsKey(n.Id)) {
                hidden.Remove(n.Id);
            }
        }

        public bool IsHidden(GraphNode n) {
            return (hidden != null && hidden.ContainsKey(n.Id));
        }

        public virtual void SaveState(ViewState state) {
            if (hidden != null) state["hidden"] = new Dictionary<string, string>(hidden);
            if (disconnected != null) state["disconnected"] = new Dictionary<string, string>(disconnected);
            state["ComplexityRatio"] = this.complexityRatio;
            state["ShowEdgeLabels"] = this.showEdgeLabels;
        }

        public virtual void LoadState(ViewState state) {
            hidden = state["hidden"] as Dictionary<string, string>;
            disconnected = state["disconnected"] as Dictionary<string, string>;
            this.ComplexityRatio = (double)state["ComplexityRatio"];
            this.ShowEdgeLabels = (bool)state["ShowEdgeLabels"];
        }
    }
}
