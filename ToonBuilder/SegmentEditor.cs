using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows;
using System.Diagnostics;

namespace ToonBuilder
{
    /// <summary>
    /// This class is used to draw one segment of a Path and make it editable when mouse over is detected.
    /// </summary>
    public abstract class SegmentEditor : Canvas
    {
        Path path;
        PathFigure figure;
        PathGeometry geometry;
        PathSegment segment;
        ControlPoint start;
        ControlPoint end;
        bool isEditing;
        bool selected;
        static Color ControlPointColor = Color.FromArgb(0xc8, 0x51, 0x8c, 0x54);

        public SegmentEditor(Point start, PathSegment segment)
        {
            this.path = new Path() { Stroke = Brushes.Teal, StrokeThickness = 1 };
            this.segment = segment;
            this.figure = new PathFigure(start, new PathSegment[] { segment }, false);
            geometry = new PathGeometry(new PathFigure[] { figure });
            path.Data = geometry;
            this.Children.Add(path);
            this.IsHitTestVisible = true;
        }

        public ControlPoint Start 
        {
            get { return start; }
            set
            {
                if (start != value)
                {
                    if (start != null)
                    {
                        if (VisualTreeHelper.GetParent(start) == this)
                        {
                            this.Children.Remove(start);
                        }

                        UnregisterEvents(start);
                    }
                    start = value;
                    if (VisualTreeHelper.GetParent(start) == null)
                    {
                        this.Children.Add(start);
                    }
                    if (start != null)
                    {
                        RegisterEvents(start);
                    }
                }
            }
        }

        public ControlPoint End
        {
            get { return end; }
            set
            {
                if (end != value)
                {
                    if (end != null)
                    {
                        if (VisualTreeHelper.GetParent(end) == this)
                        {
                            this.Children.Remove(end);
                        }
                        UnregisterEvents(start);
                    }
                    end = value;
                    if (VisualTreeHelper.GetParent(end) == null)
                    {
                        this.Children.Add(end);
                    }
                    if (end != null)
                    {
                        RegisterEvents(end);
                    }
                }
            }
        }

        public PathSegment Segment
        {
            get { return segment; }
        }

        public bool IsSelected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
                if (value)
                {
                    path.Stroke = Brushes.Red;
                    path.StrokeThickness = 2;
                }
                else
                {
                    path.Stroke = Brushes.Teal;
                    path.StrokeThickness = 1;
                }
                IsSelectedChanged();
            }
        }

        protected virtual void IsSelectedChanged()
        {
        }

        protected ControlPoint AddControlPoint(Point pos)
        {
            ControlPoint dot = new ControlPoint();
            dot.Background = new SolidColorBrush(ControlPointColor);
            Canvas.SetLeft(dot, pos.X);
            Canvas.SetTop(dot, pos.Y);
            this.Children.Add(dot);
            RegisterEvents(dot);
            return dot;
        }

        protected void RegisterEvents(ControlPoint dot)
        {
            dot.Moved += new EventHandler(Moved_ControlPoint);
        }

        protected void UnregisterEvents(ControlPoint dot)
        {
            dot.Moved -= new EventHandler(Moved_ControlPoint);
        }

        private void Moved_ControlPoint(object sender, EventArgs e)
        {
            OnControlPointMoved((ControlPoint)sender);
        }

        protected virtual void OnControlPointMoved(ControlPoint pt)
        {
            if (pt == Start)
            {
                figure.StartPoint = pt.Position;
            }
        }

        public virtual void BeginEdit()
        {
            isEditing = true;
        }

        public virtual void Commit()
        {
            isEditing = false;
        }

        public bool IsEditing { get { return isEditing; } }

        public abstract SegmentEditor CreateNew(ControlPoint pt, Point pos);

        public abstract bool Add(Point pos);

        public abstract void Move(Point pos);

        internal void MakeHead()
        {            
            // make the start point a child of this canvas.      
            object parent = VisualTreeHelper.GetParent(Start);
            if (parent != null)
            {
                Debug.WriteLine("Who?");
            }
            else
            {
                this.Children.Add(Start);
            }
        }

        internal void MakeTail()
        {
            // nothing to do.
        }

        public virtual void OnRemoved()
        {
            if (VisualTreeHelper.GetParent(Start) == this)
            {
                this.Children.Remove(Start);
            }
            this.Children.Remove(End);
        }

        public virtual void Join(SegmentEditor previous)
        {
            this.Start = previous.End;
            OnControlPointMoved(this.Start);
        }

        public abstract void WriteSegment(PathGeometry geometry);
    }

    class LineSegmentEditor : SegmentEditor
    {
        int points;

        public LineSegmentEditor(Point pos)
            : base(pos, new LineSegment(pos, true))
        {
            this.Start = AddControlPoint(pos);
            this.End = AddControlPoint(pos);
        }

        private LineSegmentEditor(ControlPoint start, Point pos)
            : base(pos, new LineSegment(pos, true))
        {
            this.Start = start;
            RegisterEvents(start);
            this.End = AddControlPoint(pos);
        }

        public override void BeginEdit()
        {
            base.BeginEdit();
            this.End.IsEnabled = false;
        }

        public override void Commit()
        {
            base.Commit();
            this.End.IsEnabled = true;
        }

        protected override void OnControlPointMoved(ControlPoint pt)
        {
            base.OnControlPointMoved(pt);
            LineSegment ls = (LineSegment)this.Segment;
            if (pt == this.End)
            {
                ls.Point = pt.Position;
            }
        }

        public override SegmentEditor CreateNew(ControlPoint pt, Point pos)
        {
            return new LineSegmentEditor(pt, pos);
        }

        public override bool Add(Point pos)
        {
            LineSegment line = (LineSegment)Segment;
            
            if (points == 0)
            {
                points++;
                line.Point = pos; 
            }

            Commit();
            return false;
        }

        public override void Move(Point pos)
        {
            if (IsEditing && points == 0)
            {
                this.End.Position = pos;
            }
        }

        public override void WriteSegment(PathGeometry geometry)
        {
            PathFigure fig = geometry.Figures.FirstOrDefault();
            if (fig == null)
            {
                fig = new PathFigure();
                fig.StartPoint = this.Start.Position;
                geometry.Figures.Add(fig);
            }
            fig.Segments.Add(new LineSegment(this.End.Position, true));
        }
    }

    class ArcSegmentEditor : SegmentEditor
    {
        int points;

        public ArcSegmentEditor(Point pos)
            : base(pos, new ArcSegment(pos, new Size(50, 50), 0, false, SweepDirection.Clockwise, true))
        {
            this.Start = AddControlPoint(pos);
            this.End = AddControlPoint(pos);
        }

        private ArcSegmentEditor(ControlPoint start, Point pos)
            : base(pos, new ArcSegment(pos, new Size(50, 50), 0, false, SweepDirection.Clockwise, true))
        {
            this.Start = start; 
            RegisterEvents(start);
            this.End = AddControlPoint(pos);
        }

        public override void BeginEdit()
        {
            base.BeginEdit();
            this.End.IsEnabled = false;
        }

        public override void Commit()
        {
            base.Commit();
            this.End.IsEnabled = true;
        }

        protected override void OnControlPointMoved(ControlPoint pt)
        {
            base.OnControlPointMoved(pt);
            ArcSegment arc = (ArcSegment)this.Segment;
            if (pt == this.End)
            {
                arc.Point = pt.Position;
            }
        }

        public override SegmentEditor CreateNew(ControlPoint pt, Point pos)
        {
            return new ArcSegmentEditor(pt, pos);
        }

        public override bool Add(Point pos)
        {
            ArcSegment arc = (ArcSegment)Segment;

            if (points == 0)
            {
                points++;
                arc.Point = pos;
            }
            Commit();
            return false;
        }

        public override void Move(Point pos)
        {
            if (IsEditing && points == 0)
            {
                this.End.Position = pos;
            }
        }

        public override void WriteSegment(PathGeometry geometry)
        {
            PathFigure fig = geometry.Figures.FirstOrDefault();
            if (fig == null)
            {
                fig = new PathFigure();
                fig.StartPoint = this.Start.Position;
                geometry.Figures.Add(fig);
            }
            ArcSegment arc = (ArcSegment)this.Segment;
            fig.Segments.Add(new ArcSegment(this.End.Position, arc.Size, arc.RotationAngle, arc.IsLargeArc, arc.SweepDirection, true));
        }
    }

    class BezierSegmentEditor : SegmentEditor
    {
        int points;
        ControlPoint middle1;
        ControlPoint middle2;
        static Pen ControlPointPen = new Pen(Brushes.Red, 0.5);

        public BezierSegmentEditor(Point pos)
            : base(pos, new BezierSegment(pos, pos, pos, true))
        {
            this.Start = AddControlPoint(pos);
            middle1 = AddDiamondPoint(pos);
            middle2 = AddDiamondPoint(pos);
            this.End = AddControlPoint(pos);
        }

        private BezierSegmentEditor(ControlPoint start, Point pos)
            : base(pos, new BezierSegment(pos, pos, pos, true))
        {
            this.Start = start;
            RegisterEvents(start);
            middle1 = AddDiamondPoint(pos);
            middle2 = AddDiamondPoint(pos);
            this.End = AddControlPoint(pos);
        }

        ControlPoint AddDiamondPoint(Point pos)
        {
            var pt = AddControlPoint(pos);
            pt.Shape = ControlPointShape.Diamond;
            pt.Background = Brushes.Red;
            return pt;
        }

        public override void BeginEdit()
        {
            base.BeginEdit();
            middle1.IsEnabled = false;
            middle2.IsEnabled = false;
            this.End.IsEnabled = false;
        }

        public override void Commit()
        {
            base.Commit();
            this.Start.IsEnabled = true;
            middle1.IsEnabled = true;
            middle2.IsEnabled = true;
            this.End.IsEnabled = true;
        }

        protected override void IsSelectedChanged()
        {
            middle1.Visibility = middle2.Visibility = IsSelected ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            InvalidateVisual();
        }


        protected override void OnControlPointMoved(ControlPoint pt)
        {
            base.OnControlPointMoved(pt);
            Point pos = pt.Position;
            BezierSegment bs = (BezierSegment)this.Segment;
            if (pt == middle1)
            {
                bs.Point1 = pos;
            }
            else if (pt == middle2)
            {
                bs.Point2 = pos;
            }
            else if (pt == this.End)
            {
                bs.Point3 = pos;
            }
            InvalidateVisual();
        }

        public override SegmentEditor CreateNew(ControlPoint pt, Point pos)
        {
            return new BezierSegmentEditor(pt, pos);
        }

        public override bool Add(Point pos)
        {
            BezierSegment curve = (BezierSegment)Segment;

            Move(pos);
            if (points == 0)
            {
                middle1.IsEnabled = true;
                points++;
            }
            else if (points == 1)
            {
                middle2.IsEnabled = true;
                points++;
            }
            else if (points == 2)
            {
                this.End.IsEnabled = true;
                points++;
            }
            if (points == 3)
            {
                Commit();
            }
            return IsEditing;
        }

        public override void Move(Point pos)
        {
            BezierSegment curve = (BezierSegment)Segment;
            if (points == 0)
            {                
                curve.Point1 = pos;
                curve.Point2 = pos;
                curve.Point3 = pos;
                middle1.Position = pos;
                middle2.Position = pos;
                this.End.Position = pos;
                InvalidateVisual();
            }
            else if (points == 1)
            {
                curve.Point2 = pos;
                curve.Point3 = pos;
                middle2.Position = pos;
                this.End.Position = pos;
                InvalidateVisual();
            }
            else if (points == 2)
            {
                curve.Point3 = pos;
                this.End.Position = pos;
                InvalidateVisual();
            }

        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            this.Children.Remove(middle1);
            this.Children.Remove(middle2);
        }

        public override void WriteSegment(PathGeometry geometry)
        {
            PathFigure fig = geometry.Figures.FirstOrDefault();
            if (fig == null)
            {
                fig = new PathFigure();
                fig.StartPoint = this.Start.Position;
                geometry.Figures.Add(fig);
            }
            BezierSegment bez = (BezierSegment)this.Segment;
            fig.Segments.Add(new BezierSegment(bez.Point1, bez.Point2, bez.Point3, true));
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (middle1.Visibility == System.Windows.Visibility.Visible)
            {
                dc.DrawLine(ControlPointPen, Start.Position, middle1.Position);
                dc.DrawLine(ControlPointPen, End.Position, middle2.Position);
            }
            base.OnRender(dc);
        }
    }
}

