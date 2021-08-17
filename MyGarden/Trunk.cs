using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyGarden
{
    class Trunk : FrameworkElement
    {
        Path p;
        bool dirty;

        public Trunk()
        {
            // A trunk is a simple spline contains anchor points where leaves attach.
            p = new Path();
            this.AddVisualChild(p);
        }

        // 0 <= v <= 1
        public Point GetPositionOnTrunk(double v)
        {
            PathGeometry g = (PathGeometry)p.Data;
            Point pt = new Point(0, 0);
            Point tangent;
            if (g != null && g.Figures.Count > 0)
            {
                PathFigure figure = g.Figures[0];
                if (figure.Segments.Count > 0)
                {
                    g.GetPointAtFractionLength(v * TrunkHeight, out pt, out tangent);
                }
            }
            return pt;
        }


        // the percentage of the trunk that is visible (0 to 1)
        public double TrunkHeight
        {
            get { return (double)GetValue(TrunkHeightProperty); }
            set { SetValue(TrunkHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrunkHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrunkHeightProperty =
            DependencyProperty.Register("TrunkHeight", typeof(double), typeof(Trunk), new PropertyMetadata(0.0, OnTrunkHeightChanged));

        private static void OnTrunkHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Trunk)d).UpdateClipRegion();
        }

        private void UpdateClipRegion()
        {

            Rect bounds = p.Data.Bounds;

            double top = bounds.Top + ((1 - TrunkHeight) * bounds.Height);

            double margin = this.StrokeThickness;
            bounds = new Rect(bounds.Left - margin, top - margin, bounds.Width + (2 * margin), bounds.Bottom - top + (2 * margin));
            this.Clip = new RectangleGeometry(bounds);
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            if (dirty)
            {
                CalcGeometry();
            }
            p.Measure(availableSize);
            return p.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            p.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return p.DesiredSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
            {
                return p;
            }
            return null;
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }

        private void CalcGeometry()
        {
            Point basePt = this.BasePosition;
            Point tipPt = this.TipPosition;
            Vector spline = tipPt - basePt;
            double length = spline.Length;

            Vector normalizedSpline = spline;
            normalizedSpline.Normalize();

            Vector normal = new Vector(-normalizedSpline.Y, normalizedSpline.X);
            if (double.IsNaN(normal.X)) 
            {
                return;
            }
            
            Point base1 = basePt + (normalizedSpline * 0.3333 * length);
            Point base2 = basePt + (normalizedSpline * 0.6666 * length);

            Vector spread = normalizedSpline * 0.1 * length;
            Point ctrlPt1 = basePt + spread;

            double curviness = this.Curviness;
            Point ctrlPt3 = base1 + (normal * curviness);
            Point ctrlPt2 = ctrlPt3 - spread;
            Point ctrlPt4 = ctrlPt3 + spread;

            Point ctrlPt6 = base2 - (normal * curviness);
            Point ctrlPt5 = ctrlPt6 - spread;
            Point ctrlPt7 = ctrlPt6 + spread;

            Point ctrlPt8 = tipPt - spread;

            PathGeometry g = (PathGeometry)p.Data;
            if (g == null)
            {
                g = new PathGeometry();
                p.Data = g;

                PathFigure figure = new PathFigure();
                g.Figures.Add(figure);

                PolyBezierSegment ps = new PolyBezierSegment();
                ps.IsStroked = true;
                figure.Segments.Add(ps);
            }

            PathFigure f = (g.Figures[0]);
            f.IsClosed = false;
            f.IsFilled = true;
            f.StartPoint = basePt;

            PolyBezierSegment poly = (PolyBezierSegment)f.Segments[0];

            if (double.IsNaN(ctrlPt1.X))
            {
                poly.Points = new PointCollection();
            }
            else
            {
                poly.Points = new PointCollection(new Point[] 
                {
                    ctrlPt1, ctrlPt2, ctrlPt3, ctrlPt4, ctrlPt5, ctrlPt6,
                    ctrlPt7, ctrlPt8, tipPt
                });
            }
            dirty = false;
        }


        private void UpdateGeometry()
        {
            dirty = true;
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        }

        public Path Path { get { return p; } }

        public double Curviness
        {
            get { return (double)GetValue(CurvinessProperty); }
            set { SetValue(CurvinessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Curviness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurvinessProperty =
            DependencyProperty.Register("Curviness", typeof(double), typeof(Trunk), new PropertyMetadata(0.25, OnCurvinessChanged));


        private static void OnCurvinessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Trunk)d).UpdateGeometry();
        }

        public Point BasePosition
        {
            get { return (Point)GetValue(BasePositionProperty); }
            set { SetValue(BasePositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BasePosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BasePositionProperty =
            DependencyProperty.Register("BasePosition", typeof(Point), typeof(Trunk), new PropertyMetadata(new Point(0, 0), OnBasePositionChanged));

        private static void OnBasePositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Trunk)d).UpdateGeometry();
        }

        public Point TipPosition
        {
            get { return (Point)GetValue(TipPositionProperty); }
            set { SetValue(TipPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TipPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TipPositionProperty =
            DependencyProperty.Register("TipPosition", typeof(Point), typeof(Trunk), new PropertyMetadata(new Point(0, 0), OnTipPositionChanged));


        private static void OnTipPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Trunk)d).UpdateGeometry();
        }


        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Fill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush), typeof(Trunk), new PropertyMetadata(null, OnFillColorChanged));

        private static void OnFillColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Trunk Trunk = (Trunk)d;
            Trunk.p.Fill = (Brush)e.NewValue;
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(Trunk), new PropertyMetadata(null, OnStrokeColorChanged));

        private static void OnStrokeColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Trunk Trunk = (Trunk)d;
            Trunk.p.Stroke = (Brush)e.NewValue;
        }


        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(Trunk), new PropertyMetadata(0.0, OnStrokeThicknessChanged));

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Trunk Trunk = (Trunk)d;
            Trunk.p.StrokeThickness = (double)e.NewValue;
        }




    }
}
