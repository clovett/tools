using NetgearDataUsage.Model;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Walkabout.Utilities;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NetworkDataUsage.Controls
{
    public sealed partial class SimpleLineChart : UserControl
    {
        private DataSeries series;
        private DispatcherTimer _updateTimer;
        private DelayedActions _delayedUpdates = new DelayedActions();
        DataValue currentValue;
        bool liveScrolling;
        double liveScrollingXScale = 1;

        /// <summary>
        /// Set this property to add the chart to a group of charts.  The group will share the same "scale" information across the 
        /// combined chart group so that the charts line up under each other if they are arranged in a stack.
        /// </summary>
        public SimpleLineChart Next { get; set; }

        public SimpleLineChart()
        {
            this.InitializeComponent();
            this.Background = new SolidColorBrush(Colors.Transparent); // ensure we get manipulation events no matter where user presses.
            this.SizeChanged += SimpleLineChart_SizeChanged;
        }

        void SimpleLineChart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateChart();
        }

        /// <summary>
        /// Call this method when you turn on LiveScrolling.
        /// </summary>
        public void SetCurrentValue(DataValue value)
        {
            System.Threading.Interlocked.Exchange<DataValue>(ref currentValue, value);
        }

        internal void Close()
        {
            if (Closed != null)
            {
                Closed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Set this to true to start the graph scrolling showing live values provided by thread safe
        /// SetCurrentValue method.
        /// </summary>
        public bool LiveScrolling
        {
            get { return liveScrolling; }
            set
            {
                liveScrolling = value;
                if (value)
                {
                    StartUpdateTimer();
                }
                else
                {
                    StopUpdateTimer();
                }
            }
        }

        /// <summary>
        /// For live scrolling we need to know how to scale the X values, this determines how fast the graph scrolls.
        /// </summary>
        public double LiveScrollingXScale
        {
            get { return liveScrollingXScale; }
            set
            {
                liveScrollingXScale = value;
                DelayedUpdate();
            }
        }

        

        private void StartUpdateTimer()
        {
            if (_updateTimer == null)
            {
                _updateTimer = new DispatcherTimer();
                _updateTimer.Interval = TimeSpan.FromMilliseconds(30);
                _updateTimer.Tick += OnUpdateTimerTick;
            }
            _updateTimer.Start();
        }


        private void StopUpdateTimer()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Tick -= OnUpdateTimerTick;
                _updateTimer.Stop();
                _updateTimer = null;
            }
        }        

        void OnUpdateTimerTick(object sender, object e)
        {
            if (liveScrolling && currentValue != null)
            {
                SmoothScroll(currentValue);
            }
            else
            {
                UpdateChart();
            }
        }

        void DelayedUpdate()
        {
            _delayedUpdates.StartDelayedAction("Update", UpdateChart, TimeSpan.FromMilliseconds(30));
        }

        MatrixTransform zoomTransform = new MatrixTransform();
        MatrixTransform scaleTransform = new MatrixTransform();

        internal void ZoomTo(double x, double width)
        {
            // figure out where this is given existing transform.
            Matrix mp = zoomTransform.Matrix;
            mp.OffsetX -= x;
            mp.Scale(this.ActualWidth / width, 1);
            zoomTransform.Matrix = mp;

            var info = ComputeScaleSelf(0);
            ApplyScale(info);

            UpdateChart();
        }

        internal void ResetZoom()
        {
            zoomTransform = new MatrixTransform();
            UpdateChart();
        }

        /// <summary>
        /// Walk the circularly linked list to find all charts in the current chart group that we belong to.
        /// </summary>
        public IEnumerable<SimpleLineChart> GroupItems
        {
            get
            {
                for (var ptr = this; ptr != null; ptr = ptr.Next)
                {
                    yield return ptr;
                    if (ptr.Next == this)
                    {
                        break;
                    }
                }
            }
        }

        public void SetData(DataSeries series)
        {
            this.dirty = true;
            this.series = series;

            this.UpdateLayout();
            if (this.ActualWidth != 0)
            {
                UpdateChart();
            }
        }

        bool dirty;
        int scaleIndex; // for incremental scale calculation.

        double xScale;
        double yScale;
        double minY;
        double maxY;
        double minX;
        double maxX;

        /// <summary>
        /// Returns true if the scale just changed.
        /// </summary>
        bool ComputeScale()
        {
            bool changed = false;
            if (this.Next != null)
            {
                ScaleInfo combined = null;

                int index = this.scaleIndex;

                // make sure they are all up to date.
                foreach (var ptr in GroupItems)
                {
                    ScaleInfo info = ptr.ComputeScaleSelf(index);
                    if (combined == null)
                    {
                        combined = info;
                    }
                    else
                    {
                        combined.Combine(info);
                    }
                }

                //Debug.WriteLine("Combined scale: minx={0}, maxx={1}, miny={2}, maxy={3}", combined.minX, combined.maxX, combined.minY, combined.maxY);

                foreach (var ptr in GroupItems)
                {
                    if (ptr.ApplyScale(combined))
                    {
                        if (ptr != this)
                        {
                            ptr.DelayedUpdate();
                        }
                        changed = true;
                    }
                }
            }
            else
            {
                ScaleInfo info = ComputeScaleSelf(this.scaleIndex);
                changed = ApplyScale(info);
            }
            return changed;
        }

        class ScaleInfo
        {
            public double minX;
            public double maxX;
            public double minY;
            public double maxY;

            public ScaleInfo()
            {
                minY = double.MaxValue;
                maxY = double.MinValue;
                minX = double.MaxValue;
                maxX = double.MinValue;
            }

            public void Combine(ScaleInfo info)
            {
                minX = Math.Min(minX, info.minX);
                maxX = Math.Max(maxX, info.maxX);
                minY = Math.Min(minY, info.minY);
                maxY = Math.Max(maxY, info.maxY);
            }

            internal void Add(double x, double y)
            {
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }
        }


        ScaleInfo ComputeScaleSelf(int index)
        {
            ScaleInfo scale = new ScaleInfo();

            if (!dirty)
            {
                return null;
            }

            if (index > 0)
            {
                // this is an incremental update then so we pick up where we left off.
                scale.minY = minY;
                scale.maxY = maxY;
                scale.minX = minX;
                scale.maxX = maxX;
            }

            if (series == null)
            {
                return null;
            }

            int len = series.Values.Count;
            
            for (int i = index; i < len; i++)
            {
                if (i < 0 || i >= len)
                {
                    continue;
                }
                DataValue d = series.Values[i];
                double x = d.X;
                double y = d.Y;
                scale.Add(x, y);
            }

            scaleIndex = len;

            return scale;
        }

        bool ApplyScale(ScaleInfo info)
        {
            if (info == null)
            {
                return false;
            }
            double actualHeight = this.ActualHeight - 1;
            double actualWidth = this.ActualWidth;

            bool changed = false;
            this.minX = info.minX;
            this.maxX = info.maxX;
            this.minY = info.minY;
            this.maxY = info.maxY;

            double yRange = maxY - minY;
            if (yRange == 0)
            {
                yRange = 1;
            }
            double newyScale = actualHeight / yRange;
            if (newyScale == 0)
            {
                newyScale = 1;
            }
            if (newyScale != yScale)
            {
                yScale = newyScale;
                changed = true;
            }
            double newxScale = LiveScrollingXScale;
            if (!LiveScrolling)
            {
                double xRange = maxX - minX;
                if (xRange == 0) xRange = 1;
                newxScale = actualWidth / xRange;
                if (newxScale == 0)
                {
                    newxScale = 1;
                }
            }
            
            if (newxScale != xScale)
            {
                xScale = newxScale;
                changed = true;
            }

            if (changed || scaleTransform == null)
            {
                Matrix m = new Matrix();
                m.Scale(xScale, yScale);
                m.OffsetX = -minX * xScale;
                m.OffsetY = -minY * yScale;
                scaleTransform = new MatrixTransform(m);
            }

            return changed;
        }

        int updateIndex;
        int startIndex;

        private void SmoothScroll(DataValue newValue)
        {
            if (this.series == null)
            {
                this.series = new DataSeries() { Name = "", Values = new List<DataValue>() };
            }
            
            DataValue copy = new DataValue()
            {
                X = newValue.X,
                Y = newValue.Y
            };

            this.series.Values.Add(copy);

            PathGeometry g = Graph.Data as PathGeometry;
            if (g == null)
            {
                UpdateChart();
                return;
            }
            PathFigure f = g.Figures[0];

            bool redo = false;
            if (g.Bounds.Width > 2 * this.ActualWidth && this.series != null && this.series.Values.Count > 2 * this.ActualWidth)
            {
                // purge history since this is an infinite scrolling stream...
                this.series.Values.RemoveRange(0, this.series.Values.Count - (int)this.ActualWidth);
                System.Diagnostics.Debug.WriteLine("Trimming data series {0} back to {1} values", this.series.Name, this.series.Values.Count);
                dirty = true;
                updateIndex = 0;
                redo = true;
            }

            if (ComputeScale() || redo)
            {
                UpdateChart();
                g = Graph.Data as PathGeometry;
            }
            else
            {
                AddScaledValues(f, updateIndex, series.Values.Count);
            }

            double dx = g.Bounds.Width - this.ActualWidth;
            if (dx > 0)
            {
                Canvas.SetLeft(Graph, -g.Bounds.Left - dx);
                UpdatePointer(lastMousePosition);
            }
            updateIndex = series.Values.Count;
        }


        void UpdateChart()
        {
            Canvas.SetLeft(Graph, 0);
            scaleIndex = 0;
            updateIndex = 0;

            if (series == null || series.Values == null || series.Values.Count == 0)
            {
                Graph.Data = null;
                MinLabel.Text = "";
                MaxLabel.Text = "";
                return;
            }
            if (liveScrolling && series.Values.Count > this.ActualWidth)
            {
                // just show the tail that fits on screen, since the scaling will not happen on x-axis in this case.
                updateIndex = series.Values.Count - (int)this.ActualWidth;
                scaleIndex = updateIndex;
                startIndex = updateIndex;
                minY = double.MaxValue;
                maxY = double.MinValue;
                minX = double.MaxValue;
                maxX = double.MinValue;
            }

            ComputeScale();

            double count = series.Values.Count;
            PathGeometry g = new PathGeometry();
            PathFigure f = new PathFigure();
            f.IsClosed = true;
            g.Figures.Add(f);

            AddScaledValues(f, updateIndex, series.Values.Count);
            updateIndex = series.Values.Count;

            Graph.Data = g;
            Graph.Stroke = this.Stroke;
            Graph.Fill = this.Stroke;
            Graph.StrokeThickness = this.StrokeThickness;

            UpdatePointer(lastMousePosition);
        }

        private void AddScaledValues(PathFigure figure, int start, int end)
        {
            double height = this.ActualHeight - 1;
            double availableHeight = height;
            double width = this.ActualWidth;

            int len = series.Values.Count;
            double offset = Canvas.GetLeft(Graph);

            bool started = (figure.Segments.Count > 0);
            if (started)
            {
                // remove line to close graph.
                figure.Segments.RemoveAt(figure.Segments.Count - 1);
            }
            Point lastPoint = new Point();
            for (int i = start; i < end; i++)
            {
                DataValue d = series.Values[i];

                // add graph segment
                Point point = scaleTransform.Transform(new Point(d.X, d.Y));
                point = zoomTransform.Transform(point);
                double y = availableHeight - point.Y;
                double x = point.X;

                double rx = x + offset;
                if (rx > 0) 
                {
                    Point pt = new Point(x, y);
                    if (!started)
                    {
                        figure.StartPoint = new Point(x, availableHeight);
                        started = true;
                        figure.Segments.Add(new LineSegment() { Point = pt });
                    }
                    else
                    {
                        figure.Segments.Add(new LineSegment() { Point = pt });
                    }
                    lastPoint = pt;
                }
            }
            if (start < end)
            {
                // add line to close graph.
                lastPoint.Y = availableHeight;
                figure.Segments.Add(new LineSegment() { Point = lastPoint });
            }
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(SimpleLineChart), new PropertyMetadata(new SolidColorBrush(Colors.White)));



        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StrokeThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(SimpleLineChart), new PropertyMetadata(1.0));

        public event EventHandler Closed;
        public Point lastMousePosition;

        internal void HandleMouseMove(MouseEventArgs e)
        {
            lastMousePosition = e.GetPosition(this);
            UpdatePointer(lastMousePosition);
        }

        internal void HandleMouseLeave()
        {
            HidePointer();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            HandleMouseMove(e);
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            HandleMouseLeave();
            base.OnMouseLeave(e);
        }

        public Color LineColor
        {
            get
            {
                var brush = this.Stroke as SolidColorBrush;
                if (brush != null)
                {
                    return brush.Color;
                }

                return Colors.Black;
            }
            set
            {
                this.Stroke = new SolidColorBrush(value);

                HlsColor darker = new HlsColor(value);
                darker.Darken(0.33f);
                PointerBorder.BorderBrush = Pointer.Fill = PointerLabel.Foreground = new SolidColorBrush(darker.Color);

            }
        }

        const double TooltipThreshold = 20;

        void UpdatePointer(Point pos)
        {
            // transform top Graph coordinates (which could be constantly changing because of zoom and scrolling.
            pos = this.TransformToDescendant(Graph).Transform(pos);

            if (series != null && series.Values != null && series.Values.Count > 0)
            {

                // add graph segment
                double height = this.ActualHeight;
                double availableHeight = height;
                double width = this.ActualWidth;

                double minDistance = double.MaxValue;
                DataValue found = null;

                double offset = Canvas.GetLeft(Graph); 

                // find the closed data value.

                for (int i = 0; i < series.Values.Count; i++)
                {
                    DataValue d = series.Values[i];

                    Point scaled = scaleTransform.Transform(new Point(d.X, d.Y));
                    scaled = zoomTransform.Transform(scaled);
                    double x = scaled.X;
                    double y = availableHeight - scaled.Y;
                    double dx = (x - pos.X);
                    double dy = y - pos.Y;
                    double distance = Math.Sqrt((dx * dx) + (dy * dy));
                    if (distance < TooltipThreshold)
                    {
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            found = d;
                        }
                    }
                }
                if (found != null)
                {
                    PointerLabel.Text = series.Name + " = " + (!string.IsNullOrEmpty(found.Label) ? found.Label : found.Y.ToString());
                    PointerBorder.UpdateLayout();

                    double tipPositionX = pos.X + offset;
                    if (tipPositionX + PointerBorder.ActualWidth > this.ActualWidth)
                    {
                        tipPositionX = this.ActualWidth - PointerBorder.ActualWidth;
                    }
                    double tipPositionY = pos.Y - PointerLabel.ActualHeight - 4;
                    if (tipPositionY < 0)
                    {
                        tipPositionY = 0;
                    }
                    PointerBorder.Margin = new Thickness(tipPositionX, tipPositionY, 0, 0);
                    PointerBorder.Visibility = System.Windows.Visibility.Visible;

                    double value = found.Y;
                    Point scaled = scaleTransform.Transform(new Point(found.X, found.Y));
                    scaled = zoomTransform.Transform(scaled);
                    double x = scaled.X + offset;
                    double y = availableHeight - scaled.Y;
                    Point pointerPosition = new Point(x, y);
                    Pointer.RenderTransform = new TranslateTransform(pointerPosition.X, pointerPosition.Y);
                    Pointer.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    PointerBorder.Visibility = System.Windows.Visibility.Hidden;
                    Pointer.Visibility = System.Windows.Visibility.Hidden;
                }

            }
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            this.dirty = true;
            DelayedUpdate();
            return base.ArrangeOverride(arrangeBounds);
        }

        void HidePointer()
        {
            Pointer.Visibility = Visibility.Collapsed;
        }

    }

}
