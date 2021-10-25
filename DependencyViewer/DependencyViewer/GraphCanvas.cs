using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Automation;
using System.Diagnostics;
using MsaglPlugin;
using System.Windows.Data;

namespace DependencyViewer {

    public class GraphCanvas : Canvas {
        static int next = 0;
        int id = next++;
        bool graphChanged;
        IGraph graph = new MsaglGraph();
        List<IGraph> subGraphs;
        int subComplete;
        Rect srcRect;
        Canvas canvas;
        GraphDirection direction;
        ObservableCollection<GraphEdge> edges;
        ObservableCollection<GraphNode> nodes;
        GraphKeyboadNavigator nav;
        double aspectRatio;
        bool threadRunning;
        GraphCanvas parent;
        bool showEdgeLabels;

        public static double NodeSeparation = 30;
        public static double LayerSeparation = 36;
        public static double NormalEdgeThickness = 1.0; // default unzoomed edge thickness.

        public static readonly DependencyProperty NodeSourceProperty =
            DependencyProperty.Register("NodeSource", typeof(IEnumerable<GraphNode>), typeof(GraphCanvas), new PropertyMetadata(null));
        public static readonly DependencyProperty EdgeSourceProperty =
            DependencyProperty.Register("EdgeSource", typeof(IEnumerable<GraphEdge>), typeof(GraphCanvas), new PropertyMetadata(null));
        public static readonly DependencyProperty EdgeThicknessProperty =
            DependencyProperty.Register("EdgeThickness", typeof(double), typeof(GraphCanvas), new PropertyMetadata(1.0));

        public static readonly RoutedEvent LayoutStartEvent = EventManager.RegisterRoutedEvent("LayoutStart", RoutingStrategy.Bubble, typeof(EventHandler), typeof(GraphCanvas));
        public static readonly RoutedEvent LayoutEndEvent = EventManager.RegisterRoutedEvent("LayoutEnd", RoutingStrategy.Bubble, typeof(EventHandler), typeof(GraphCanvas));
        public event EventHandler LayoutComplete; // routed events don't bubble if diagram is disconnected.

        public GraphCanvas() {
            this.Focusable = true; 
            this.canvas = new Canvas();
            this.Children.Add(canvas);
            nav = new GraphKeyboadNavigator(this);
            Clear();
            this.graph.UserData = this;
            this.EdgeThickness = NormalEdgeThickness;
        }

        public double EdgeThickness
        {
            get { return (double)GetValue(EdgeThicknessProperty); }
            set { SetValue(EdgeThicknessProperty, value); }
        }

        public void Clear() {            
            if (edges == null) {
                this.edges = new ObservableCollection<GraphEdge>();
                this.edges.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnEdgesCollectionChanged);
            } else {
                this.edges.Clear();
            }
            if (nodes == null) {
                this.nodes = new ObservableCollection<GraphNode>();
                this.nodes.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnNodesCollectionChanged);
            } else {
                this.nodes.Clear();
            }
        }

        public double AspectRatio {
            get { return aspectRatio; }
            set { aspectRatio = value; }
        }

        // if this is a subgraph then it keeps a pointer to the parent containing graph.
        protected GraphCanvas ParentGraph {
            get { return this.parent; }
            set { this.parent = value; }
        }

        void OnNodesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            OnGraphChanged();
        }

        void OnEdgesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            OnGraphChanged();
        }

        public UIElementCollection GraphElements {
            get {
                return this.canvas.Children;
            }
        }

        public void OnGraphChanged() {
            graphChanged = true;
            this.InvalidateArrange();
        }

        public ObservableCollection<GraphEdge> Edges {
            get { return this.edges; }
            set {
                if (this.edges != value) {
                    this.edges.CollectionChanged -= new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnEdgesCollectionChanged);
                    this.edges = value;
                    this.edges.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnEdgesCollectionChanged);
                    OnGraphChanged();
                }
            }
        }

        public ObservableCollection<GraphNode> Nodes {
            get { return this.nodes; }
            set {
                if (this.nodes != value) {
                    this.nodes.CollectionChanged -= new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnNodesCollectionChanged);
                    this.nodes = value;
                    this.nodes.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnNodesCollectionChanged);
                    OnGraphChanged();
                }
            }
        }

        public IEnumerable<GraphNode> NodeSource {
            get { return null; }
            set {
                BindNodes(value);
            }
        }

        public IEnumerable<GraphEdge> EdgeSource {
            get { return null; }
            set {
                BindEdges(value);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            string name = e.Property.Name;
            if (name == "NodeSource" || name == "EdgeSource") {
                this.OnGraphChanged();
            }
        }

        void BindEdges(IEnumerable<GraphEdge> value) {
            this.edges.Clear();
            if (value != null) {
                foreach (GraphEdge e in value) 
                    this.edges.Add(e);
            }
        }

        void BindNodes(IEnumerable<GraphNode> value) {
            this.nodes.Clear();
            if (value != null) {
                foreach (GraphNode n in value) 
                    this.nodes.Add(n);
            }
        }

        public bool HasGraph { get { return this.graph != null; } }

        public IGraph InnerGraph { get { return this.graph; } }

        protected override Size ArrangeOverride(Size arrangeSize) {
            Size result = base.ArrangeOverride(arrangeSize);
            try {
                if (this.graphChanged) {                
                    this.Cancel();
                    this.graphChanged = false;
                    this.canvas.Children.Clear();
                    graph.Clear();
                    graph.LayerSeparation = LayerSeparation;
                    if (this.aspectRatio != 0) {
                        graph.AspectRatio = this.aspectRatio;
                    }
                    graph.NodeSeparation = NodeSeparation;
                    SetTransformation();
                    this.subGraphs = new List<IGraph>();

                    if ((this.edges == null || this.edges.Count == 0) &&
                        (this.nodes == null || this.nodes.Count == 0)) {
                        BindEdges(GetValue(GraphCanvas.EdgeSourceProperty) as IEnumerable<GraphEdge>);
                        BindNodes(GetValue(GraphCanvas.NodeSourceProperty) as IEnumerable<GraphNode>);
                    }

                    if (this.edges != null || this.nodes != null) {
                        this.RaiseEvent(new RoutedEventArgs(GraphCanvas.LayoutStartEvent));
                        
                        CreateGraph(this.canvas, this.graph, this.nodes, this.edges);
                        
                        // hide canvas while we lay it out asynchronously.
                        this.canvas.Visibility = Visibility.Hidden;

                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                            new EventHandler(StartLayout), this, EventArgs.Empty);                                
                    }
                }
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
            }
            return result;
        }

        public void Cancel() {
            if (threadRunning) {
                state = ThreadState.Cancelled; // tell thread to stop!
                if (threadRunning) {
                    this.pass2Complete.Set(); // unblock the thread just in case we are in this state.
                    threadFinished.WaitOne(); // wait for it to stop!
                }
                FireCompleteEvents();
            }
            // Stop a pending LayoutDone invoke from thinking graph is done.
            state = ThreadState.Cancelled; 
        }

        public GraphDirection Direction {
            get { return this.direction; }
            set { this.direction = value; }
        }

        public bool ShowEdgeLabels {
            get { return this.showEdgeLabels; }
            set { this.showEdgeLabels = value; }
        }

        void SetTransformation() {
            this.graph.Direction = this.direction;
        }

        protected override Size MeasureOverride(Size constraint) {
            Size s = new Size(this.Width, this.Height);
            if (double.IsNaN(s.Width) || double.IsNaN(s.Height) ||
                double.IsPositiveInfinity(s.Width) || double.IsPositiveInfinity(s.Height)) {
                return new Size(0, 0);
            }
            return s;
        }

        IEdge AddToGraph(GraphEdge ge, IGraph g) {            
            INode s = AddToGraph(ge.Source, g);
            INode t = AddToGraph(ge.Target, g);
            IEdge e = g.AddEdge(s, t);
            e.Weight = ge.Weight;
            e.BiDirectional = ge.BiDirectional;
            e.UserData = ge;
            return e;        
        }

        INode AddToGraph(GraphNode gn, IGraph g) {
            return FindNode(gn, g);
        }

        INode FindNode(GraphNode n, IGraph g) {
            string id = n.Id;
            INode ng = g.FindNode(id);
            if (ng == null) {
                ng = g.AddNode(id);
                ng.UserData = n;
            }
            return ng;
        }

        void CreateGraph(Canvas canvas, IGraph graph, ObservableCollection<GraphNode> nodes, ObservableCollection<GraphEdge> edges) {
            Size defaultSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            if (edges != null) {
                foreach (GraphEdge e in edges) {

                    TextBlock label = null;
                    string automationId = e.Source.Id + "->" + e.Target.Id;
                    if (showEdgeLabels) {
                        label = CreateLabel(e.Label);
                        if (label != null) {
                            e.Text = label;
                            AutomationProperties.SetAutomationId(label, "label:" + automationId);
                            canvas.Children.Add(e.Text);
                        }
                    }
                    IEdge edge = AddToGraph(e, graph);

                    EdgeShape es = new EdgeShape(edge, e, label);
                    Binding edgeBinding = new Binding();
                    edgeBinding.Source = this;
                    edgeBinding.Path = new PropertyPath("EdgeThickness");
                    es.SetBinding(Shape.StrokeThicknessProperty, edgeBinding);

                    AutomationProperties.SetAutomationId(es, automationId);
                    if (label != null) label.Tag = es;
                    edge.UserData = es;                    
                    es.Visibility = Visibility.Hidden;
                    canvas.Children.Add(es);
                }
            }

            if (nodes != null) {
                foreach (GraphNode n in nodes) {
                    string caption = n.Label;
                    if (string.IsNullOrEmpty(caption))
                    {
                        caption = n.Id;
                    }
                    TextBlock label = CreateLabel(caption);
                    if (label != null) {
                        AutomationProperties.SetAutomationId(label, n.Id);
                        n.Text = label;
                        label.Visibility = Visibility.Visible;
                    }
                    INode ng = AddToGraph(n, graph);

                    if (n.Nodes.Count > 0) {
                        
                        SubgraphShape s = new SubgraphShape(this, n, ng, label);
                        if (label != null) label.Tag = s;
                        ng.UserData = s;
                        s.SetValue(NodeShape.NodeTypeProperty, n.NodeType);

                        IGraph subGraph = s.SubGraph;
                        subGraph.Direction = this.direction;
                        this.subGraphs.Add(subGraph);

                        s.Visibility = Visibility.Hidden; // so it doesn't try and draw it until we lay it out.

                        GraphCanvas subcanvas = s.Canvas;
                        subcanvas.Nodes = n.Nodes;
                        subcanvas.Edges = n.Edges;
                        subcanvas.ParentGraph = this;
                        subcanvas.Direction = this.direction;

                        //CreateGraph(s.Canvas, subGraph, n.Nodes, n.Edges);
                        canvas.Children.Add(s);


                    } else {
                        NodeShape ns = new NodeShape(this, n, ng, label);
                        AutomationProperties.SetAutomationId(ns, n.Id);
                        if (label != null) {
                            label.Visibility = Visibility.Visible;
                            label.Tag = ns;
                        }
                        ng.UserData = ns;
                        ns.Tag = n;
                        ns.SetValue(NodeShape.NodeTypeProperty, n.NodeType);

                        ns.Visibility = Visibility.Hidden; // so it doesn't try and draw it until we lay it out.
                        canvas.Children.Add(ns);
                        // get initial size of the node!
                        ns.UpdateLayout();
                        ns.Measure(defaultSize);
                    }
                                        
                }
            }
        }

        void OnSubCanvasLayoutComplete(GraphCanvas child) {
            lock (this) {
                subComplete++;
            }
            this.childComplete.Set();
        }

        void PushDataFromLayoutGraphToFrameworkElements(IGraph layoutGraph, Rect graphBounds) {
            foreach (IEdge edge in layoutGraph.Edges) {
                PushEdgeLayoutInfoToTheFramework(edge, graphBounds);
            }
            foreach (INode node in layoutGraph.Nodes) {
                PushNodeLayoutInfoToTheFramework(node, graphBounds);
            }
        }

        void PushGeometryInfoToLayoutGraph() {
            // note: must be on UI thread.               
            if (this.subGraphs != null) {
                foreach (IGraph g in this.subGraphs) {
                    // Layout the subgraphs first
                    PushGeometryInfoToLayoutGraph(g);
                }
            }
            if (this.graph != null) {
                // Now we can do the master layout.
                PushGeometryInfoToLayoutGraph(this.graph);
            }
        }

        void PushGeometryInfoToLayoutGraph(IGraph graph) {
            foreach (INode node in graph.Nodes) {
                NodeShape ns = node.UserData as NodeShape;
                if (ns != null) {
                    node.SetBoundaryCurve(ns.GetVisualGeometry(), ns.ActualWidth, ns.ActualHeight);
                } else {
                    Debug.Assert(false, "Node is missing NodeShape");
                }
            }
            foreach (IEdge edge in graph.Edges) {
                EdgeShape drawingEdge = edge.UserData as EdgeShape;
                if (drawingEdge != null) {
                    TextBlock label = drawingEdge.Label;
                    if (label != null) {
                        edge.SetLabel(label.ActualWidth + 3, label.ActualHeight+3);
                    }
                } else {
                    Debug.Assert(false, "Edge is missing EdgeShape");
                }
            }
        }

        enum ThreadState { None, Start, Started, Pass1, Done, Cancelled };
        private ThreadState state;
        private AutoResetEvent childComplete = new AutoResetEvent(false);
        private AutoResetEvent pass2Complete = new AutoResetEvent(false);
        private AutoResetEvent threadFinished = new AutoResetEvent(false);

        void StartLayout(object sender, EventArgs e) {
            this.subComplete = 0;
            this.state = ThreadState.Start;
            this.PushGeometryInfoToLayoutGraph();
            // start the thread!
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.Calculate), null);
        }

        void StartPass2(object sender, EventArgs e) {
            if (state == ThreadState.Pass1) {
                Size defaultSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

                //Transfer the geometry of nodes and edges from the layout graph to the viewer
                foreach (IGraph subg in this.subGraphs) {
                    SubgraphShape n = subg.UserData as SubgraphShape;

                    n.InvalidateMeasure();
                    n.InvalidateArrange();
                    n.UpdateLayout();

                    Rect bounds = subg.BoundingBox;
                    Vector center = new Vector((n.Width - bounds.Width) / 2, (n.Height - bounds.Height) / 2);
                    bounds.Offset(center);

                    PushDataFromLayoutGraphToFrameworkElements(subg, bounds);

                }

                // Now need to recalculate the NodeBoundaryCurve for the outer subgraph shapes
                // now that we've measured them properly.
                this.PushGeometryInfoToLayoutGraph();
            }
            // Must always set this to unblock the background thread.
            pass2Complete.Set();
        }

        void LayoutDone(object sender, EventArgs e) {
            if (state == ThreadState.Done) {
                srcRect = this.graph.BoundingBox;
            
                PushDataFromLayoutGraphToFrameworkElements(this.graph, srcRect);

                this.Width = canvas.Width = srcRect.Width;
                this.Height = canvas.Height = srcRect.Height;                    

                // show canvas now that we're done.
                this.canvas.Visibility = Visibility.Visible;

                FireCompleteEvents();
            }
        }

        void FireCompleteEvents() {
            this.RaiseEvent(new RoutedEventArgs(GraphCanvas.LayoutEndEvent));
            if (LayoutComplete != null)
                LayoutComplete(this, new EventArgs());
        }

        private void PushNodeLayoutInfoToTheFramework(INode node, Rect graphBounds) {
            NodeShape ns = node.UserData as NodeShape;
            Rect r = ns.NodeBounds;
            double dx = -graphBounds.Left + r.Left;
            double dy = -graphBounds.Top + r.Top;
            Canvas.SetLeft(ns, dx);
            Canvas.SetTop(ns, dy);
            ns.Visibility = Visibility.Visible; // now we can draw it!            
            ns.InvalidateArrange();
            ns.InvalidateMeasure();
        }

        private void PushEdgeLayoutInfoToTheFramework(IEdge e, Rect graphBounds) {
            EdgeShape es = e.UserData as EdgeShape;
            es.RenderTransform = new TranslateTransform(-graphBounds.Left, -graphBounds.Top);
            TextBlock edgeLabel = es.Label;
            if (edgeLabel != null) {
                //update position of the existing id
                SetLabelPosition(-graphBounds.Left + e.LabelCenter.X - edgeLabel.ActualWidth / 2,
                  -graphBounds.Top + e.LabelCenter.Y - edgeLabel.ActualHeight / 2, edgeLabel);
            }
            es.Visibility = Visibility.Visible; // now we can draw it!            
            es.InvalidateVisual(); // re-calc DefiningGeometry.            
                
        }


        TextBlock CreateLabel(string label) {
            if (!string.IsNullOrEmpty(label)) {
                TextBlock t = new TextBlock();
                t.Text = label;
                t.Visibility = Visibility.Hidden; // wait till layout is done.
                return t;
            }
            return null;
        }

        private void SetLabelPosition(double x, double y, TextBlock label) {
            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, y);

            label.Visibility = Visibility.Visible;
            label.InvalidateMeasure();
            label.InvalidateArrange();

            Canvas.SetZIndex(label, 1);
        }

        bool HasSubGraphs {
            get { return this.subGraphs != null && this.subGraphs.Count != 0; }
        }

        static int threadcount = 0;
        internal void Calculate(object arg) {
            try {
                lock (this) {
                    threadRunning = true;
                    threadcount++;
                }
                if (state == ThreadState.Cancelled) return;

                state = ThreadState.Started;

                if (HasSubGraphs) {
                    int count = this.subGraphs.Count;
                    while (subComplete < count) {
                        childComplete.WaitOne();
                        if (state == ThreadState.Cancelled) return; // cancelling
                    }
                    state = ThreadState.Pass1;
                    this.pass2Complete.Reset();
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new EventHandler(StartPass2), this, EventArgs.Empty);
                    this.pass2Complete.WaitOne();

                    if (state == ThreadState.Cancelled) return;
                }

                if (this.graph != null && state != ThreadState.Cancelled) {
                    // Now we can do the master layout.
                    this.graph.CalculateLayout();

                    if (state != ThreadState.Cancelled) {
                        state = ThreadState.Done;
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                            new EventHandler(LayoutDone), this, EventArgs.Empty);
                    }
                }
            } catch (ThreadAbortException) {
                // ignore this cancellation.
            } finally {
                lock (this) {
                    threadcount--;
                    threadRunning = false;
                }
                if (parent != null) {
                    // make sure parent always gets this callback!
                    parent.OnSubCanvasLayoutComplete(this);
                }
                threadFinished.Set();
            }
        }

    }
}