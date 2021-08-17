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
    class Leaf : FrameworkElement
    {
        Path p;
        bool dirty;

        public Leaf()
        {
            // A leaf has a spine and is symmetric about that spine, for now we assume a linear spline
            // from BasePosition to TipPosition.
            p = new Path();
            this.AddVisualChild(p);
        }

        // the trunk that this leaf is attaced to.
        public Trunk Trunk { get; set; }


        public double PositionOnTrunk
        {
            get { return (double)GetValue(PositionOnTrunkProperty); }
            set { SetValue(PositionOnTrunkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PositionOnTrunk.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionOnTrunkProperty =
            DependencyProperty.Register("PositionOnTrunk", typeof(double), typeof(Leaf), new PropertyMetadata(0.0));

        
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
            p.Arrange(new Rect(0,0,finalSize.Width, finalSize.Height));
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
            if (Trunk == null)
            {
                return;
            }

            Point basePt = this.Trunk.GetPositionOnTrunk(this.PositionOnTrunk);
            Point tipPt = basePt + this.Spine;
            Vector spline = tipPt - basePt;
            double length = spline.Length;

            // now the pointiness of the leaf determes how far up the spline we travel from base to tip 
            // for the first control point.
            double pointiness = this.Pointiness;
            if (pointiness < 0.05)
            {
                pointiness = 0.05;
            }
            if (pointiness > .5)
            {
                pointiness = .5;
            }

            Vector normalizedSpline = spline;
            normalizedSpline.Normalize();

            // Curviness controls the distance from the spline for ctrlpt1,7,8 and 10
            // where 0 = on the spline and 1 is at the width of the leaf.
            double curviness = this.Curviness;
            if (curviness < 0) curviness = 0;
            if (curviness > 1) curviness = 1;

            Vector upperNormal = new Vector(-normalizedSpline.Y, normalizedSpline.X);

            double width = this.LeafWidth;
            Vector curvinessDelta = upperNormal * width * curviness;
            
            // and ctrlPt11
            Point base1 = basePt + (normalizedSpline * pointiness * length);
            Point ctrlPt1 = base1 + curvinessDelta;

            Point ctrlPt2 = base1 + (upperNormal * width);

            Point halfWay = basePt + (normalizedSpline * 0.5 * length);
            Point ctrlPt3 = halfWay + (upperNormal * width);

            Point base3 = basePt + (normalizedSpline * (1 - pointiness) * length);
            Point ctrlPt5 = base3 + curvinessDelta;
            Point ctrlPt4 = base3 + (upperNormal * width);

            Point ctrlPt6 = tipPt;

            // reflecting these points down other side
            Point ctrlPt7 = basePt + (normalizedSpline * (1 - pointiness) * length) - curvinessDelta;
            Point ctrlPt8 = base3 - (upperNormal * width);
            Point ctrlPt9 = halfWay - (upperNormal * width);
            Point ctrlPt10 = base1 - (upperNormal * width);
            Point ctrlPt11 = base1 - curvinessDelta;
            Point ctrlPt12 = basePt;

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
            f.IsClosed = true;
            f.IsFilled = true;
            f.StartPoint = basePt;

            PolyBezierSegment poly = (PolyBezierSegment)f.Segments[0];

            poly.Points = new PointCollection(new Point[] {
                ctrlPt1, ctrlPt2, ctrlPt3, ctrlPt4, ctrlPt5, ctrlPt6,
                ctrlPt7, ctrlPt8, ctrlPt9, ctrlPt10, ctrlPt11, ctrlPt12
            });

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

        public double Pointiness
        {
            get { return (double)GetValue(PointinessProperty); }
            set { SetValue(PointinessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Pointiness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointinessProperty =
            DependencyProperty.Register("Pointiness", typeof(double), typeof(Leaf), new PropertyMetadata(0.1, OnPointinessChanged));


        private static void OnPointinessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Leaf)d).UpdateGeometry();
        }

        public double Curviness
        {
            get { return (double)GetValue(CurvinessProperty); }
            set { SetValue(CurvinessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Curviness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurvinessProperty =
            DependencyProperty.Register("Curviness", typeof(double), typeof(Leaf), new PropertyMetadata(0.25, OnCurvinessChanged));


        private static void OnCurvinessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Leaf)d).UpdateGeometry();
        }

        public double LeafWidth
        {
            get { return (double)GetValue(LeafWidthProperty); }
            set { SetValue(LeafWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LeafWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LeafWidthProperty =
            DependencyProperty.Register("LeafWidth", typeof(double), typeof(Leaf), new PropertyMetadata(0.1, OnLeafWidthChanged));


        private static void OnLeafWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Leaf)d).UpdateGeometry();
        }


        public Vector Spine
        {
            get { return (Vector)GetValue(SpineProperty); }
            set { SetValue(SpineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BasePosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpineProperty =
            DependencyProperty.Register("Spine", typeof(Vector), typeof(Leaf), new PropertyMetadata(new Vector(0, 0), OnSpineChanged));

        private static void OnSpineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Leaf)d).UpdateGeometry();
        }

       
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Fill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush), typeof(Leaf), new PropertyMetadata(null, OnFillColorChanged));

        private static void OnFillColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Leaf leaf = (Leaf)d;
            leaf.p.Fill = (Brush)e.NewValue;
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(Leaf), new PropertyMetadata(null, OnStrokeColorChanged));

        private static void OnStrokeColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Leaf leaf = (Leaf)d;
            leaf.p.Stroke = (Brush)e.NewValue;
        }


        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(Leaf), new PropertyMetadata(0.0, OnStrokeThicknessChanged));

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Leaf leaf = (Leaf)d;
            leaf.p.StrokeThickness = (double)e.NewValue;            
        }



        
    }
}
