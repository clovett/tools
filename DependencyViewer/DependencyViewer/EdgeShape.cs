using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows;

namespace DependencyViewer {

    public class EdgeShape : Shape, ISelectable, IHasGraphEdge {

        IEdge edge;
        TextBlock label;
        Brush oldStroke;
        bool selected;
        bool highlighted;

        public event EventHandler HighlightChanged;
        public event EventHandler SelectionChanged;

        public static readonly System.Windows.DependencyProperty SelectedProperty =
          DependencyProperty.Register("Selected", typeof(bool), typeof(EdgeShape));
        public static readonly System.Windows.DependencyProperty EdgeTypeProperty =
              DependencyProperty.Register("EdgeType", typeof(string), typeof(EdgeShape));
        public static readonly System.Windows.DependencyProperty SelectedStrokeProperty =
              System.Windows.DependencyProperty.Register("SelectedStroke", typeof(Brush), typeof(EdgeShape));
        public static readonly System.Windows.DependencyProperty HighlightedStrokeProperty =
              System.Windows.DependencyProperty.Register("HighlightedStroke", typeof(Brush), typeof(EdgeShape));
        public static readonly System.Windows.DependencyProperty ArrowHeadSizeProperty =
                System.Windows.DependencyProperty.Register("ArrowHeadSize", typeof(double), typeof(EdgeShape));

        public EdgeShape() {
            this.Focusable = false;
        }

        internal EdgeShape(IEdge e, GraphEdge model, TextBlock label) {
            this.Focusable = false;

            this.Model = model;
            edge = e;
            this.label = label;
            if (model.BiDirectional)
            {
                EdgeType = "Circular";
            }
            else
            {
                EdgeType = model.EdgeType;
            }
        }

        public string EdgeType {
            get { return (string)GetValue(EdgeTypeProperty); }
            set { SetValue(EdgeTypeProperty, value); }
        }

        public double ArrowHeadSize
        {
            get { return (double)GetValue(ArrowHeadSizeProperty); }
            set { SetValue(ArrowHeadSizeProperty, value); }
        }

        internal IEdge Edge { 
            get { return this.edge; } 
            set { this.edge = value; } 
        }

        public GraphEdge Model {
            get { return this.Tag as GraphEdge; }
            set { this.Tag = value; }
        }

        public TextBlock Label {
            get { return label; }
            set { label = value; }
        }

        public Brush SelectedStroke {
            get { return GetValue(SelectedStrokeProperty) as Brush; }
            set { SetValue(SelectedStrokeProperty, value); }
        }

        public Brush HighlightedStroke {
            get { return GetValue(HighlightedStrokeProperty) as Brush; }
            set { SetValue(HighlightedStrokeProperty, value); }
        }


        public System.Windows.Rect Bounds {
            get {
                return this.DefiningGeometry.Bounds;
            }
        }
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e) {
            Highlighted = true;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e) {
            Highlighted = false;
            base.OnMouseLeave(e);
        }

        public bool Selected {
            get { return selected; }
            set {
                if (selected != value) {
                    selected = value;
                    this.SetValue(SelectedProperty, value);
                    GetStrokeFill();
                    if (SelectionChanged != null) {
                        SelectionChanged(this, new EventArgs());
                    }
                    InvalidateVisual();
                }
            }
        }

        public bool Highlighted {
            get { return highlighted; }
            set {
                highlighted = value;
                GetStrokeFill();
                if (HighlightChanged != null) {
                    HighlightChanged(this, new EventArgs());
                }
            }
        }

        static Brush selectedStroke = new SolidColorBrush(Colors.Blue);
        static Brush highlightedStroke = new SolidColorBrush(Colors.Red);

        protected void GetStrokeFill() {
            Brush fill = null;
            Brush stroke = null;
            if (oldStroke == null) {
                oldStroke = this.Stroke;
            }
            Brush labelBrush = this.GetValue(Control.ForegroundProperty) as Brush;
            if (labelBrush != null && label != null) {
                label.Foreground = labelBrush;
            }
            if (Highlighted) {
                stroke = HighlightedStroke;
                if (stroke == null) stroke = highlightedStroke;
                fill = stroke;
            } else if (Selected) {
                stroke = SelectedStroke;
                if (stroke == null) stroke = selectedStroke;
                fill = stroke;
            } else {
                stroke = oldStroke;
                if (stroke == null) stroke = Brushes.Black;
                fill = stroke;
            }
            this.Fill = fill;
            this.Stroke = stroke;
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext) {
            if (this.Fill == null) {
                GetStrokeFill();
            }
            base.OnRender(drawingContext);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            if (e.Property == SelectedProperty) {
                this.Selected = (bool)e.NewValue;
            } else if (e.Property == EdgeTypeProperty) {
                this.EdgeType = (string)e.NewValue;
            } else if (e.Property == SelectedStrokeProperty) {
                this.SelectedStroke = (Brush)e.NewValue;
            } else if (e.Property == HighlightedStrokeProperty) {
                this.HighlightedStroke = (Brush)e.NewValue;
            } else if (e.Property == ArrowHeadSizeProperty) {
                this.ArrowHeadSize = (double)e.NewValue;
            }
            base.OnPropertyChanged(e);
        }

        protected override Geometry DefiningGeometry {
            get {
                edge.Thickness = this.StrokeThickness;
                object arrowSize = GetValue(ArrowHeadSizeProperty);
                if (arrowSize != null)
                {
                    double v = (double)arrowSize;
                    if (v != 0)
                    {
                        edge.ArrowHeadSize = (double)arrowSize;
                    }
                }
                return edge.Line;
            }
        }

    }
}