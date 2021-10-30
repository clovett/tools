using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Automation;

namespace DependencyViewer {
    public class NodeShape : ContentControl, ISelectable, IHasGraphNode {
        GraphCanvas diagram;
        INode node;
        bool selected;
        string type;

        public static readonly System.Windows.DependencyProperty NodeTypeProperty =
          DependencyProperty.Register("NodeType", typeof(string), typeof(NodeShape));
        public static readonly System.Windows.DependencyProperty SelectedProperty =
          DependencyProperty.Register("Selected", typeof(bool), typeof(NodeShape));
        public static readonly System.Windows.DependencyProperty LabelProperty =
          DependencyProperty.Register("Label", typeof(string), typeof(NodeShape));

        public EventHandler SelectionChanged;

        public NodeShape() {
        }

        public NodeShape(GraphCanvas diagram, GraphNode model, INode gn, TextBlock label) {
            this.diagram = diagram;
            this.node = gn;

            if (label != null) label.TextAlignment = TextAlignment.Center;
            this.Content = label;

            AutomationProperties.SetAutomationId(this, model.Id);
            this.Tag = model;
            this.Label = label.Text;
        }

        // in untransformed Graph coordinates.
        public Rect NodeBounds {
            get {
                Point center = this.NodeCenter;
                double w = node.Width;
                double h = node.Height;
                return new Rect(center.X - (w / 2), center.Y - (h / 2), w, h);
            }
        }

        // in untransformed Graph coordinates.
        public System.Windows.Point NodeCenter {
            get { return node.Center; }
        }

        public INode Node {
            get { return this.node; }
        }

        public GraphNode Model {
            get { return this.Tag as GraphNode; }
            set { this.Tag = value; }
        }

        public string Label {
            get { return (string)GetValue(LabelProperty);  }
            set { SetValue(LabelProperty, value); }
        }

        public string NodeType {
            get { return type; }
            set { type = value; }
        }

        public bool Selected {
            get { return this.selected; }
            set {
                if (this.selected != value) {
                    this.selected = value;
                    this.SetValue(SelectedProperty, value);
                    if (SelectionChanged != null)
                        SelectionChanged(this, EventArgs.Empty);
                    InvalidateVisual();
                }
                //if (value) this.Focus();
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            if (e.Property == NodeTypeProperty) {
                NodeType = (string)e.NewValue;
            } else if (e.Property == SelectedProperty) {
                Selected = (bool)e.NewValue;
            } else if (e.Property == LabelProperty) {
                Label = (string)e.NewValue;
            }
            base.OnPropertyChanged(e);
        }

        protected Size s;
        protected override Size MeasureOverride(Size constraint) {
            // Save the desired size
            s = base.MeasureOverride(constraint);
            return s;
        }

        public virtual Geometry GetVisualGeometry() {
            this.InvalidateMeasure();
            this.InvalidateArrange();
            this.UpdateLayout();

            double width = s.Width;
            double height = s.Height;
            if (width == 0) width = 80;
            if (height == 0) height = 24;

            Visual v = FindDrawingVisual(this);
            if (v != null) {
                DrawingGroup dg = VisualTreeHelper.GetDrawing(v);
                Debug.Assert(dg != null);
                foreach (Drawing d in dg.Children) {
                    GeometryDrawing g = d as GeometryDrawing;
                    if (g != null) {
                        return g.Geometry;
                    }
                }
            }
            return new RectangleGeometry(new Rect(0,0,width,height),3,3);
        }

        Visual FindDrawingVisual(Visual parent) {
            // Find non-transparent visual.
            for (int i = 0, n = VisualTreeHelper.GetChildrenCount(parent); i < n; i++) {
                Visual v = VisualTreeHelper.GetChild(parent, i) as Visual;
                if (v != null) {
                    DrawingGroup dg = VisualTreeHelper.GetDrawing(v);
                    if (dg != null) {
                        return v;
                    }
                }
                v = FindDrawingVisual(v);
                if (v != null) return v;
            }
            return null;
        }
    }
}