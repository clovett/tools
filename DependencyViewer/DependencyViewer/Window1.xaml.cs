using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Xml.Schema;
using System.Xml;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Sample.Controls;

namespace DependencyViewer {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class Window1 : Window {

        GraphDirection direction = GraphDirection.TopToBottom;
        string[] fileNames;
        MapZoom zoom;
        RectangleSelectionGesture rzoom;
        Pan pan;
        AutoScroll autoScroll;
        HoverGesture hover;
        ResourceDictionary theme;
        EditingContext context = new EditingContext();
        FileDragDrop fdd1;
        FileDragDrop fdd2;
        Settings settings;
        ViewHistory history = new ViewHistory();
        ToolTip tip = new ToolTip();
        string source;
        RecentFilesMenu recentFiles;
        
        public static readonly RoutedEvent LayoutPendingEvent = EventManager.RegisterRoutedEvent("LayoutPending", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Window1));
        public static readonly RoutedEvent LayoutCompleteEvent = EventManager.RegisterRoutedEvent("LayoutComplete", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Window1));

        // commands
        public readonly static RoutedUICommand ReloadFileCommand;
        public readonly static RoutedUICommand CopyImageCommand;
        public readonly static RoutedUICommand CopyUnscaledCommand;
        public readonly static RoutedUICommand PageSetupCommand;
        
        static Window1() {
            //ApplicationCommands.ex
            ReloadFileCommand = new RoutedUICommand("Reload", "ReloadFileCommand", typeof(Window1));
            CopyImageCommand = new RoutedUICommand("Copy Image", "CopyImageCommand", typeof(Window1));
            CopyUnscaledCommand = new RoutedUICommand("Copy Unscaled", "CopyUnscaledCommand", typeof(Window1));
            PageSetupCommand = new RoutedUICommand("Page Setup...", "PageSetupCommand", typeof(Window1));            
        }

        public Window1() {
            settings = new Settings();

            this.AllowDrop = true;
            
            InitializeComponent();

            context.Container = panel;
            panel.SetValue(EditingContext.EditingContextProperty, this.context);

            this.Closing += new System.ComponentModel.CancelEventHandler(Window_Closing);

            recentFiles = new RecentFilesMenu(RecentFilesParent);
            recentFiles.RecentFileSelected += new EventHandler<RecentFileEventArgs>(OnRecentFileSelected);

            zoom = new MapZoom(panel, ModifierKeys.Control);
            this.zoom.ZoomChanged += new EventHandler(OnZoomChanged);

            rzoom = new RectangleSelectionGesture(panel, zoom, ModifierKeys.Control);
            rzoom.ZoomSelection = true;

            pan = new Pan(panel, zoom, ModifierKeys.None);
            autoScroll = new AutoScroll(panel, zoom);

            hover = new HoverGesture(panel);
            hover.Hover += new EventHandler(OnHover);
            hover.HoverCancelled += new EventHandler(OnHoverCancelled);
            tip.StaysOpen = true;

            Keyboard.AddGotKeyboardFocusHandler(this, new KeyboardFocusChangedEventHandler(OnFocusChanged));

            fdd1 = new FileDragDrop(this);
            fdd1.FilesDropped += new EventHandler<FileEventArgs>(OnFilesDropped);            
            fdd2 = new FileDragDrop(this.FDoc);
            fdd2.FilesDropped += new EventHandler<FileEventArgs>(OnFilesDropped);            

            context.SelectionChanged += new SelectionChangedEventHandler(OnSelectionChanged);

            scroller.Target = panel;
            scroller.Zoom = this.zoom;

            // Provide defaults and type information.
            settings.Changed += new EventHandler<SettingChangedEventArgs>(OnSettingsChanged);
            settings["LayerSeparation"] = (double)GraphCanvas.LayerSeparation;
            settings["NodeSeparation"] = (double)GraphCanvas.NodeSeparation;
            settings["AspectRatio"] = false;
            settings["EdgeThickness"] = (double)GraphCanvas.NormalEdgeThickness;
            settings["FindStrings"] = new List<string>();
            settings["Skin"] = "Glass";
            settings["RecentFiles"] = new List<Uri>();
            settings.Load(ConfigFile);

            Plugins.LoadGenerators();

            history.Changed += new EventHandler(OnHistoryChanged);
            this.BackButton.IsEnabled = false;
            this.ForwardButton.IsEnabled = false;
            this.SaveMenu.IsEnabled = false;
        }

        void OnNewWindow(object sender, EventArgs e) {
            Window1 w = new Window1();
            w.Show();
        }

        void OnHistoryChanged(object sender, EventArgs e) {
            Application a = App.Current;
            this.BackButton.IsEnabled = history.CanGoBack;
            this.ForwardButton.IsEnabled = history.CanGoForward;
        }

        void OnSettingsChanged(object sender, SettingChangedEventArgs e) {
            switch (e.Name) {
                case "NodeSeparation":
                    GraphCanvas.NodeSeparation = (double)e.NewValue;                    
                    InvalidateGraphLayout();
                    break;
                case "LayerSeparation":
                    GraphCanvas.LayerSeparation = (double)e.NewValue;
                    InvalidateGraphLayout();
                    break;
                case "EdgeThickness":
                    GraphCanvas.NormalEdgeThickness = (double)e.NewValue;
                    OnZoomChanged(sender, e);
                    break;
                case "AspectRatio":
                    InvalidateGraphLayout();
                    break;
                case "Skin":
                    ApplySkin((string)e.NewValue);
                    break;
                case "RecentFiles":
                    recentFiles.SetFiles(e.NewValue as List<Uri>);
                    break;
            }
        }

        void OnRecentFileSelected(object sender, RecentFileEventArgs e)
        {
            if (gen != null) SaveState();
            string[] names = new string[1];
            names[0] = e.Uri.IsFile ? e.Uri.LocalPath : e.Uri.AbsoluteUri;
            ProcessFiles(names);
        }

        public string ConfigFile {
            get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Microsoft\\DependencyViewer\\DependencyViewer.xml"); 
            }
        }

        void ShowOptions(object sender, RoutedEventArgs e) {
            Options opt = new Options(settings);
            opt.Show();
        }

        void OnFilesDropped(object sender, FileEventArgs e) {
            ProcessFiles(e.FileNames);
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            settings["RecentFiles"] = recentFiles.RecentFiles;
            settings.Save(ConfigFile);            
            CancelLayout();
            if (find != null) find.Close();
        }

        FindDialog find;
        GraphSearcher searcher;

        void OnFind(object sender, RoutedEventArgs e) {
            if (find == null) {
                searcher = new GraphSearcher(context);
                find = new FindDialog(searcher, settings);
                find.Closing += new System.ComponentModel.CancelEventHandler(find_Closing);
            } 
            find.Show();
        }

        void find_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            find = null;
        }

        void Exit(object sender, RoutedEventArgs e) {
            this.Close();
        }

        void dockPanel_Initialized(object sender, EventArgs e) {
            if (App.Args.Length > 0) {
                ProcessFiles(App.Args);
            }
        }

        void graph_LayoutComplete(object sender, EventArgs e) {
            // adjust zoom so diagram fits on screen.      
            RefreshLayout();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            base.OnRenderSizeChanged(sizeInfo);
            RefreshLayout();
        }

        void OnZoomChanged(object sender, EventArgs e) {

            clipper.InvalidateMeasure();
            clipper.InvalidateArrange();

            foreach (UIElement c in this.panel.Children)
            {
                GraphCanvas graph = c as GraphCanvas;
                if (graph != null)
                {
                    graph.EdgeThickness = GraphCanvas.NormalEdgeThickness / zoom.Zoom;
                    foreach (UIElement child in graph.GraphElements)
                    {
                        SubgraphShape subgraph = child as SubgraphShape;
                        if (subgraph != null)
                        {
                            // then the parent was really a "group"
                            graph.EdgeThickness = (GraphCanvas.NormalEdgeThickness * 2.0);// / zoom.Zoom;

                            subgraph.Canvas.EdgeThickness = GraphCanvas.NormalEdgeThickness ;/// zoom.Zoom;
                        }
                    }
                }
            }
        }


        void OnHover(object sender, EventArgs e)
        {
            TipBuilder result = new TipBuilder();
            string label = "Name";
            Point pt = Mouse.GetPosition(panel);
            HitTestResult r = VisualTreeHelper.HitTest(panel, pt);
            if (r != null && r.VisualHit != null)
            {
                DependencyObject p = r.VisualHit;
                while (p != null)
                {
                    NodeShape ns = p as NodeShape;
                    if (ns != null)
                    {
                        GraphNode node = (GraphNode)ns.Tag;
                        result.AddRow(label, node.Label);
                        if (node.Id != node.Label)
                        {
                            result.AddRow("Id", node.Id);
                        }
                        label = "Group";
                    }
                    else
                    {
                        EdgeShape es = p as EdgeShape;
                        if (es != null && es.Edge != null)
                        {
                            GraphEdge edge = (GraphEdge)es.Tag;
                            if (!string.IsNullOrEmpty(edge.Label))
                            {
                                result.AddRow("Name", edge.Label);
                            }
                            result.AddRow("From", edge.Source.Label);
                            result.AddRow("To", edge.Target.Label);
                            if (!string.IsNullOrEmpty(edge.Tip))
                            {
                                result.AddRow("Reason", edge.Tip);
                            }
                            label = "Group";
                        }
                    }
                    p = VisualTreeHelper.GetParent(p);
                }
            }
            if (result.Rows > 0)
            {
                tip.Content = result.Grid;
                tip.IsOpen = true;
            }
        }

        void OnHoverCancelled(object sender, EventArgs e)
        {
            tip.IsOpen = false;
        }

        class TipBuilder
        {
            Grid grid;
            int rows;

            public TipBuilder()
            {
                grid = new Grid();
                ColumnDefinition col1 = new ColumnDefinition();
                col1.Width = new GridLength(1, GridUnitType.Auto);
                ColumnDefinition col2 = new ColumnDefinition();
                col2.Width = new GridLength(1, GridUnitType.Auto);
                grid.ColumnDefinitions.Add(col1);
                grid.ColumnDefinitions.Add(col2);
            }

            public  void AddRow(string label, string value){
                RowDefinition rd = new RowDefinition();
                rd.Height = new GridLength(1, GridUnitType.Auto);
                grid.RowDefinitions.Add(rd);

                TextBlock label1 = new TextBlock();
                label1.Text = label + ": ";
                Grid.SetColumn(label1, 0);
                Grid.SetRow(label1, rows);
                grid.Children.Add(label1);

                TextBlock name = new TextBlock();
                name.Text = value;
                Grid.SetColumn(name, 1);
                Grid.SetRow(name, rows);
                grid.Children.Add(name);
                rows++;
            }

            public Grid Grid { get { return grid; } }
            public int Rows { get { return rows; } }

        }

        void ShowHelp(object sender, RoutedEventArgs e) {
            ShowHelp(true);
        }

        void CloseHelp(object sender, RoutedEventArgs e) {
            ShowHelp(false);
        }

        void ShowHelp(bool show) {
            Visibility vis = show ? Visibility.Visible : Visibility.Hidden;
            Help.Visibility = CloseBox.Visibility = vis;
            if (show) {
                System.Windows.Input.NavigationCommands.GoToPage.Execute(1, this.Help);
            } else {
                scroller.InvalidateVisual();
            }            
        }

        GraphGenerator gen;
        List<MenuItem> items;

        void ProcessFiles(string[] fileNames) {            
            CancelLayout();
            ShowHelp(false);
            if (this.zoom != null) this.zoom.ResetTranslate();
            try {
                foreach (string name in fileNames)
                {
                    recentFiles.AddRecentFile(new Uri(name));
                }

                if (gen == null || FilesChanged(fileNames)) {
                    ShowStatus("Loading files...");
                    this.fileNames = fileNames;
                    this.ViewMenu.Items.Clear();
                    gen = Plugins.Create(fileNames);
                    if (gen != null) {
                        if (gen is DgmlGenerator && fileNames.Length == 1) {
                            this.source = fileNames[0];
                            this.SaveMenu.IsEnabled = true;
                        }
                        gen.Context = this.context;
                        items = new List<MenuItem>();
                        gen.Direction = this.direction;
                        gen.CreateViewMenu(this.ViewMenu);
                        // just in case this is a graph generator we've seen before.
                        gen.BeforeChange -= new EventHandler(OnBeforeGraphChanged);
                        gen.AfterChange -= new EventHandler(OnAfterGraphChanged);
                        gen.BeforeChange += new EventHandler(OnBeforeGraphChanged);
                        gen.AfterChange += new EventHandler(OnAfterGraphChanged);
                        ContextMenu contextMenu = new ContextMenu();
                        gen.CreateContextMenu(contextMenu);
                        panel.ContextMenu = contextMenu;
                        ThreadPool.QueueUserWorkItem(new WaitCallback(LoadAssemblies));
                    } 
                } else {
                    gen.Direction = this.direction;
                    CreateAndLayoutGraph(this, EventArgs.Empty);
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message, "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowStatus("");
            }
        }


        void OnBeforeGraphChanged(object sender, EventArgs e) {
            SaveState();
        }

        void OnAfterGraphChanged(object sender, EventArgs e) {
            ReloadFiles(sender, new RoutedEventArgs());            
        }

        bool FilesChanged(string[] fileNames) {
            if (this.fileNames == null || this.fileNames.Length != fileNames.Length) return true;
            for (int i = 0; i < this.fileNames.Length; i++) {
                if (this.fileNames[i] != fileNames[i])
                    return true;
            }
            return false;
        }


        void ShowStatus(string msg) {
            toolStripStatusLabel.Text = msg;
        }

        delegate void StringHandler(string s);

        public void LoadAssemblies(object state) {
            try {
                gen.Prepare();
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new EventHandler(CreateAndLayoutGraph), this, EventArgs.Empty);
            } catch (Exception e) {
                MessageBox.Show(e.Message, "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                ThreadSafeSetStatus("");
            }
        }

        void ThreadSafeSetStatus(string status) {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                new StringHandler(ShowStatus), status);
        }

        public void CreateAndLayoutGraph(object sender, EventArgs e) {
            if (gen != null) {
                CancelLayout();
                ClearPanel();
                if (this.zoom != null) this.zoom.ResetTranslate();
                ShowStatus("Creating Graph...");
                try {
                    gen.Create(this.panel);
                    GraphBuilder builder = new GraphBuilder(this.direction);
                    builder.Build(this.panel, gen);

                    // See if there's any new menu items...
                    foreach (object o in this.ViewMenu.Items) {
                        MenuItem mi = o as MenuItem;
                        if (mi != null && !items.Contains(mi)) {
                            items.Add(mi);
                            mi.Click += new RoutedEventHandler(ReloadFiles);
                        }
                    }
                    if (this.panel.Children.Count == 0) {
                        ShowStatus("Graph is empty");
                    } else {
                        double ratio = GetAspectRatio();
                        foreach (UIElement c in this.panel.Children) {
                            GraphCanvas graph = c as GraphCanvas;
                            if (graph != null) {
                                graph.AspectRatio = ratio;
                                graph.AddHandler(GraphCanvas.LayoutStartEvent, new RoutedEventHandler(OnLayoutStart));
                                graph.AddHandler(GraphCanvas.LayoutEndEvent, new RoutedEventHandler(OnLayoutEnd));
                            }
                        }                        
                    }
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, "Create Graph Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowStatus("");
                }
            }
        }

        double GetAspectRatio() {
            if (settings.HasValue("AspectRatio") && (bool)settings["AspectRatio"]) {
                return scroller.ActualWidth / scroller.ActualHeight;
            }
            return 0;
        }

        public void InvalidateGraphLayout() {
            if (this.panel == null) return;
            if (this.zoom != null) this.zoom.ResetTranslate();
            CancelLayout();
            double ratio = GetAspectRatio();
            foreach (UIElement e in this.panel.Children) {
                GraphCanvas graph = e as GraphCanvas;
                if (graph != null) {
                    graph.AspectRatio = ratio; // but NOT the subgraphs!
                    graph.Direction = this.direction;
                    graph.OnGraphChanged();
                    foreach (object o in graph.GraphElements) {
                        SubgraphShape s = o as SubgraphShape;
                        if (s != null) {
                            graph = s.Canvas;
                             if (graph != null) {
                                 graph.Direction = this.direction;
                                 graph.OnGraphChanged();
                             }
                        }
                    }
                }
            }
        }

        int insideSelectionChanged = 0; // only scroll into view the top level objects that are selected, and not the cascading objects.

        void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            insideSelectionChanged++;
            try
            {
                foreach (ISelectable s in e.AddedItems)
                {
                    NodeShape ns = s as NodeShape;
                    if (ns != null)
                    {
                        SelectEdges(ns, s.Selected);
                        if (insideSelectionChanged == 1)
                        {
                            zoom.ScrollIntoView(ns);
                        }
                        break;
                    }
                    EdgeShape es = s as EdgeShape;
                    if (es != null)
                    {
                        INode src = es.Edge.Source;
                        INode target = es.Edge.Target;
                        if (src != null && target != null)
                        {
                            NodeShape a = src.UserData as NodeShape;
                            NodeShape b = target.UserData as NodeShape;
                            if (insideSelectionChanged == 1)
                            {
                                zoom.ScrollIntoView(new FrameworkElement[] { a, b });
                            }
                        }
                        break;
                    }
                }
                foreach (ISelectable s in e.RemovedItems)
                {
                    NodeShape ns = s as NodeShape;
                    if (ns != null)
                    {
                        SelectEdges(ns, s.Selected);
                    }
                }
            }
            finally
            {
                insideSelectionChanged--;
            }
        }

        public void SelectEdges(NodeShape ns, bool selected) {
            if (ns == null) return;
            GraphNode n = ns.Tag as GraphNode;
            if (n == null) return;

            foreach (UIElement g in this.panel.Children) {
                GraphCanvas graph = g as GraphCanvas;
                if (graph != null) {
                    foreach (UIElement e in graph.GraphElements) {
                        if (e is EdgeShape) {
                            EdgeShape es = (EdgeShape)e;
                            GraphEdge edge = es.Tag as GraphEdge;
                            if (edge != null && (edge.Source == n || edge.Target == n)) {
                                if (selected) {
                                    context.AddSelection(es);
                                } else {
                                    context.RemoveSelection(es);
                                }
                            } 
                        }
                    }
                }
            }
        }

        void OnFocusChanged(object sender, KeyboardFocusChangedEventArgs e) {
            NodeShape n = Keyboard.FocusedElement as NodeShape;
            if (n != null) {
                zoom.ScrollIntoView(n);
            }
            GraphCanvas c = Keyboard.FocusedElement as GraphCanvas;
            if (c != null) {
                //zoom.ScrollIntoView(c);
            }
            this.CopyMenu.IsEnabled = (n != null);
            this.CutMenu.IsEnabled = (n != null);
        }

        int pending = 0;
        void OnLayoutStart(object sender, RoutedEventArgs e) {
            pending++;
            if (pending == 1) {
                
                int graphs = 0;
                int nodes = 0;
                int edges = 0;

                foreach (UIElement c in this.panel.Children) {
                    GraphCanvas graph = c as GraphCanvas;
                    if (graph != null)
                    {
                        graphs++;
                        nodes += graph.Nodes.Count;
                        edges += graph.Edges.Count;
                        foreach (GraphNode n in graph.Nodes)
                        {
                            if (n.Nodes != null && n.Nodes.Count > 0)
                            {
                                nodes += n.Nodes.Count;
                                edges += n.Edges.Count;
                            }
                        }
                    }
                }
                ShowStatus(string.Format("{0} graphs, {1} nodes, {2} edges...", graphs, nodes, edges));

                this.CancelMenu.IsEnabled = true;
                this.Clock.Visibility = Visibility.Visible;
                this.RaiseEvent(new RoutedEventArgs(Window1.LayoutPendingEvent));
            }
        }

        void OnLayoutEnd(object sender, RoutedEventArgs e) {
            pending--;
            if (pending == 0) {
                LayoutComplete();
            }
        }

        private void LayoutComplete() {
            this.pending = 0;
            this.CancelMenu.IsEnabled = false;
            this.Clock.Visibility = Visibility.Hidden;
            this.RaiseEvent(new RoutedEventArgs(Window1.LayoutCompleteEvent));
            FixWrapPanelSize();            
        }

        void FixWrapPanelSize() {
            Rect bounds = new Rect();
            foreach (UIElement e in this.panel.Children) {
                GraphCanvas graph = e as GraphCanvas;
                if (graph != null) {
                    Rect r = new Rect(0, 0, graph.Width, graph.Height);
                    if (!double.IsNaN(r.Width)) {
                        bounds.Union(r);
                    }
                }
            }
            double w = bounds.Width;
            double h = bounds.Height;
            this.panel.Width = Math.Max(w, this.RenderSize.Width / zoom.Zoom);
            this.panel.InvalidateMeasure();
            this.panel.InvalidateArrange();
            this.panel.InvalidateVisual();

            this.Help.Width = this.RenderSize.Width;            
        }
        
        void RefreshLayout() {
            FixWrapPanelSize();
        }

        void TopToBottom(object sender, RoutedEventArgs e) {
            ChangeDirection(GraphDirection.TopToBottom);
        }
        void BottomToTop(object sender, RoutedEventArgs e) {
            ChangeDirection(GraphDirection.BottomToTop);
        }
        void LeftToRight(object sender, RoutedEventArgs e) {
            ChangeDirection(GraphDirection.LeftToRight);
        }

        void RightToLeft(object sender, RoutedEventArgs e) {
            ChangeDirection(GraphDirection.RightToLeft);
        }

        void CancelLayout(object sender, RoutedEventArgs e) {
            CancelLayout();
        }

        void CancelLayout() {
            foreach (UIElement c  in this.panel.Children) {
                GraphCanvas graph = c as GraphCanvas;
                if (graph != null) {
                    graph.Cancel();

                    foreach (object o in graph.GraphElements) {
                        SubgraphShape s = o as SubgraphShape;
                        if (s != null) {
                            graph = s.Canvas;
                            if (graph != null) {
                                graph.Cancel();
                            }
                        }
                    }
                }
            }
            LayoutComplete();            
        }

        void Check(string menu, bool check) {
            MenuItem item = (MenuItem)this.FindName(menu);
            item.IsChecked = check;
        }
        
        void ChangeDirection(GraphDirection dir) {
            if (this.direction != dir) SaveState();
            CheckDirectionMenu(dir);
            
            if (gen != null) {                
                gen.Direction = this.direction;
                InvalidateGraphLayout();
                ShowHelp(false);
            } else {
                ReloadFiles(this, new RoutedEventArgs());
            }
        }

        private void CheckDirectionMenu(GraphDirection dir) {
            Check(this.direction.ToString() + "Menu", false);
            this.direction = dir;
            Check(dir.ToString() + "Menu", true);
        }

        void NewFile(object sender, RoutedEventArgs e) {
            ClearPanel();
            fileNames = null;
        }

        void ClearPanel() {
            this.context.OnReloaded();            
            this.panel.Children.Clear();
            this.panel.Width = double.NaN;
            this.panel.Height = double.NaN;
        }

        void OnZoomMenu(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            string tag = (string)item.Tag;
            if (tag == "Fit")
            {
                ZoomToFit();
            }
            else
            {
                double factor = double.Parse(tag) / 100;
                this.zoom.Zoom = factor;
            }
        }

        void ZoomToFit()
        {
            Rect bounds = new Rect(0,0,0,0);
            foreach (UIElement c in this.panel.Children)
            {
                GraphCanvas graph = c as GraphCanvas;
                if (graph != null && graph.Width != 0 && !double.IsNaN(graph.Width) && graph.Height != 0 && !double.IsNaN(graph.Height))
                {
                    Rect graphBounds = new Rect(0, 0, graph.Width, graph.Height);
                    graphBounds = graph.TransformToAncestor(panel).TransformBounds(graphBounds);
                    bounds = Rect.Union(bounds, graphBounds);
                }
            }
            this.zoom.ZoomToRect(new Rect(0, 0, bounds.Width, bounds.Height));            
        }    

        void OpenFile(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            string filter  = "All Files (*.*)|*.*";
            string filters = Plugins.Filters;
            if (!string.IsNullOrEmpty(filters)) filter += "|" + filters;
            openFileDialog.Filter = filter;
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog(this) == true) {
                if (gen != null) SaveState();
                ProcessFiles(openFileDialog.FileNames);
            }
        }

        void ReloadFiles(object sender, RoutedEventArgs e) {
            if (!e.Handled) {
                CancelLayout(this, new RoutedEventArgs());
                if (fileNames != null && fileNames.Length > 0) {
                    ProcessFiles(fileNames);
                }
            }
        }

        void SaveFile(object sender, RoutedEventArgs e) {
            DgmlGenerator.SaveGraph(gen, this.source);
        }

        void SaveGraph(string filename) {
            this.source = filename;
            if (gen == null) {
                MessageBox.Show("You cannot save a .dgml file till you actually load a graph", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            } else {
                string ext = System.IO.Path.GetExtension(filename);
                if (ext.Equals(".dot", StringComparison.OrdinalIgnoreCase))
                {
                    DotGenerator.SaveGraph(gen, this.source);
                }
                else 
                {
                    DgmlGenerator.SaveGraph(gen, this.source);
                }
                SaveMenu.IsEnabled = true;
            }
        }

        void SaveAsFile(object sender, RoutedEventArgs e) {
            SaveAs();
        }

        public void SaveAs() {
            if (gen == null)
            {
                MessageBox.Show("You cannot save a .dgml file till you actually load a graph", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            } 
            Microsoft.Win32.SaveFileDialog fd = new Microsoft.Win32.SaveFileDialog();
            fd.RestoreDirectory = true;
            fd.Filter = "All Files (*.*)|*.*|" +
                        "Bitmap (*.bmp)|*.bmp|" +
                        "GIF (*.gif)|*.gif|" +
                        "Portable Network Graphics (*.png)|*.png|" +
                        "JPEG (*.jpg)|*.jpg|" +
                        "DOT (*.dot)|*.dot|" +
                        "DGML (*.dgml)|*.dgml|" +
                        "XAML (*.xaml)|*.xaml|" +
                        "XPS (*.xps)|*.xps"; 
                        

            if (fd.ShowDialog() == true) {
                string ext = System.IO.Path.GetExtension(fd.FileName);
                if (ext.Equals(".dgml", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".dot", StringComparison.OrdinalIgnoreCase))
                {
                    SaveGraph(fd.FileName);
                } else {
                    Image.Save(this.panel, fd.FileName);
                }
            }
        }

        void Cut(object sender, RoutedEventArgs e) {
            // need a clipboard format for our nodes...
            //Clipboard.SetText(ns.Model.Label);
            SaveState();
            gen.HideAll(context.SelectedNodes);
            this.CreateAndLayoutGraph(this, e);
        }

        void Copy(object sender, RoutedEventArgs e) {            
            NodeShape ns = Keyboard.FocusedElement as NodeShape;
            if (ns != null) {
                Clipboard.Clear();
                Clipboard.SetText(ns.Model.Label);
            }
        }

        void CopyImage(object sender, RoutedEventArgs e) {
            try {
                Clipboard.Clear(); 
                Clipboard.SetImage(Image.GetVisibleBitmap(panel));
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Imaging Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void CopyUnscaled(object sender, RoutedEventArgs e) {
            try {
                Clipboard.Clear();
                Clipboard.SetImage(Image.GetBitmap(panel));
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Imaging Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void Paste(object sender, RoutedEventArgs e) {
            MessageBox.Show("Not implemented");
        }

        void PageSetup(object sender, RoutedEventArgs e) {            
            MessageBox.Show("Not implemented");
        }
        void Print(object sender, RoutedEventArgs e) {
            PrintDialog dlg = new PrintDialog();
            if (dlg.ShowDialog() == true) {
                Image.Print(dlg, this.panel, gen.Label);
            }
        }
        void PrintPreview(object sender, RoutedEventArgs e) {
            MessageBox.Show("Not implemented");            
        }

        void SetTheme(object sender, RoutedEventArgs e) {
            MenuItem item = e.OriginalSource as MenuItem;
            settings["Skin"] = (string)item.Tag;
        }

        void ApplySkin(string name) {
            ResourceDictionary newTheme = Themes.GetTheme(name);
            if (newTheme != null)
            {
                if (theme != null)
                {
                    this.Resources.MergedDictionaries.Remove(theme);
                }
                this.Resources.MergedDictionaries.Add(newTheme);
                theme = newTheme;
            }
            foreach (MenuItem item in ThemeMenu.Items)
            {
                item.IsChecked = ((string)item.Tag == name);
            }
            InvalidateGraphLayout();
            InvalidateVisual();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            switch (e.Key) {
                case Key.Space:
                    ISelectable s = Keyboard.FocusedElement as ISelectable;
                    if (s != null) {
                        ToggleSelection(s);
                        e.Handled = true;
                    }
                    break;
                case Key.F3:
                    if (searcher != null) {
                        searcher.FindNext();
                    }
                    break;
            }
            base.OnKeyDown(e);
        }

        private void ToggleSelection(ISelectable s) {
            if (s.Selected) {
                context.RemoveSelection(s);
            } else {
                context.AddSelection(s);
            }
        }

        ISelectable downShape;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            downShape = FindSelectable(e.OriginalSource);            
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                ISelectable s = FindSelectable(e.OriginalSource);
                if (s == downShape)
                {
                    if (s != null && !s.Selected)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                        {
                            context.ClearSelection();
                        }
                        ToggleSelection(s);
                        e.Handled = true;
                    }
                    else
                    {
                        context.ClearSelection();
                    }
                    UIElement ue = s as UIElement;
                    if (ue != null)
                    {
                        ue.Focus();
                    }
                }
            }            
            base.OnMouseLeftButtonUp(e);
        }

        ISelectable FindSelectable(object o) {
            ISelectable s = o as ISelectable;
            if (s != null) return s;
            FrameworkElement e = o as FrameworkElement;
            if (e != null) {
                if (e.Parent != null) {
                    return FindSelectable(e.Parent);
                }
                if (e.TemplatedParent != null) {
                    return FindSelectable(e.TemplatedParent);
                }
            }
            
            return null;
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e) {
            if (this.gen != null) {
                NodeShape ns = FindSelectable(e.OriginalSource) as NodeShape;
                gen.SetContextMenuState(ns != null ? ns.Model : null);
            }
            base.OnMouseRightButtonDown(e);
        }

        void OnBackButton(object sender, RoutedEventArgs e) {
            if (!history.CanGoForward && (currentState == null || history.Peek() != currentState)) {
                // then save the current state so we can go back to it!
                SaveState();
                history.GoBack();
            }
            currentState = history.GoBack();
            LoadState(currentState);
        }

        ViewState currentState = null;

        void OnForwardButton(object sender, RoutedEventArgs e) {
            currentState = history.GoForward();
            LoadState(currentState);
        }


        void SaveState() {
            ViewState state = new ViewState();
            state.SetValue("direction", this.direction);
            state.SetValue("fileNames", this.fileNames);
            if (gen != null) {
                gen.SaveState(state);
            }
            history.PushState(state);
            currentState = null;
        }

        void LoadState(ViewState state) {
            CancelLayout(this, new RoutedEventArgs());
            this.direction = (GraphDirection)state["direction"];
            string[] fileNames = (string[])state["fileNames"];
            if (fileNames != null && fileNames.Length > 0) {
                ProcessFiles(fileNames);
            }
            if (gen != null) {
                gen.LoadState(state);
                CreateAndLayoutGraph(this, EventArgs.Empty);
            }
        }

        void LinkClicked(object sender, RoutedEventArgs e) {
            Hyperlink link = sender as Hyperlink;
            if (link != null) {
                Uri uri = link.NavigateUri;
                if (!uri.IsAbsoluteUri && uri.OriginalString.StartsWith("#")) {
                    System.Windows.Input.NavigationCommands.GoToPage.Execute(link.Tag, this.Help);
                } else {
                    Navigate(uri);
                }
            }
        }


        void Navigate(Uri location) {
            const int SW_SHOWNORMAL = 1;
            string path = location.IsFile ? location.LocalPath : location.AbsoluteUri;
            int rc = ShellExecute(IntPtr.Zero, "open", path, null, Directory.GetCurrentDirectory(), SW_SHOWNORMAL);
        }

        [DllImport("Shell32.dll", EntryPoint = "ShellExecuteW",
                SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true,
                CallingConvention = CallingConvention.StdCall)]
        static extern int ShellExecute(IntPtr handle, string verb, string file,
                            string args, string dir, int show);

    }

}
