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
using Microsoft.VisualStudio.XmlEditor;

namespace Microsoft.SvgEditorPackage.View
{
    /// <summary>
    /// This control is designed to bind to an "XDocument" containing SVG data
    /// </summary>
    public partial class SvgPanel : UserControl
    {
        SvgDocument doc;
        SvgCanvas canvas; 
        HashSet<SvgElement> selection = new HashSet<SvgElement>();

        public SvgPanel(SvgDocument doc)
        {
            InitializeComponent();
            this.IsHitTestVisible = true;
            this.doc = doc;
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(OnDataContextChanged);
            Diagram.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(OnDiagramPreviewMouseLeftButtonDown);
            if (doc.Document != null)
            {
                this.DataContext = doc.Document;
            }
        }

        void OnDocReloaded(object sender, EventArgs e)
        {
            Bind();
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Bind();
        }

        void Bind()
        {
            XDocument doc = this.DataContext as XDocument;
            if (doc != null)
            {
                Diagram.Children.Clear();
                this.canvas = new SvgCanvas(doc);
                BuildTree(doc);
                Diagram.Children.Add(canvas);
                Adorners.Children.Clear();
                selection.Clear();
            }
        }

        internal void BuildTree(XDocument doc)
        {            
            foreach (XElement child in doc.Root.Elements())
            {
                SvgElement visual = CreateVisual(child);
                if (visual != null)
                {
                    canvas.Children.Add(visual);
                }
            }
        }

        private SvgElement CreateVisual(XElement child)
        {
            string name = child.Name.LocalName;
            switch (name)
            {
                case "rect":
                    return new SvgRect() { DataContext = child };
                case "path":
                    return new SvgPath() { DataContext = child };
                case "circle":
                    return new SvgCircle() { DataContext = child };
                case "ellipse":
                    return new SvgEllipse() { DataContext = child };
                case "line":
                    return new SvgLine() { DataContext = child };
                case "text":
                    return new SvgText() { DataContext = child };
            }
            return null;
        }

        // ok, a change has been made, perhaps outside our GUI designer, and in the XML editor, so now
        // we have to merge those changes back.
        internal void MergeChanges(IEnumerable<XmlModelChange> changes)
        {
            foreach (XmlModelChange mc in changes)
            {
                switch (mc.Action)
                {
                    case XObjectChange.Add:
                        OnAdd(mc.Node);
                        break;
                    case XObjectChange.Name:
                        OnNameChange(mc.Node);
                        break;
                    case XObjectChange.Remove:
                        OnRemove(mc.Node);
                        break;
                    case XObjectChange.Value:
                        OnChange(mc.Node);
                        break;
                    default:
                        break;
                }
            }
        }

        // now we have to insert this new child in the right place relative to other elements already in the canvas.
        void Insert(SvgElement visual)
        {
            XElement element = visual.Element;
            
            XDocument doc = this.DataContext as XDocument;
            if (element.Parent == doc.Root)
            {
                int i = 0;
                foreach (XElement child in doc.Root.Elements())
                {
                    if (child == element)
                    {
                        break;
                    }
                    else
                    {
                        SvgElement existing = FindElement(child);
                        if (existing != null)
                        {
                            i++;
                        }
                    }
                }
                canvas.Children.Insert(i, visual);
            }
        }

        public void OnAdd(XObject node)
        {
            XElement e = node as XElement;
            if (e != null)
            {
                SvgElement visual = CreateVisual(e);
                if (visual != null)
                {
                    Insert(visual);
                }
                return;
            }

            XAttribute a = node as XAttribute;
            if (a != null)
            {
                SvgElement svg = FindElement(a.Parent);
                if (svg != null)
                {
                    // rebind to force refresh of attribute values.
                    svg.DataContext = null;
                    svg.DataContext = a.Parent;
                }
                return;
            }

            XText text = node as XText;
            if (text != null)
            {
                SvgText svg = FindElement(text.Parent) as SvgText;
                if (svg != null)
                {
                    svg.InvalidateVisual();
                }
                return;
            }

            // we ignore comments, and whitespace and pi
        }

        void OnRemove(XObject node)
        {
            XElement e = node as XElement;
            if (e != null)
            {
                SvgElement svg = FindElement(e);
                if (svg != null)
                {
                    RemoveElement(svg);
                }
                return;
            }

            XAttribute a = node as XAttribute;
            if (a != null)
            {
                SvgElement svg = FindElement(a.Parent);
                if (svg != null)
                {
                    // rebind to force refresh of attribute values.
                    svg.DataContext = null;
                    svg.DataContext = a.Parent;
                }
                return;
            }

        }

        void OnChange(XObject node)
        {
            return;
        }

        void OnNameChange(XObject node)
        {
            // for us this means we have to switch SvgElement types, so we'll remove the old child and add a new one.
            OnRemove(node);
            OnAdd(node);
        }

        SvgElement FindElement(XElement node)
        {
            if (node == null) return null;
            foreach (SvgElement e in canvas.Children)
            {
                if (e.Element == node)
                {
                    return e;
                }
            }
            return null;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
        }

        void OnDiagramPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                // select shapes.
                SvgElement element = HitTest(e.GetPosition(this));
                if (element != null)
                {
                    bool add = (Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) != 0 ||
                                (Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) != 0;
                    if (!add)
                    {
                        if (selection.Count == 1 && selection.Contains(element))
                        {
                            // already correct
                        }
                        else
                        {
                            ClearSelection();
                        }
                    }
                    if (!selection.Contains(element))
                    {
                        selection.Add(element);
                        Adorners.Children.Add(new SvgResizer(element));
                    }
                }
                else
                {
                    ClearSelection();
                }
            }
        }

        private void RemoveElement(SvgElement e)
        {
            selection.Remove(e);
            canvas.Children.Remove(e);
            foreach (SvgResizer s in Adorners.Children)
            {
                if (s.Adorned == e)
                {
                    Adorners.Children.Remove(s);
                    break;
                }
            }
        }

        private void ClearSelection()
        {
            selection.Clear();
            Adorners.Children.Clear();
        }

        public ISet<SvgElement> Selection { get { return selection; } }

        public SvgElement HitTest(Point pos)
        {
            SvgElement item = null;
            RectangleGeometry hitRect = new RectangleGeometry(new Rect(pos.X, pos.Y, 1, 1));
            var callback = new HitTestResultCallback((r) =>
            {
                DependencyObject v = r.VisualHit;
                do
                {
                    Visibility visibility = (Visibility)v.GetValue(UIElement.VisibilityProperty);
                    if (visibility != Visibility.Visible)
                    {
                        return HitTestResultBehavior.Continue;
                    }

                    // We have to walk the parent chain because some of the parents might not be hit test visible.
                    item = v as SvgElement;
                    if (item == null)
                    {
                        v = VisualTreeHelper.GetParent(v);
                    }
                }
                while (v != null && v != this && item == null);

                if (item != null)
                {
                    return HitTestResultBehavior.Stop;
                }
                return HitTestResultBehavior.Continue;
            });

            VisualTreeHelper.HitTest(this, null, callback, new GeometryHitTestParameters(hitRect));
            return item;
        }

        internal void DeleteSelection()
        {
            foreach (SvgElement e in Selection.ToArray())
            {
                RemoveElement(e);
                e.Element.Remove();
            }
        }
    }
}
