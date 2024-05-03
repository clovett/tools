using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BarChart.Utilities;
using System.Collections.Generic;
using System;
using System.Linq;
using BarChart.ViewModels;

namespace BarChart.Controls;

public delegate Visual ToolTipGenerator(ChartDataValue value);

public partial class AnimatingBarChart : UserControl
{
    DelayedActions actions = new DelayedActions();
    ColumnInfo tipColumn;
    Point movePos;
    ColumnInfo inside;
    ChartViewModel data;

    Random rand = new Random(Environment.TickCount);
    Size actualSize;

    // this is maintained for hit testing only since the mouse events don't seem to be 
    // working on the animated Rectangles.
    List<ColumnInfo> bars = new List<ColumnInfo>();

    List<Polygon> axisLines = new List<Polygon>();
    List<TextBlock> axisLabels = new List<TextBlock>();

    public event EventHandler<ChartDataValue> ColumnHover;
    public event EventHandler<ChartDataValue> ColumnClicked;

    public int HoverDelayMilliseconds { get; set; }

    /// <summary>
    /// Time to animate growth of the columns.
    /// </summary>
    public int AnimationGrowthMilliseconds { get; set; }

    /// <summary>
    /// Delay from column to column creates a nice ripple effect.
    /// </summary>
    public int AnimationRippleMilliseconds { get; set; }

    /// <summary>
    /// Time to animate the column color.
    /// </summary>
    public int AnimationColorMilliseconds { get; set; }

    public IBrush LineBrush
    {
        get { return (Brush)GetValue(LineBrushProperty); }
        set { SetValue(LineBrushProperty, value); }
    }

    public static readonly StyledProperty<IBrush> LineBrushProperty = AvaloniaProperty.Register<AnimatingBarChart, IBrush>("LineBrush", Brushes.Black);

    public ToolTipGenerator ToolTipGenerator { get; set; }

    class ColumnInfo
    {
        public TextBlock Label;
        public Rect Bounds;
        public Polygon Shape;
        public Color Color;
        public ChartDataValue Data;
    }

    static AnimatingBarChart()
    {
        LineBrushProperty.Changed.AddClassHandler<Interactive>(OnLineBrushChanged);
        OrientationProperty.Changed.AddClassHandler<Interactive>(OnOrientationChanged);
    }

    private static void OnOrientationChanged(Interactive interactive, AvaloniaPropertyChangedEventArgs args)
    {
        if (interactive is AnimatingBarChart chart)
        {
            chart.OnDelayedUpdate();
        }
    }

    private static void OnLineBrushChanged(Interactive interactive, AvaloniaPropertyChangedEventArgs args)
    {
        if (interactive is AnimatingBarChart chart)
        {
            chart.OnDelayedUpdate();
        }
    }

    public Orientation Orientation
    {
        get { return (Orientation)GetValue(OrientationProperty); }
        set { SetValue(OrientationProperty, value); }
    }

    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<AnimatingBarChart, Orientation>("ChartData", Orientation.Horizontal);

    public AnimatingBarChart()
    {
        InitializeComponent();

        this.HoverDelayMilliseconds = 250;
        this.AnimationGrowthMilliseconds = 200;
        this.AnimationRippleMilliseconds = 20;
        this.AnimationColorMilliseconds = 120;
        this.AttachedToVisualTree += OnAttachedToVisualTree;
        this.DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (DataContext is ChartViewModel data) {
            this.OnDataChanged(data);
        }
        base.OnDataContextChanged(e);
    }

    ColumnInfo FindColumn(Point pos)
    {
        for (int i = 0, n = bars.Count; i < n; i++)
        {
            var info = this.bars[i];
            var r = info.Bounds;
            if (pos.X >= r.Left && pos.X <= r.Right)
            {
                if (pos.Y >= r.Top && pos.Y <= r.Bottom)
                {
                    // found it!
                    return info;
                }
            }
        }
        return null;
    }


    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        var info = FindColumn(pos);
        if (info != null)
        {
            ChartDataValue value = info.Data;
            if (ColumnClicked != null)
            {
                ColumnClicked(this, value);
            }
        }
        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);
        var info = FindColumn(pos);
        if (info != null)
        {
            OnEnterColumn(info);
            HideToolTip();
            this.movePos = pos;
            this.tipColumn = info;
            actions.StartDelayedAction("hover", () =>
            {
                OnHover();
            }, TimeSpan.FromMilliseconds(HoverDelayMilliseconds));
        }
        else
        {
            this.tipColumn = null;
            OnExitColumn();
        }
        base.OnPointerMoved(e);
    }

    private void OnHover()
    {
        var info = this.tipColumn;
        if (info == null)
        {
            return;
        }

        ChartDataValue value = info.Data;
        //var tip = this.ToolTip as ToolTip;
        object tip = null;
        var content = ToolTipGenerator != null ? ToolTipGenerator(value) : new TextBlock() { Text = value.Label + "\r\n" + value.Value };
        if (tip == null)
        {
            //tip = new ToolTip()
            //{
            //    Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint,
            //    Content = content,
            //    IsOpen = true
            //};
            //this.ToolTip = tip;
        }
        else
        {
            //tip.Content = content;
            //tip.IsOpen = true;
        }
        //tip.Measure(new Size(100, 100));
        //tip.HorizontalOffset = 0;
        //tip.VerticalOffset = -tip.ActualHeight;

        // notify any interested listeners
        var h = this.ColumnHover;
        if (h != null)
        {
            h(this, value);
        }

    }

    private void OnEnterColumn(ColumnInfo info)
    {
        if (info != null)
        {
            var color = info.Color;
            Polygon r = info.Shape;
            if (inside == null || r != inside.Shape)
            {
                if (inside != null)
                {
                    OnExitColumn();
                }

                var duration = TimeSpan.FromMilliseconds(AnimationColorMilliseconds);
                var brush = r.Fill as SolidColorBrush;
                var highlight = GetMouseOverColor(color);
                if (brush.Transitions != null)
                {
                    brush.Transitions.Clear();
                    brush.Transitions = null;
                }
                var transitions = new Transitions();
                var mouseOverAnimation = new ColorTransition() { Duration = duration, Property = SolidColorBrush.ColorProperty };
                transitions.Add(mouseOverAnimation);
                brush.Transitions = transitions;
                brush.Color = highlight;
                inside = info;
            }
        }
    }

    void OnExitColumn()
    {
        // in case we are moving quickly, reset the colors of all the columns we've touched.
        for (int i = 0, n = bars.Count; i < n; i++)
        {
            var bar = bars[i];
            if (bar != inside)
            {
                Polygon r = bar.Shape;
                var brush = r.Fill as SolidColorBrush;
                brush.Color = bar.Color;
            }
        }
        inside = null;
    }

    private void OnDetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        HideToolTip();
    }

    private void OnDelayedUpdate()
    {
        actions.StartDelayedAction("update", UpdateChart, TimeSpan.FromMilliseconds(10));
    }

    private void HideToolTip()
    {
        // TODO: tooltips.
        //var tip = this.ToolTip as ToolTip;
        //if (tip != null)
        //{
        //    tip.IsOpen = false;
        //    this.ToolTip = null;
        //}
    }

    private Color GetMouseOverColor(Color c)
    {
        var hls = new HlsColor(c);
        hls.Lighten(0.5f);
        return hls.Color;
    }

    private void OnDataChanged(ChartViewModel data)
    {
        HideToolTip();
        this.data = data;
        if (data == null)
        {
            ResetVisuals();
        }
        else 
        {
            var s = data.Series;
            if (s.Count > 0)
            {
                var first = s[0].Values;
                int cols = first.Count;
                foreach (var series in s)
                {
                    var seriesDefaultColor = GetRandomColor();
                    if (series.Values.Count != cols)
                    {
                        throw new Exception("All series must have the same number of columns");
                    }
                    for (int i = 0; i < series.Values.Count; i++)
                    {
                        var d = series.Values[i];
                        if (!d.Color.HasValue)
                        {
                            d.Color = seriesDefaultColor;
                        }
                        if (d.Label != first[i].Label)
                        {
                            throw new Exception("All series must have the same label on each column");
                        }
                    }
                }
            }
            OnDelayedUpdate();
        }
    }
    void ResetVisuals()
    {
        ChartCanvas.Children.Clear();
        bars.Clear();
        tipColumn = null;
        inside = null;
    }

    private void OnAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        OnDelayedUpdate();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        this.actualSize = finalSize;
        OnDelayedUpdate();
        return base.ArrangeOverride(finalSize);
    }

    private void UpdateChart()
    {
        if (double.IsNaN(this.actualSize.Width) || this.actualSize.Width == 0)
        {
            return;
        }
        double w = this.actualSize.Width;
        double h = this.actualSize.Height;
        if (this.data == null || this.data.Series.Count == 0 || w == 0 || h == 0)
        {
            ResetVisuals();
        }
        else if (this.IsVisible)
        {
            if (this.Orientation == Orientation.Horizontal)
            {
                HorizontalLayout();
            }
            else
            {
                VerticalLayout();
            }
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        actions.CancelDelayedAction("hover");
        tipColumn = null;
        OnExitColumn();
        base.OnPointerExited(e);
    }


    private Size CreateColumnInfos()
    {
        int index = 0;
        Size minMax = new Size();
        bool firstSeries = true;
        foreach (var series in this.data.Series)
        {
            foreach (var item in series.Values)
            {
                ColumnInfo info = null;
                if (index < bars.Count)
                {
                    info = bars[index];
                }
                else
                {
                    info = new ColumnInfo();
                    bars.Add(info);
                }
                info.Data = item;
                info.Color = item.Color.Value;

                if (firstSeries && !string.IsNullOrEmpty(item.Label))
                {
                    var block = info.Label;
                    if (block == null)
                    {
                        block = new TextBlock() { Foreground = this.Foreground };
                        info.Label = block;
                    }
                    block.Text = item.Label;
                    if (block.Transitions != null)
                    {
                        block.Transitions.Clear();
                        block.Transitions = null;
                    }
                    block.Opacity = 0;
                    ChartCanvas.Children.Add(block); // so it measures properly.
                    block.Measure(new Size(100, 100));
                    ChartCanvas.Children.Remove(block);
                    var size = block.DesiredSize;
                    minMax = minMax.WithWidth(Math.Max(minMax.Width, size.Width));
                    minMax = minMax.WithHeight(Math.Max(minMax.Height, size.Height));
                }
                else
                {
                    info.Label = null;
                }
                index++;
            }
            firstSeries = false;
        }

        bars.RemoveRange(index, bars.Count - index);

        return minMax;
    }

    /// <summary>
    /// Add the range axis labels.
    /// </summary>
    private Size AddAxisLabels(out AxisTickSpacer scale)
    {
        double maxValue = 0;
        double minValue = 0;
        foreach (var series in this.data.Series)
        {
            foreach (var item in series.Values)
            {
                var v = item.Value;
                maxValue = Math.Max(maxValue, v);
                minValue = Math.Min(minValue, v);
            }
        }

        Size minMax = new Size();
        scale = new AxisTickSpacer(minValue, maxValue);
        var spacing = scale.GetTickSpacing();
        var min = scale.GetNiceMin();
        var max = scale.GetNiceMax();
        var labels = new List<TextBlock>();
        int i = 0;
        for (var r = min; r <= max; r += spacing)
        {
            TextBlock label = null;
            Polygon line = null;
            if (i < axisLabels.Count)
            {
                label = axisLabels[i];
                line = axisLines[i];
            }
            else
            {
                label = new TextBlock() { Foreground = this.Foreground };
                axisLabels.Add(label);
                line = new Polygon() { Stroke = this.LineBrush, StrokeThickness = 1, Points = new List<Point>() };
                axisLines.Add(line);
            }
            ChartCanvas.Children.Add(line);
            label.Text = r.ToString("N0");
            ChartCanvas.Children.Add(label);
            label.Measure(new Size(100, 100));
            minMax = minMax.WithWidth(Math.Max(minMax.Width, label.DesiredSize.Width));
            minMax = minMax.WithHeight(Math.Max(minMax.Height, label.DesiredSize.Height));
            i++;
        }

        axisLabels.RemoveRange(i, axisLabels.Count - i);
        axisLines.RemoveRange(i, axisLines.Count - i);

        return minMax;
    }

    private void VerticalLayout()
    {
        ChartCanvas.Children.Clear();

        var duration = TimeSpan.FromMilliseconds(this.AnimationGrowthMilliseconds);

        int columns = (from series in this.data.Series select series.Values.Count).Max();
        double w = this.actualSize.Width;
        double h = this.actualSize.Height;

        Size axisLabelSize = AddAxisLabels(out AxisTickSpacer scale);

        var min = scale.GetNiceMin();
        var max = scale.GetNiceMax();
        var spacing = scale.GetTickSpacing();

        Size labelSize = CreateColumnInfos();

        double labelGap = 10;
        double labelMargin = labelSize.Width + labelGap + labelGap;
        if (-min > labelMargin)
        {
            labelMargin = 0;
        }
        w -= labelMargin; // allocate space at the left column labels.
        h -= axisLabelSize.Height + labelGap + labelGap;

        int numSeries = this.data.Series.Count;
        double seriesHeight = h / columns;
        double innerGap = numSeries > 1 ? 2 : 0; // gap between columns in a series
        double seriesGap = seriesHeight / (3 * numSeries); // gap between series
        seriesHeight -= seriesGap;

        double columnHeight = seriesHeight / numSeries;
        columnHeight -= innerGap;

        double range = (max - min);
        double zero = 0;
        if (min < 0)
        {
            zero = Math.Abs(min) * w / range;
        }

        // layout the range axis labels and lines
        int i = 0;
        for (var r = min; r <= max; r += spacing)
        {
            double xpos = labelMargin + zero + (r * w / range);
            var label = axisLabels[i];
            var line = axisLines[i];
            var mid = label.DesiredSize.Width / 2;
            Canvas.SetLeft(label, xpos > mid ? xpos - mid : xpos + labelGap);
            Canvas.SetTop(label, h + labelGap);

            var poly = new List<Point>();
            poly.Add(new Point(xpos, 0));
            poly.Add(new Point(xpos, h));

            if (line.Transitions != null)
            {
                line.Transitions.Clear();
                line.Transitions = null;
            }
            var lineTransitions = new Transitions
                {
                    new PointCollectionTransition() { Duration = duration, Property = Polygon.PointsProperty }
                };
            line.Transitions = lineTransitions;
            line.Points = poly;

            if (label.Transitions != null)
            {
                label.Transitions.Clear();
                label.Transitions = null;
            }
            label.Opacity = 0;
            var transitions = new Transitions
                {
                    new DoubleTransition() { Duration = duration, Property = TextBlock.OpacityProperty }
                };
            label.Transitions = transitions;
            label.Opacity = 1;
            i++;
        }

        double y = 0;
        double x = labelMargin + zero;
        Rect previousLabel = new Rect(-1000, 0, 0, 0);

        // layout the columns.
        for (int col = 0; col < columns; col++)
        {
            int index = 0;
            foreach (var series in this.data.Series)
            {
                var dataValue = series.Values[col];
                double s = (dataValue.Value * w / range);
                Color color = dataValue.Color.Value;

                ColumnInfo info = this.bars[col + (index * columns)];
                Polygon polygon = info.Shape;
                SolidColorBrush brush = null;
                if (polygon != null)
                {
                    brush = polygon.Fill as SolidColorBrush;
                }
                else
                {
                    // make initial bars grow from zero.
                    var initial = new List<Point>();
                    initial.Add(new Point(x, y));
                    initial.Add(new Point(x, y));
                    initial.Add(new Point(x, y + columnHeight));
                    initial.Add(new Point(x, y + columnHeight));
                    brush = new SolidColorBrush() { Color = Colors.Transparent };
                    polygon = new Polygon() { Fill = brush, Points = initial };
                    info.Shape = polygon;
                }

                var start = TimeSpan.FromMilliseconds(index * AnimationRippleMilliseconds);

                if (info.Label != null)
                {
                    var block = info.Label;
                    var size = block.DesiredSize;
                    double xpos = 0;
                    if (s < 0)
                    {
                        // right of the negative sized column
                        xpos = x + labelGap;
                    }
                    else
                    {
                        xpos = x - labelGap - size.Width;
                    }

                    Rect bounds = new Rect(xpos, y + (seriesHeight - size.Height) / 2, size.Width, size.Height);
                    Rect inflated = bounds.Inflate(new Thickness(this.FontSize / 2, 0));
                    if (inflated.Intersects(previousLabel))
                    {
                        // skip it!
                    }
                    else
                    {
                        previousLabel = inflated;
                        Canvas.SetLeft(block, bounds.X);
                        Canvas.SetTop(block, bounds.Y);
                        if (block.Transitions != null)
                        {
                            block.Transitions.Clear();
                            block.Transitions = null;
                        }
                        block.Opacity = 0;
                        var transitions = new Transitions
                            {
                                new DoubleTransition() { Delay = start, Duration = duration, Property = TextBlock.OpacityProperty }
                            };
                        block.Transitions = transitions;
                        block.Opacity = 1;

                        ChartCanvas.Children.Add(block);
                    }
                }

                if (s < 0)
                {
                    info.Bounds = new Rect(x + s, y, -s, columnHeight);
                }
                else
                {
                    info.Bounds = new Rect(x, y, s, columnHeight);
                }

                var poly = new List<Point>();
                poly.Add(new Point(x, y));
                poly.Add(new Point(x + s, y));
                y += columnHeight;
                poly.Add(new Point(x + s, y));
                poly.Add(new Point(x, y));
                ChartCanvas.Children.Add(polygon);

                if (polygon.Transitions != null)
                {
                    polygon.Transitions.Clear();
                    polygon.Transitions = null;
                }
                var polyTransitions = new Transitions
                    {
                        new PointCollectionTransition() { Duration = duration, Property = Polygon.PointsProperty }
                    };
                polygon.Transitions = polyTransitions;
                polygon.Points = poly;

                brush.Color = Colors.Transparent;
                if (brush.Transitions != null)
                {
                    brush.Transitions.Clear();
                    brush.Transitions = null;
                }
                var colorTransitions = new Transitions
                    {
                        new ColorTransition() { Delay = start, Duration = duration, Property = SolidColorBrush.ColorProperty }
                    };
                brush.Transitions = colorTransitions;
                brush.Color = color;
                index++;
                y += innerGap;
            }
            y += seriesGap;
        }
    }

    private void HorizontalLayout()
    {
        ChartCanvas.Children.Clear();

        var duration = TimeSpan.FromMilliseconds(this.AnimationGrowthMilliseconds);

        int columns = (from series in this.data.Series select series.Values.Count).Max();
        double w = this.actualSize.Width;
        double h = this.actualSize.Height;

        Size axisLabelSize = AddAxisLabels(out AxisTickSpacer scale);

        var min = scale.GetNiceMin();
        var max = scale.GetNiceMax();
        var spacing = scale.GetTickSpacing();

        Size labelSize = CreateColumnInfos();

        double labelGap = this.FontSize / 3;
        double labelMargin = labelSize.Height + labelGap + labelGap;
        if (-min > labelMargin)
        {
            labelMargin = 0;
        }
        h -= labelMargin; // allocate space at the bottom for column labels.
        double axisLabelGap = axisLabelSize.Width + labelGap + labelGap;
        w -= axisLabelGap; // allocate space for axis labels.

        int numSeries = this.data.Series.Count;
        double seriesWidth = w / columns;
        double innerGap = numSeries > 1 ? 2 : 0; // gap between columns in a series
        double seriesGap = seriesWidth / (3 * numSeries); // gap between series
        seriesWidth -= seriesGap;

        double columnWidth = seriesWidth / numSeries;
        columnWidth -= innerGap;

        double range = (max - min);
        double zero = 0;
        if (min < 0)
        {
            zero = (Math.Abs(min) * h / range);
        }

        // layout the axis labels and lines
        int i = 0;
        for (var r = min; r <= max; r += spacing)
        {
            double ypos = (h - zero) - (r * h / range);
            var label = axisLabels[i];
            var line = axisLines[i];
            var mid = label.DesiredSize.Height / 2;
            Canvas.SetLeft(label, labelGap);
            Canvas.SetTop(label, ypos - mid);

            var poly = new List<Point>();
            poly.Add(new Point(axisLabelGap, ypos));
            poly.Add(new Point(w, ypos));
            if (line.Transitions != null)
            {
                line.Transitions.Clear();
                line.Transitions = null;
            }
            var polyTransitions = new Transitions
                {
                    new PointCollectionTransition() { Duration = duration, Property = Polygon.PointsProperty }
                };
            line.Transitions = polyTransitions;
            line.Points = poly;

            label.Opacity = 0;
            if (label.Transitions != null)
            {
                label.Transitions.Clear();
                label.Transitions = null;
            }
            var transitions = new Transitions
                {
                    new DoubleTransition() { Duration = duration, Property = TextBlock.OpacityProperty }
                };
            label.Transitions = transitions;
            label.Opacity = 1;
            i++;
        }

        Rect previousLabel = new Rect(-1000, 0, 0, 0);
        double x = axisLabelGap;
        double y = h - zero;

        // layout the columns.
        for (int col = 0; col < columns; col++)
        {
            int index = 0;
            foreach (var series in this.data.Series)
            {
                var dataValue = series.Values[col];
                double s = (dataValue.Value * h / range);
                Color color = dataValue.Color.Value;

                var start = TimeSpan.FromMilliseconds(index * AnimationRippleMilliseconds);
                ColumnInfo info = this.bars[col + (index * columns)];
                Polygon polygon = info.Shape;
                SolidColorBrush brush = null;
                if (polygon != null)
                {
                    brush = polygon.Fill as SolidColorBrush;
                }
                else
                {
                    // make initial bars grow from zero.
                    var initial = new List<Point>();
                    initial.Add(new Point(x, y));
                    initial.Add(new Point(x, y));
                    initial.Add(new Point(x + columnWidth, y));
                    initial.Add(new Point(x + columnWidth, y));
                    brush = new SolidColorBrush() { Color = Colors.Transparent };
                    polygon = new Polygon() { Fill = brush, Points = initial };
                    info.Shape = polygon;
                }

                if (info.Label != null)
                {
                    var block = info.Label;
                    var size = block.DesiredSize;
                    double ypos = 0;
                    if (s < 0)
                    {
                        // above the downward pointing column then.
                        ypos = y - labelGap - size.Height;
                    }
                    else
                    {
                        ypos = y + labelGap;
                    }

                    Rect bounds = new Rect(x + (seriesWidth - size.Width) / 2, ypos, size.Width, size.Height);
                    Rect inflated = bounds.Inflate(new Thickness(this.FontSize / 2, 0));
                    if (inflated.Intersects(previousLabel))
                    {
                        // skip it!
                    }
                    else
                    {
                        previousLabel = inflated;
                        Canvas.SetLeft(block, bounds.X);
                        Canvas.SetTop(block, bounds.Y);

                        if (block.Transitions != null)
                        {
                            block.Transitions.Clear();
                            block.Transitions = null;
                        }
                        block.Opacity = 0;
                        var transitions = new Transitions
                            {
                                new DoubleTransition() { Delay = start, Duration = duration, Property = TextBlock.OpacityProperty }
                            };
                        block.Transitions = transitions;
                        block.Opacity = 1;

                        ChartCanvas.Children.Add(block);
                    }
                }

                if (s < 0)
                {
                    info.Bounds = new Rect(x, y, columnWidth, -s);
                }
                else
                {
                    info.Bounds = new Rect(x, y - s, columnWidth, s);
                }

                var poly = new List<Point>();
                poly.Add(new Point(x, y));
                poly.Add(new Point(x, y - s));
                x += columnWidth;
                poly.Add(new Point(x, y - s));
                poly.Add(new Point(x, y));

                ChartCanvas.Children.Add(polygon);
                if (polygon.Transitions != null)
                {
                    polygon.Transitions.Clear();
                    polygon.Transitions = null;
                }
                var polyTransitions = new Transitions
                    {
                        new PointCollectionTransition() { Duration = duration, Property = Polygon.PointsProperty }
                    };
                polygon.Transitions = polyTransitions;
                polygon.Points = poly;

                if (brush.Transitions != null)
                {
                    brush.Transitions.Clear();
                    brush.Transitions = null;
                }
                brush.Color = Colors.Transparent;
                var colorTransitions = new Transitions
                    {
                        new ColorTransition() { Delay = start, Duration = duration, Property = SolidColorBrush.ColorProperty }
                    };
                brush.Transitions = colorTransitions;
                brush.Color = color;
                index++;
                x += innerGap;
            }

            x += seriesGap;
        }
    }

    private Color GetRandomColor()
    {
        return Color.FromRgb((byte)rand.Next(80, 200), (byte)rand.Next(80, 200), (byte)rand.Next(80, 200));
    }

}
