using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;

namespace ToonBuilder
{
    public enum ControlPointShape
    {
        Circle,
        Diamond
    }

    /// <summary>
    /// A ControlPoint connects two segments of a Path, one to the left, and one to the right.
    /// Potentially one of these could be null if the control point is at the beginning or end
    /// of the Path.
    /// </summary>
    public class ControlPoint : FrameworkElement
    {
        public const double FocusSize = 15;
        public const double NormalSize = 5;
        private static int next;
        private int id;
        private ControlPointShape shape = ControlPointShape.Circle;

        public ControlPoint()
        {
            id = next++;
            this.Background = Brushes.Teal;
        }

        public ControlPointShape Shape
        {
            get { return this.shape; }
            set { this.shape = value; InvalidateVisual(); }
        }

        public static readonly RoutedEvent ActivatedEvent = EventManager.RegisterRoutedEvent(
            "Activated", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ControlPoint));

        public event RoutedEventHandler Activated
        {
            add { AddHandler(ActivatedEvent, value); }
            remove { RemoveHandler(ActivatedEvent, value); }
        }

        void RaiseActivatedEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(ControlPoint.ActivatedEvent);
            RaiseEvent(newEventArgs);
        }

        public static readonly RoutedEvent DeactivatedEvent = EventManager.RegisterRoutedEvent(
            "Deactivated", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ControlPoint));

        public event RoutedEventHandler Deactivated
        {
            add { AddHandler(DeactivatedEvent, value); }
            remove { RemoveHandler(DeactivatedEvent, value); }
        }

        void RaiseDeactivatedEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(ControlPoint.DeactivatedEvent);
            RaiseEvent(newEventArgs);
        }        
        
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Background.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register("Background", typeof(Brush), typeof(ControlPoint), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));


        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            this.Opacity = 0.6;
            BeginAnimation(SizeProperty, new DoubleAnimation(FocusSize, new Duration(TimeSpan.FromMilliseconds(150))));
            RaiseActivatedEvent();
            e.Handled = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            this.Opacity = 1;
            BeginAnimation(SizeProperty, new DoubleAnimation(NormalSize, new Duration(TimeSpan.FromMilliseconds(150))));
            RaiseDeactivatedEvent();
        }

        bool captured;
        Vector mouseDownDelta;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsEnabled)
            {
                Point pos = GetPositionInParent(e); 
                mouseDownDelta = Position - pos;
                captured = this.CaptureMouse();
                e.Handled = true;
            }
        }

        public event EventHandler Moved;

        public Point Position
        {
            get
            {
                return new Point(Canvas.GetLeft(this), Canvas.GetTop(this));                
            }
            set
            {
                Canvas.SetLeft(this, value.X);
                Canvas.SetTop(this, value.Y);

                if (Moved != null)
                {
                    Moved(this, EventArgs.Empty);
                }
            }
        }

        Canvas GetCanvas()
        {
            Canvas canvas = null;
            SegmentEditor editor = VisualTreeHelper.GetParent(this) as SegmentEditor;
            if (editor != null)
            {
                canvas = VisualTreeHelper.GetParent(editor) as Canvas;
            }
            return canvas;
        }

        Point GetPositionInParent(MouseEventArgs e)
        {
            return e.GetPosition(GetCanvas());
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (captured)
            {
                Point pos = GetPositionInParent(e);
                Position = pos + mouseDownDelta;
                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (captured)
            {
                Point pos = GetPositionInParent(e);
                Position = pos + mouseDownDelta;
                ReleaseMouseCapture();
                captured = false;
            }
        }

        public double Size
        {
            get { return (double)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Radius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(double), typeof(ControlPoint), new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender));


        protected override void OnRender(DrawingContext drawingContext)
        {
            double s = this.Size;
            drawingContext.DrawEllipse(Brushes.Transparent, null, new Point(0, 0), FocusSize, FocusSize); // so we get mouse early entry
            switch (shape)
            {
                case ControlPointShape.Circle:
                    drawingContext.DrawEllipse(Background, null, new Point(0, 0), s, s);
                    break;
                case ControlPointShape.Diamond:
                    drawingContext.DrawGeometry(Background, null, new PathGeometry(new PathFigure[] {
                        new PathFigure(new Point(-s, 0), new PathSegment[] { 
                            new LineSegment(new Point(0,s), true),
                            new LineSegment(new Point(s,0), true),
                            new LineSegment(new Point(0,-s), true) }, true) }));
                    break;
                default:
                    break;
            }
        }
    }
}
