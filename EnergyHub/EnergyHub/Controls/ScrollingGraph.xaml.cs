using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using EnergyHub.Utilities;
using System.Threading;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace EnergyHub.Controls
{

    public partial class ScrollingGraph : UserControl
    {
        // these values have not been rendered yet
        List<double> pending = new List<double>();

        // we need two separate paths for the graph, 
        Path lineGraph1;
        Path lineGraph2;

        // these are used if Fill color is set.
        Path areaGraph1;
        Path areaGraph2;

        DispatcherTimer layoutTimer;
        bool scrollTimerStopped;
        Timer scrollTimer;
        bool sizeChanged;
        Size graphSize;
        const int FastestLayoutSpeed = 30; // can't seem to update the Path object more frequently than this.
        int x;
        bool started;
        bool canRun;
        double? lastY;

        public ScrollingGraph()
        {
            InitializeComponent();

            GraphCanvas.SetBinding(Canvas.BackgroundProperty, new Binding() { Source = this, Path = new PropertyPath("Background") });

            this.LayoutUpdated += Chart_LayoutUpdated;

            this.SizeChanged += ScrollingGraph_SizeChanged;

            this.Unloaded += ScrollingGraph_Unloaded;

            this.Loaded += ScrollingGraph_Loaded;
        }

        void ScrollingGraph_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeGraph();
        }

        private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas c = (Canvas)sender;
            c.Clip = new RectangleGeometry() { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
        }

        void InitializeGraph()
        {
            Brush brush = this.Stroke;
            if (brush == null)
            {
                brush = Brushes.Green;
            }

            Brush fill = this.Fill;
            if (this.areaGraph1 == null && fill != null)
            {
                areaGraph1 = new Path();
                areaGraph1.Fill = fill;
                GraphCanvas.Children.Add(areaGraph1);
            }
            if (this.areaGraph2 == null && fill != null)
            {
                areaGraph2 = new Path();
                areaGraph2.Fill = fill;
                GraphCanvas.Children.Add(areaGraph2);
            }
            if (this.lineGraph1 == null)
            {
                lineGraph1 = new Path();
                lineGraph1.StrokeThickness = StrokeThickness;
                lineGraph1.StrokeLineJoin = PenLineJoin.Round;
                lineGraph1.Stroke = brush;
                lineGraph1.StrokeLineJoin = PenLineJoin.Round;
                GraphCanvas.Children.Add(lineGraph1);
            }
            if (lineGraph2 == null)
            {
                lineGraph2 = new Path();
                lineGraph2.StrokeThickness = StrokeThickness;
                lineGraph2.StrokeLineJoin = PenLineJoin.Round;
                lineGraph2.Stroke = brush;
                lineGraph2.StrokeLineJoin = PenLineJoin.Round;
                GraphCanvas.Children.Add(lineGraph2);
            }
        }

        void ScrollingGraph_Unloaded(object sender, RoutedEventArgs e)
        {
            canRun = false;
            Stop();
        }

        /// <summary>
        /// Set this functor to provide a steady stream of values, this is called once per
        /// "scroll tick" according to the ScrollingSpeed.  For example, if the scrolling speed
        /// is 30 ms then it is called 1000/30 times a second.
        /// </summary>
        public Func<double> ValueGetter { get; set; }

        /// <summary>
        /// Start scrolling the current values.
        /// </summary>
        public void Start()
        {
            this.started = true;
            if (this.canRun)
            {
                StartAnimating();
            }
        }

        /// <summary>
        /// Stop scrolling.
        /// </summary>
        public void Stop()
        {
            this.started = false;

            if (layoutTimer != null)
            {
                layoutTimer.Tick -= OnLayoutTimer;
                layoutTimer.Stop();
                layoutTimer = null;
            }
            if (scrollTimer != null)
            {
                scrollTimer.Dispose();
                scrollTimer = null;
            }
            scrollTimerStopped = true;
        }



        public bool AutoScale
        {
            get { return (bool)GetValue(AutoScaleProperty); }
            set { SetValue(AutoScaleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AutoScale.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoScaleProperty =
            DependencyProperty.Register("AutoScale", typeof(bool), typeof(ScrollingGraph), new PropertyMetadata(false, new PropertyChangedCallback(OnAutoScaleChanged)));

        private static void OnAutoScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScrollingGraph)d).OnAutoScaleChanged();
        }

        private void OnAutoScaleChanged()
        {
            UpdateChart();
        }



        public int ScrollSpeed
        {
            get { return (int)GetValue(ScrollSpeedProperty); }
            set { SetValue(ScrollSpeedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScrollSpeed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScrollSpeedProperty =
            DependencyProperty.Register("ScrollSpeed", typeof(int), typeof(ScrollingGraph), new PropertyMetadata(0, new PropertyChangedCallback(OnScrollSpeedChanged)));

        private static void OnScrollSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScrollingGraph)d).OnScrollSpeedChanged();
        }

        private void OnScrollSpeedChanged()
        {
            if (scrollTimer != null)
            {
                var scrollSpeed = Math.Max(1, this.ScrollSpeed);
                scrollTimer.Dispose();
                scrollTimer = new Timer(new TimerCallback(OnScrollData), this, scrollSpeed, Timeout.Infinite);
            }
            if (layoutTimer != null && layoutTimer.IsEnabled)
            {
                layoutTimer.Stop();
                layoutTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(FastestLayoutSpeed, this.ScrollSpeed));
                layoutTimer.Start();
            }
        }



        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Minimum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(ScrollingGraph), new PropertyMetadata(0.0, new PropertyChangedCallback(OnMinimumChanged)));

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScrollingGraph)d).OnMinimumChanged();
        }

        private void OnMinimumChanged()
        {
            if (lastY.HasValue)
            {
                sizeChanged = true;
            }
        }


        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(ScrollingGraph), new PropertyMetadata(1.0, new PropertyChangedCallback(OnMaximumChanged)));

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScrollingGraph)d).OnMaximumChanged();
        }

        private void OnMaximumChanged()
        {
            if (lastY.HasValue)
            {
                sizeChanged = true;
            }
        }

        void ScrollingGraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (lastY.HasValue)
            {
                sizeChanged = true;
            }

            graphSize = e.NewSize;

            if (!started && this.pending.Count > 0)
            {
                this.RunOnUIThread(() =>
                {
                    UpdateChart();
                });
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            graphSize = finalSize;
            return base.ArrangeOverride(finalSize);
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StokeThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(ScrollingGraph), new PropertyMetadata(0.0));


        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(ScrollingGraph), new PropertyMetadata(null));



        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush), typeof(ScrollingGraph), new PropertyMetadata(null));



        public void Clear()
        {
            lastY = null;
            x = 0;
            sizeChanged = false;

            if (this.lineGraph1 != null)
            {
                Canvas.SetLeft(this.lineGraph1, 0);
                this.lineGraph1.Data = null;
            }
            if (this.lineGraph2 != null)
            {
                Canvas.SetLeft(this.lineGraph2, 0);
                this.lineGraph2.Data = null;
            }
        }


        private void Chart_LayoutUpdated(object sender, object e)
        {
            canRun = true;
            if (this.started)
            {
                StartAnimating();
            }
        }

        void StartAnimating()
        {
            if (layoutTimer != null)
            {
                layoutTimer.Start();
            }
            else
            {
                layoutTimer = new DispatcherTimer();
                layoutTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(FastestLayoutSpeed, this.ScrollSpeed));
                layoutTimer.Tick += OnLayoutTimer;
                layoutTimer.Start();
                if (scrollTimer != null)
                {
                    scrollTimer.Dispose();
                }
                scrollTimerStopped = false;
                scrollTimer = new Timer(new TimerCallback(OnScrollData), this, 1, Timeout.Infinite);
            }
        }

        private async void OnScrollData(object sender)
        {
            if (scrollTimerStopped)
            {
                return;
            }
            int now = Environment.TickCount;
            int scrollSpeed = 1;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, new Windows.UI.Core.DispatchedHandler(() =>
            {
                pending.Add(GetNextValue());
                if (x > graphSize.Width)
                {
                    double x1 = Canvas.GetLeft(this.lineGraph1) - 1;
                    double x2 = Canvas.GetLeft(this.lineGraph2) - 1;
                    Canvas.SetLeft(this.lineGraph1, x1);
                    Canvas.SetLeft(this.lineGraph2, x2);
                    if (this.areaGraph1 != null)
                    {
                        Canvas.SetLeft(this.areaGraph1, x1);
                        Canvas.SetLeft(this.areaGraph2, x2);
                    }
                }

                scrollSpeed = Math.Max(1, this.ScrollSpeed);
            }));

            int diff = Environment.TickCount - now;

            scrollTimer = new Timer(new TimerCallback(OnScrollData), this, scrollSpeed, Timeout.Infinite);
        }

        double GetNextValue()
        {
            if (ValueGetter == null)
            {
                throw new Exception("Did you forget to set the ValueGetter functor?");
            }
            return ValueGetter();
        }

        private void OnLayoutTimer(object sender, object e)
        {
            UpdateChart();
        }

        public void SetData(IEnumerable<double> values)
        {
            Clear();

            this.pending = new List<double>(values);
            if (lineGraph1 == null)
            {
                InitializeGraph();
            }
            this.RunOnUIThread(() =>
            {
                UpdateChart();
            });
        }

        /// <summary>
        /// Fill the graph with a horizontal line as the initial state instead of
        /// building graph slowly from the left hand edge of the screen.
        /// </summary>
        public bool FillGraph { get; set; }

        // This is called at most 30 times a second to update the graph from the pending values.
        private void UpdateChart()
        {
            double maxHorizontal = graphSize.Width;

            ComputeScale();

            var min = this.Minimum;
            var max = this.Maximum;

            double range = (max - min);
            double height = graphSize.Height;
            if (height == 0)
            {
                return; // not ready yet...
            }
            if (range == 0) range = 1;


            List<double> values = this.pending;
            this.pending = new List<double>();

            if (sizeChanged)
            {
                Clear();
                sizeChanged = false;
            }

            HashSet<Path> modified = new HashSet<Path>();

            int start = 0;
            int end = values.Count;

            if (values.Count > maxHorizontal)
            {
                // only show the tail that fits.
                start = values.Count - (int)maxHorizontal;
            }

            for (int i = start; i < end; i++)
            {
                double v = values[i];

                Path lineGraph = this.lineGraph1;
                Path areaGraph = this.areaGraph1;
                if (x >= maxHorizontal)
                {
                    lineGraph = this.lineGraph2;
                    areaGraph = this.areaGraph2;
                }

                if (lineGraph.Data == null)
                {
                    lineGraph.Data = new PathGeometry();
                    if (areaGraph != null)
                    {
                        areaGraph.Data = new PathGeometry();
                    }
                }
                modified.Add(lineGraph);
                if (areaGraph != null)
                {
                    modified.Add(areaGraph);
                }

                PathGeometry g = (PathGeometry)lineGraph.Data;
                PathFigure f = g.Figures.FirstOrDefault();

                PathGeometry areaGeometry = null;
                PathFigure areaFigure = null;
                if (areaGraph != null)
                {
                    areaGeometry = (PathGeometry)areaGraph.Data;
                    areaFigure = areaGeometry.Figures.FirstOrDefault();
                }

                Point minLabelPos = new Point(-100, 0);
                Point maxLabelPos = new Point(-100, 0);
                Point minLabelConnector = new Point(-100, 0);
                Point maxLabelConnector = new Point(-100, 0);

                // add the new values to the path
                double value = v;

                double y = height - ((value - min) * height / range);
                if (y < 0)
                {
                    y = 0;
                }
                else if (y > height)
                {
                    y = height;
                }
                if (f == null)
                {
                    double yStart = (lastY.HasValue ? lastY.Value : y);
                    f = new PathFigure() { StartPoint = new Point(x - 1, yStart), IsFilled = false, IsClosed = false };
                    g.Figures.Add(f);

                    if (areaGraph != null)
                    {
                        areaFigure = new PathFigure() { StartPoint = new Point(x - 1, height), IsFilled = true, IsClosed = true };
                        areaFigure.Segments.Add(new LineSegment() { Point = new Point(x - 1, yStart) });
                        areaFigure.Segments.Add(new LineSegment() { Point = new Point(x, height) });
                        areaGeometry.Figures.Add(areaFigure);
                    }

                    if (x + values.Count < maxHorizontal && FillGraph)
                    {
                        // then we want to fill the graph with a horizontal line rather than
                        // building graph from the left side of the screen.
                        x = (int)maxHorizontal - values.Count;
                        if (i + 1 == end)
                        {
                            values.Add(v);
                        }
                    }
                }

                f.Segments.Add(new LineSegment() { Point = new Point(x, y) });

                if (areaGraph != null)
                {
                    LineSegment lastSegment = (LineSegment)areaFigure.Segments.Last();
                    lastSegment.Point = new Point(x, height);
                    areaFigure.Segments.Insert(areaFigure.Segments.Count - 1, new LineSegment() { Point = new Point(x, y) });
                }

                if (x + 1 >= maxHorizontal * 2)
                {
                    // we swap them
                    Path temp = lineGraph2;
                    lineGraph2 = lineGraph1;
                    lineGraph1 = temp;
                    Canvas.SetLeft(this.lineGraph2, 0);

                    if (areaGraph != null)
                    {
                        temp = areaGraph2;
                        areaGraph2 = areaGraph1;
                        areaGraph1 = temp;
                        Canvas.SetLeft(this.areaGraph2, 0);
                    }

                    x = (int)maxHorizontal;

                    g = new PathGeometry(); // start new graph.
                    f = new PathFigure() { StartPoint = new Point(x - 1, y), IsFilled = false, IsClosed = false };
                    g.Figures.Add(f);
                    this.lineGraph2.Data = g;

                    if (areaGraph != null)
                    {
                        areaGeometry = new PathGeometry(); // start new graph.
                        areaFigure = new PathFigure() { StartPoint = new Point(x - 1, height), IsFilled = true, IsClosed = true };
                        areaFigure.Segments.Add(new LineSegment() { Point = new Point(x - 1, y) });
                        areaFigure.Segments.Add(new LineSegment() { Point = new Point(x, y) });
                        areaGeometry.Figures.Add(areaFigure);
                        this.areaGraph2.Data = areaGeometry;
                    }

                }

                x++;
                lastY = y;
            }

            if (areaGraph1 != null)
            {
                ScaleFill(areaGraph1);
                ScaleFill(areaGraph2);
            }

            // reassign the data to force a visual update.
            foreach (Path path in modified)
            {
                path.Data = path.Data;
            }
        }

        private void ComputeScale()
        {

            if (!AutoScale)
            {
                return;
            }

        }


        private void ScaleFill(Path areaGraph)
        {
            if (this.Fill is LinearGradientBrush && areaGraph.Data != null)
            {
                PathGeometry g = (PathGeometry)areaGraph.Data;
                Rect previous = Rect.Empty;
                if (areaGraph.Tag is Rect)
                {
                    previous = (Rect)areaGraph.Tag;
                }
                Rect box = g.Bounds;
                if (box.Height != previous.Height)
                {
                    areaGraph.Tag = box;
                    areaGraph.Fill = ScaleLinearGradient(box, (LinearGradientBrush)this.Fill);
                }
            }
        }

        List<GradientStop> sorted;

        private LinearGradientBrush ScaleLinearGradient(Rect bounds, LinearGradientBrush brush)
        {
            // problem with linear gradient is it stretches to fit the size of the shape, but then the
            // gradient keeps moving as the graph size changes with new data values, but if we want the
            // gradient to stay put as the graph scrolls horizontally, then we need to recompute the 
            // gradient based on the current graph height.

            // no vertical gradient, so we can return the brush unchanged.
            if (brush.StartPoint.Y == brush.EndPoint.Y)
            {
                return brush;
            }

            LinearGradientBrush newBrush = new LinearGradientBrush()
            {
                StartPoint = brush.StartPoint,
                EndPoint = brush.EndPoint,
                Opacity = brush.Opacity,
                SpreadMethod = brush.SpreadMethod,
                MappingMode = brush.MappingMode,
                ColorInterpolationMode = brush.ColorInterpolationMode
            };

            if (sorted == null)
            {
                sorted = new List<GradientStop>();
                foreach (GradientStop s in brush.GradientStops)
                {
                    sorted.Add(s);
                }
                sorted.Sort(new Comparison<GradientStop>((a, b) => { return (int)(a.Offset - b.Offset); }));
            }

            // pick the colors that are currently visible 
            double height = this.ActualHeight;
            double startOffset = (this.ActualHeight - bounds.Height) / this.ActualHeight;
            GradientStop previous = null;

            if (brush.StartPoint.Y > brush.EndPoint.Y)
            {
                throw new NotImplementedException("Please make gradient startPoint < endPoint");
            }
            bool first = true;
            for (int i = 0, n = sorted.Count; i < n; i++)
            {
                GradientStop s = sorted[i];
                double offset = s.Offset;
                if (offset > startOffset)
                {
                    Color c = s.Color;
                    if (first && previous != null)
                    {
                        double range = s.Offset - previous.Offset;
                        double ratio = startOffset / range;
                        c = InterpolateColor(previous.Color, s.Color, ratio);
                        newBrush.GradientStops.Add(new GradientStop() { Color = c, Offset = previous.Offset });
                        first = false;
                    }

                    newBrush.GradientStops.Add(new GradientStop() { Color = s.Color, Offset = s.Offset });
                }

                previous = s;
            }

            return newBrush;
        }

        /// <summary>
        /// Return color that is made up of "ratio" amount of color 2 from color.
        /// Ratio = 1 means color 2, Ratio = 0 means color1.
        /// </summary>
        private Color InterpolateColor(Color color1, Color color2, double ratio)
        {
            double da = color2.A - color1.A;
            double dr = color2.R - color1.R;
            double dg = color2.G - color1.G;
            double db = color2.B - color1.B;
            return Color.FromArgb((byte)((double)color1.A + ratio * da),
                (byte)((double)color1.R + ratio * dr),
                (byte)((double)color1.G + ratio * dg),
                (byte)((double)color1.B + ratio * db));
        }

    }

    static class RectExtensions
    {
        public static Point Center(this Rect r)
        {
            return new Point(r.Left + (r.Width / 2), r.Top + (r.Height / 2));
        }

        public static bool IntersectsWith(this Rect r1, Rect r2)
        {
            Rect r = r1;
            r.Intersect(r2);
            return !r.IsEmpty;
        }
    }

}
