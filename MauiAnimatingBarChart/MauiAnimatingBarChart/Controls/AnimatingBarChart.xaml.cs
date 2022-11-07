using Walkabout.Charts;
using Walkabout.Utilities;

namespace MauiAnimatingBarChart.Controls;

public partial class AnimatingBarChart : ContentView
{
    Random rand = new Random();
    ChartData data;
    Color foreground = Color.FromRgb(255, 255, 255);

    public AnimatingBarChart()
	{
		InitializeComponent();
        this.HoverDelayMilliseconds = 250;
        this.AnimationGrowthMilliseconds = 250;
        this.AnimationRippleMilliseconds = 20;
        this.AnimationColorMilliseconds = 120;
    }

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

    public ChartData Data { get => data;
        set
        {
            data = value;
            if (data != null)
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
                            if (d.Color == null)
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
            }
            Graphics.Drawable = CreateDrawing();
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (Graphics != null)
        {
            Graphics.Drawable = CreateDrawing();
        }
    }

    private IDrawable CreateDrawing()
    {
        var drawing = new BarChartDrawable(data, this.Bounds, foreground)
        {
            AnimationGrowthMilliseconds = this.AnimationGrowthMilliseconds,
            AnimationColorMilliseconds = this.AnimationColorMilliseconds,
            AnimationRippleMilliseconds = this.AnimationRippleMilliseconds
        };

        // WTF, why are there 2 animation classes?
        // var x = new Microsoft.Maui.Animations.Animation();

        var animation = new Microsoft.Maui.Controls.Animation(v =>
        {
            drawing.Clock = v;
            this.Graphics.Invalidate();
        }, 0, 1);

        animation.Commit(this.Graphics, "chart", 16, (uint)this.AnimationGrowthMilliseconds, Easing.Linear);

        return drawing;
    }


    private Color GetRandomColor()
    {
        return Color.FromRgb((byte)rand.Next(80, 200), (byte)rand.Next(80, 200), (byte)rand.Next(80, 200));
    }


    public class BarChartDrawable : IDrawable
    {
        ChartData data;
        Rect bounds;
        List<ColumnInfo> bars;
        List<AnimatedLabel> axisLabels;
        List<AnimatedLine> axisLines;
        string fontFamily;
        IFont font;
        float fontSize;
        Color foreground;
        Color lineColor = Color.FromRgb(50, 50, 50);

        class AnimatedLabel
        {
            public string Text;
            public Rect Start;
            public Rect Target;
        }

        class AnimatedLine
        {
            public Rect Start; // line from (x1,y1) to (x2,y2);
            public Rect Target;
        }

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

        public BarChartDrawable(ChartData data, Rect bounds, Color foreground, string fontFamily = "Segoe UI", float fontSize = 12)
        {
            this.data = data;
            this.bounds = bounds;
            this.foreground = foreground;
            this.fontFamily = fontFamily;
            this.fontSize = fontSize;
            this.font = new Microsoft.Maui.Graphics.Font(this.fontFamily);
        }

        /// <summary>
        /// Stage of the current animation frame from 0 to 1.
        /// </summary>
        public double Clock { get; set;  }

        private void LayoutColumns(ICanvas canvas)
        {
            int columns = (from series in this.data.Series select series.Values.Count).Max();
            double w = this.bounds.Width;
            double h = this.bounds.Height;

            Size axisLabelSize = AddAxisLabels(canvas, out AxisTickSpacer scale);

            var min = scale.GetNiceMin();
            var max = scale.GetNiceMax();
            var spacing = scale.GetTickSpacing();

            Size labelSize = CreateColumnInfos(canvas);

            double labelGap = this.fontSize / 3;
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

            var minmax = canvas.GetStringSize("yW^", font, fontSize);

            // layout the axis labels and lines
            int i = 0;
            for (var r = min; r <= max; r += spacing)
            {
                double ypos = (h - zero) - (r * h / range);
                var label = axisLabels[i];
                var mid = label.Target.Height / 2;
                label.Start = label.Target;
                label.Start.Offset(labelGap, h - zero); // starting point for animated labels.
                label.Target.Offset(labelGap, ypos - mid);

                var line = axisLines[i];
                line.Target = new Rect(axisLabelGap, ypos, w, ypos);
                line.Start = new Rect(axisLabelGap, h - zero, w, h - zero);
                i++;
            }

            Rect previousLabel = new Rect() { X = -1000, Y = 0, Width = 0, Height = 0 };
            double x = axisLabelGap;
            double y = h - zero;

            canvas.StrokeSize = 0;
            // layout the columns.
            for (int col = 0; col < columns; col++)
            {
                int index = 0;
                foreach (var series in this.data.Series)
                {
                    var dataValue = series.Values[col];
                    double s = (dataValue.Value * h / range);
                    Color color = dataValue.Color;

                    // var start = TimeSpan.FromMilliseconds(index * AnimationRippleMilliseconds);
                    ColumnInfo info = this.bars[col + (index * columns)];


                    if (info.Label != null)
                    {
                        var block = info.Label;
                        var size = canvas.GetStringSize(block, this.font, this.fontSize);
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

                        RectF bounds = new RectF() { X = (float)(x + (seriesWidth - size.Width) / 2), Y = (float)ypos, Width = size.Width, Height = size.Height };
                        RectF inflated = bounds;
                        inflated.Inflate(this.fontSize / 2, 0);
                        if (inflated.IntersectsWith(previousLabel))
                        {
                            // skip it!
                        }
                        else
                        {
                            previousLabel = inflated;
                            //Canvas.SetLeft(block, bounds.X);
                            //Canvas.SetTop(block, bounds.Y);
                            canvas.FillColor = this.foreground;
                            canvas.StrokeColor = this.foreground;
                            canvas.StrokeSize = 1;
                            canvas.DrawString(block, bounds.X, bounds.Y, HorizontalAlignment.Left);

                            //block.BeginAnimation(TextBlock.OpacityProperty, new DoubleAnimation()
                            //{
                            //    From = 0,
                            //    To = 1,
                            //    Duration = duration,
                            //    BeginTime = start
                            //});
                            //ChartCanvas.Children.Add(block);
                        }
                    }

                    if (s < 0)
                    {
                        info.Start = new RectF() { X = (float)x, Y = (float)y, Width = (float)columnWidth, Height = (float)y };
                        info.Target = new RectF() { X = (float)x, Y = (float)y, Width = (float)columnWidth, Height = (float)-s };
                    }
                    else
                    {
                        info.Start = new RectF() { X = (float)x, Y = (float)(y - s), Width = (float)columnWidth, Height = (float)y };
                        info.Target = new RectF() { X = (float)x, Y = (float)(y - s), Width = (float)columnWidth, Height = (float)s };
                    }

                    index++;
                    x += innerGap + columnWidth;
                }

                x += seriesGap;
            }
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (axisLabels == null)
            {
                LayoutColumns(canvas);
            }

            double opacity = this.Clock;
            canvas.StrokeColor = Color.FromRgba(this.lineColor.Red, this.lineColor.Green, this.lineColor.Blue, opacity);
            canvas.StrokeSize = 1;
            canvas.FillColor = this.foreground;

            for (int i = 0, n = axisLabels.Count; i < n; i++)            
            {
                var label = axisLabels[i];
                var line = axisLines[i];

                double ypos = label.Start.Y + ((label.Target.Y - label.Start.Y) * this.Clock);
                canvas.DrawString(label.Text, (float)label.Start.X, (float)ypos, HorizontalAlignment.Left);

                ypos = line.Start.Y + ((line.Target.Y - line.Start.Y) * this.Clock);
                canvas.DrawLine((float)line.Target.Left, (float)ypos, (float)line.Target.Right, (float)ypos);
            }

            foreach (ColumnInfo info in this.bars)
            {
                //PointCollection poly = new PointCollection();
                //poly.Add(new Point() { X = x, Y = y });
                //poly.Add(new Point() { X = x, Y = y - s });
                //x += columnWidth;
                //poly.Add(new Point() { X = x, Y = y - s });
                //poly.Add(new Point() { X = x, Y = y });

                //ChartCanvas.Children.Add(polygon);
                //polygon.BeginAnimation(Polygon.PointsProperty, new PointCollectionAnimation() { To = poly, Duration = duration, BeginTime = start });
                //brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation() { To = color, Duration = duration, BeginTime = start });

                double height = info.Target.Height * this.Clock;

                canvas.FillColor = info.Color;
                canvas.StrokeSize = 0;
                canvas.FillRectangle(new RectF((float)info.Target.X, (float)(info.Target.Y + info.Target.Height - height), (float)info.Target.Width, (float)height));
            }
        }


        class ColumnInfo
        {
            public string Label;
            public Rect Start;
            public Rect Target;
            public Color Color;
            public ChartDataValue Data;
        }

        /// <summary>
        /// Add the range axis labels.
        /// </summary>
        private Size AddAxisLabels(ICanvas canvas, out AxisTickSpacer scale)
        {
            if (axisLabels == null)
            {
                axisLabels = new List<AnimatedLabel>();
                axisLines = new List<AnimatedLine>();
            }
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
            int i = 0;
            for (var r = min; r <= max; r += spacing)
            {
                AnimatedLabel label = null;
                AnimatedLine line = null;
                if (i < axisLabels.Count)
                {
                    label = axisLabels[i];
                    line = axisLines[i];
                }
                else
                {
                    label = new AnimatedLabel();
                    axisLabels.Add(label);
                    line = new AnimatedLine();
                    axisLines.Add(line);
                }                
                label.Text = r.ToString("N0");
                var size = canvas.GetStringSize(label.Text, this.font, this.fontSize);
                label.Target = new Rect(0, 0, size.Width, size.Height);
                minMax.Width = Math.Max(minMax.Width, size.Width);
                minMax.Height = Math.Max(minMax.Height, size.Height);                
                i++;
            }

            // incremental update
            axisLabels.RemoveRange(i, axisLabels.Count - i);

            return minMax;
        }

        private Size CreateColumnInfos(ICanvas canvas)
        {
            if (bars == null)
            {
                bars = new List<ColumnInfo>();
            }

            int index = 0;
            Size minMax = new Size();
            bool firstSeries = true;
            foreach (var series in data.Series)
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
                    info.Color = item.Color;

                    if (firstSeries && !string.IsNullOrEmpty(item.Label))
                    {
                        info.Label = item.Label;
                        var size = canvas.GetStringSize(item.Label, font, fontSize);
                        minMax.Width = Math.Max(minMax.Width, size.Width);
                        minMax.Height = Math.Max(minMax.Height, size.Height);
                    }
                    else
                    {
                        info.Label = null;
                    }
                    index++;
                }
                firstSeries = false;
            }

            // incremental update to avoid memory overhead.
            bars.RemoveRange(index, bars.Count - index);

            return minMax;
        }
    }
}