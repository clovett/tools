using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using ToonBuilder.ColorPicker;
using ToonBuilder.Model;

namespace ToonBuilder
{
    /// <summary>
    /// Interaction logic for SceneEditor.xaml
    /// </summary>
    public partial class SceneEditor : UserControl
    {
        Scene scene;
        Actor current;
        List<Path> selection = new List<Path>();
        List<Resizer> resizers = new List<Resizer>();

        public readonly static RoutedUICommand CommandFill;
        public readonly static RoutedUICommand CommandBringToFront;
        public readonly static RoutedUICommand CommandSendToBack;
        public readonly static RoutedUICommand CommandBringForward;
        public readonly static RoutedUICommand CommandSendBackward;

        static SceneEditor()
        {
            CommandFill = new RoutedUICommand("Fill", "Fill", typeof(MainWindow));
            CommandBringToFront = new RoutedUICommand("CommandBringToFront", "CommandBringToFront", typeof(MainWindow));
            CommandSendToBack = new RoutedUICommand("CommandSendToBack", "CommandSendToBack", typeof(MainWindow));
            CommandBringForward = new RoutedUICommand("CommandBringForward", "CommandBringForward", typeof(MainWindow));
            CommandSendBackward = new RoutedUICommand("CommandSendBackward", "CommandSendBackward", typeof(MainWindow));
        }

        public SceneEditor()
        {
            InitializeComponent();
            CurveEditor.Finished += new EventHandler(OnCurveFinished);
            scene = new Scene();
            current = new Actor();
            scene.Actors.Add(current);
        }

        void OnCurveFinished(object sender, EventArgs e)
        {
            AddShape(CurveEditor.GetFinishedPath());
        }

        public Scene Scene { get { return this.scene; } }

        public void AddShape(Path shape)
        {
            if (shape != null)
            {
                EditorCanvas.Children.Add(shape);
            }
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            UIElement child = FindChildAt(e.GetPosition(this));
            if (child is Resizer)
            {
                Resizer resizer = (Resizer)child;
                resizer.HandleMouseLeftButtonDown(e);
                CurveEditor.HideMenu();
                return; // don't steel mouse events from the resizer.
            }

            Path shape = child as Path;
            if (shape != null)
            {
                CurveEditor.HideMenu();
                e.Handled = true;
                Resizer resizer = null;
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (selection.Contains(shape))
                    {
                        Deselect(shape);
                    }
                    else
                    {
                        resizer = Select(shape);
                    }
                }
                else
                {
                    ClearSelection();
                    resizer = Select(shape);
                }
                if (resizer != null)
                {
                    // so that a single drag can select and move the resizer
                    resizer.UpdateLayout();
                    resizer.HandleMouseLeftButtonDown(e);
                }
            }
            else
            {
                ClearSelection(); 
            }

            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected UIElement FindChildAt(Point pos)
        {
            UIElement hit = null;
            
            VisualTreeHelper.HitTest(EditorCanvas, null, new HitTestResultCallback((result) =>
            {
                DependencyObject found = result.VisualHit;
                while (found != null && found != this)
                {
                    hit = found as Resizer;
                    if (hit != null)
                    {                        
                        return HitTestResultBehavior.Stop;
                    }
                    hit = found as Path;
                    if (hit != null)
                    {
                        return HitTestResultBehavior.Stop;
                    }
                    found = VisualTreeHelper.GetParent(found);
                }

                return HitTestResultBehavior.Continue;
            }), new GeometryHitTestParameters(new EllipseGeometry(pos, 1, 1)));

            return hit;
        }

        public Resizer Select(Path shape)
        {
            selection.Add(shape);
            Rect bounds = VisualTreeHelper.GetDescendantBounds(shape);
            if (shape.RenderTransform != null)
            {
                bounds = shape.RenderTransform.TransformBounds(bounds);
            }
            var resizer = new Resizer(bounds);
            resizers.Add(resizer);
            resizer.Resized += new EventHandler(OnResized);
            EditorCanvas.Children.Add(resizer);
            OnSelectionChanged();
            return resizer;
        }

        void OnResized(object sender, EventArgs e)
        {
            Resizer r = (Resizer)sender;
            int i = resizers.IndexOf(r);
            Path shape = selection[i];
            shape.RenderTransform = null;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(shape);
            Rect target = r.Bounds;

            // transform the shape so it matches the resizer bounds.
            TransformGroup g = new TransformGroup();
            g.Children.Add(new TranslateTransform(target.X - bounds.X, target.Y - bounds.Y));
            g.Children.Add(new ScaleTransform(target.Width / bounds.Width, target.Height / bounds.Height, target.X, target.Y));
            shape.RenderTransform = g;
            
        }

        private void ClearSelection()
        {
            while (selection.Count > 0)
            {
                Deselect(selection[0]);
            }
            OnSelectionChanged();
        }

        internal void DeleteSelection()
        {
            foreach (Path p in selection)
            {
                EditorCanvas.Children.Remove(p);
            }
            ClearSelection();
            OnSelectionChanged();
        }

        internal void CopySelection()
        {
            Rect union = Rect.Empty;
            foreach (Path p in selection)
            {
                Rect bounds = p.Data.Bounds;
                bounds.Inflate(p.StrokeThickness, p.StrokeThickness);
                if (union == Rect.Empty)
                {
                    union = bounds;
                }
                else
                {
                    union = Rect.Union(union, bounds);
                }
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)union.Right, (int)union.Bottom, 96, 96, PixelFormats.Pbgra32);
            bitmap.Clear();
            foreach (Path p in selection)
            {
                bitmap.Render(p);
                p.InvalidateArrange();
            }

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            var ms = new System.IO.MemoryStream();
            encoder.Save(ms);

            IDataObject data = new DataObject();
            data.SetData("PNG", ms);
            Clipboard.Clear();
            Clipboard.SetDataObject(data, true);
        }

        private void Deselect(Path shape)
        {
            int i = selection.IndexOf(shape);
            if (i >= 0)
            {
                selection.RemoveAt(i);
                Resizer r = resizers[i];
                resizers.RemoveAt(i);
                EditorCanvas.Children.Remove(r);
                OnSelectionChanged();
            }
        }

        public IList<Path> Selection { get { return selection; } }

        public event EventHandler SelectionChanged;

        private void OnSelectionChanged()
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(this, EventArgs.Empty);
            }
        }

        ColorPickerWindow picker;

        private void OnFill(object sender, ExecutedRoutedEventArgs args)
        {
            if (picker != null)
            {
                picker.Show();
                return;
            }
            picker = new ColorPickerWindow();
            picker.Topmost = true;
            picker.Show();
            picker.ColorChanged += new EventHandler<ColorChangedEventArgs>((s, e) =>
            {
                foreach (Path p in this.Selection)
                {
                    p.Fill = new SolidColorBrush(e.NewColor);
                }
            });
            picker.Closed += new EventHandler((s, e) =>
            {
                picker = null;
                this.Focus();
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void HasSelectedShape(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.Selection.Count > 0;
        }

        List<Path> SaveSelection()
        {
            List<Path> saved = new List<Path>(selection);
            ClearSelection(); // have to remove resizers for this to work.
            return saved;
        }

        void Select(List<Path> list)
        {
            foreach (Path p in list)
            {
                Select(p);
            }
        }

        private void OnBringToFront(object sender, ExecutedRoutedEventArgs e)
        {
            List<Path> saved = SaveSelection();
            UIElementCollection children = this.EditorCanvas.Children;
            foreach (Path p in saved)
            {
                children.Remove(p);
            } 
            foreach (Path p in saved)
            {
                children.Add(p);
            }
            Select(saved);
        }

        private void OnSendToBack(object sender, ExecutedRoutedEventArgs e)
        {
            List<Path> saved = SaveSelection();
            UIElementCollection children = this.EditorCanvas.Children;
            foreach (Path p in saved)
            {
                children.Remove(p);
            } 
            foreach (Path p in saved)
            {
                children.Insert(0, p);
            }
            Select(saved);
        }

        private void OnBringForward(object sender, ExecutedRoutedEventArgs e)
        {
            List<Path> saved = SaveSelection();
            UIElementCollection children = this.EditorCanvas.Children;
            foreach (Path p in saved)
            {
                int i = children.IndexOf(p);
                children.Remove(p);
                // find something we can move in front of.
                int j = i;
                for (; j < children.Count; j++)
                {
                    Path child = children[j] as Path;
                    if (child != null && !saved.Contains(child))
                    {
                        break;
                    }
                }
                j++;
                if (j > children.Count)
                {
                    children.Add(p);
                }
                else
                {
                    children.Insert(j, p);
                }
            }
            Select(saved);
        }

        private void OnSendBackward(object sender, ExecutedRoutedEventArgs e)
        {
            List<Path> saved = SaveSelection();
            UIElementCollection children = this.EditorCanvas.Children;
            foreach (Path p in saved)
            {
                int i = children.IndexOf(p);
                children.Remove(p);
                // find something we can move behind.
                int j = i - 1;
                for (; j >= 0; j--)
                {
                    Path child = children[j] as Path;
                    if (child != null && !saved.Contains(child))
                    {
                        break;
                    }
                }
                if (j < 0) j = 0;
                children.Insert(j, p);
            }
            Select(saved);

        }


        internal void Clear()
        {
            this.EditorCanvas.Children.Clear();
            this.CurveEditor.Clear();
        }

        public static XNamespace XamlPresentation = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        internal XDocument ToXaml()
        {
            XDocument doc = new XDocument(new XElement(XamlPresentation + "Canvas"));
            using (var writer = doc.Root.CreateWriter())
            {
                foreach (UIElement e in EditorCanvas.Children)
                {
                    Path p = e as Path;
                    if (p != null)
                    {
                        System.Windows.Markup.XamlWriter.Save(p, writer);
                    }
                }
            }
            return doc;            
        }

        internal void PasteXaml(string xaml)
        {
            Clear();
            object e = System.Windows.Markup.XamlReader.Parse(xaml);
            Path p = e as Path;
            if (p == null)
            {
                if (e is Panel)
                {
                    Panel c = (Panel)e;
                    List<object> children = new List<object>();
                    foreach(var child in c.Children)
                    {
                        children.Add(child);
                    }

                    foreach (var child in children)
                    {
                        p = child as Path;
                        if (p != null)
                        {
                            c.Children.Remove(p);
                            EditorCanvas.Children.Add(p);
                        }
                    }
                }
            } 
            else
            {
                EditorCanvas.Children.Add(p);
            }
            if (p == null)
            {
                throw new Exception("Cannot find <Path> element in the XAML");
            }
        }

    }
}
