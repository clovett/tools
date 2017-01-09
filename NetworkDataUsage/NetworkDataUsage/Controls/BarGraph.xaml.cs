using NetgearDataUsage.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Walkabout.Utilities;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NetworkDataUsage.Controls
{
    public sealed partial class BarGraph : UserControl
    {
        DelayedActions delayedActions = new DelayedActions();
        List<DataValue> values = new List<DataValue>();
        int columnCount;
        bool layoutBusy;
        const double TooltipThreshold = 20;

        public BarGraph()
        {
            this.InitializeComponent();
            this.SizeChanged += BarGraph_SizeChanged;
            this.AnimationTime = TimeSpan.FromMilliseconds(100);
            this.LastAnimationTime = TimeSpan.FromMilliseconds(500);
            this.MouseMove += OnMouseMoved;
            // so we get all mouse moves.
            BarGrid.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void BarGraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            delayedActions.StartDelayedAction("Layout", () => OnLayout(false), TimeSpan.FromMilliseconds(10));
        }

        public TimeSpan AnimationTime { get; set; }

        public TimeSpan LastAnimationTime { get; set; }

        public void SetColumnCount(int count)
        {
            this.columnCount = count;
        }

        public double TargetValue { get; set; }

        public List<DataValue> DataValues
        {
            get
            {
                return values;
            }
            set
            {
                values = value;
                delayedActions.StartDelayedAction("Layout", () => OnLayout(true), TimeSpan.FromMilliseconds(10));
            }
        }

        private void OnLayout(bool animate)
        {
            if (this.columnCount == 0)
            {
                return;
            }
            if (layoutBusy)
            {
                // try again later.
                delayedActions.StartDelayedAction("Layout", () => OnLayout(true), TimeSpan.FromMilliseconds(100));
                return;
            }
            layoutBusy = true;
            try
            {
                BarGrid.Children.Clear();
                BarGrid.ColumnDefinitions.Clear();
                double h = this.ActualHeight;
                double w = this.ActualWidth;
                double colWidth = w / this.columnCount;

                for (int i = 0; i < this.columnCount; i++)
                {
                    BarGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                }

                double sum = (from dv in values select dv.Y).Sum();
                double max = TargetValue;
                if (max < sum)
                {
                    max = sum;
                }

                TargetLine.X1 = colWidth / 2;
                double step = max / this.columnCount;

                List<Rectangle> bars = new List<Rectangle>();
                bool first = true;
                int col = 0;

                foreach (var dv in values)
                {
                    double y = dv.Y;
                    Rectangle bar = new Rectangle();
                    bar.Fill = this.Foreground;
                    bar.Height = h * y / max;

                    string tip = string.Format(dv.TipFormat, y, (step * (col + 1)));

                    ToolTipService.SetToolTip(bar, tip);
                    bar.VerticalAlignment = VerticalAlignment.Bottom;
                    Grid.SetColumn(bar, col);
                    BarGrid.Children.Add(bar);

                    TextBlock label = new TextBlock() { Text = dv.ShortLabel };
                    label.HorizontalAlignment = HorizontalAlignment.Center;
                    label.Margin = new Thickness() { Top = 2, Bottom = 5 };

                    if (first)
                    {
                        first = false;
                        TargetLine.Y1 = this.ActualHeight - bar.Height;
                        label.SizeChanged += (sender, args) =>
                        {
                            TargetLine.Y1 -= args.NewSize.Height;
                        };
                    }
                    Grid.SetColumn(label, col);
                    Grid.SetRow(label, 1);

                    BarGrid.Children.Add(label);
                    col++;

                    bars.Add(bar);
                }

                TargetLine.X2 = this.ActualWidth - (colWidth / 2);
                TargetLine.Y2 = 0;

                if (animate)
                {
                    int start = 0;
                    Rectangle last = bars.LastOrDefault() as Rectangle;
                    foreach (var bar in bars)
                    {
                        DoubleAnimation grow = new DoubleAnimation();
                        grow.From = 0;
                        grow.To = bar.Height;
                        grow.BeginTime = TimeSpan.FromMilliseconds(start);
                        bar.Height = 0;
                        if (bar == last)
                        {
                            grow.Duration = new Duration(LastAnimationTime);
                        }
                        else
                        {
                            grow.Duration = new Duration(AnimationTime);
                        }
                        bar.BeginAnimation(Rectangle.HeightProperty, grow);
                        start += 30;
                    }
                }
            }
            finally
            {
                layoutBusy = false;
            }
        }

        private void OnMouseMoved(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);
            UpdatePointer(pos);
        }


        void UpdatePointer(Point pos)
        {
            // transform top Graph coordinates (which could be constantly changing because of zoom and scrolling.
            pos = this.TransformToVisual(Overlay).Transform(pos);

            double offset = Overlay.Margin.Left;
            double x1 = TargetLine.X1;
            double y1 = TargetLine.Y1;
            double x2 = TargetLine.X2;
            double y2 = TargetLine.Y2;
            double x = pos.X;
            double w = this.ActualWidth;
            double colWidth = w / this.columnCount;
            double sum = (from dv in values select dv.Y).Sum();
            double max = TargetValue;
            if (max < sum)
            {
                max = sum;
            }
            double min = max / this.columnCount; // bottom of the target line 

            double slope = (y2 - y1) / (x2 - x1);
            double y = (slope * (x - x1)) + y1;

            double distance = Math.Abs(y - pos.Y);
            if (x1 < x2 && x >= x1 && x <= x2 && distance < TooltipThreshold)
            {
                PointerLabel.Text = string.Format("Maximum Target: {0:N0}", min + this.TargetValue * ((x - colWidth/2) / (x2 - x1)));
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
                PointerBorder.Visibility = Visibility.Visible;
                
                Point pointerPosition = new Point(x, y);
                Pointer.RenderTransform = new TranslateTransform() { X = pointerPosition.X, Y = pointerPosition.Y };
                Pointer.Visibility = Visibility.Visible;

                delayedActions.StartDelayedAction("HideTip", () => {

                    PointerBorder.Visibility = Visibility.Collapsed;
                    Pointer.Visibility = Visibility.Collapsed;

                }, TimeSpan.FromSeconds(2));
            }
            else
            {
                PointerBorder.Visibility = Visibility.Collapsed;
                Pointer.Visibility = Visibility.Collapsed;
            }

        }

    }
}
