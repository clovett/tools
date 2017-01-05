using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NetgearDataUsage.Controls
{
    public class DataValue
    {
        public string TipFormat; // has two arguments, the {0} actual and {1} target (or max value)
        public string ShortLabel;
        public double Value;
    }

    public sealed partial class BarGraph : UserControl
    {
        DelayedActions layoutAction = new DelayedActions();
        List<DataValue> values = new List<DataValue>();
        int columnCount;
        bool layoutBusy;

        public BarGraph()
        {
            this.InitializeComponent();
            this.SizeChanged += BarGraph_SizeChanged;
            this.AnimationTime = TimeSpan.FromMilliseconds(100);
            this.LastAnimationTime = TimeSpan.FromMilliseconds(500);
        }

        public TimeSpan AnimationTime { get; set; }

        public TimeSpan LastAnimationTime { get; set; }

        private void BarGraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            layoutAction.StartDelayedAction("Layout", () => OnLayout(false), TimeSpan.FromMilliseconds(10));
        }

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
                layoutAction.StartDelayedAction("Layout", () => OnLayout(true), TimeSpan.FromMilliseconds(10));
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
                layoutAction.StartDelayedAction("Layout", () => OnLayout(true), TimeSpan.FromMilliseconds(100));
                return;
            }
            layoutBusy = true;
            try
            {
                BarGrid.Children.Clear();
                BarGrid.ColumnDefinitions.Clear();
                double sum = (from dv in values select dv.Value).Sum();
                double h = this.ActualHeight;
                double w = this.ActualWidth;
                double colWidth = w / this.columnCount;

                for (int i = 0; i < this.columnCount; i++)
                {
                    BarGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new Windows.UI.Xaml.GridLength(1, GridUnitType.Star) });
                }

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
                double labelHeight = 16;

                foreach (var dv in values)
                {
                    double y = dv.Value;
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
                        grow.EnableDependentAnimation = true;
                        grow.From = 0;
                        grow.To = bar.Height;
                        grow.BeginTime = TimeSpan.FromMilliseconds(start);
                        bar.Height = 0;
                        if (bar == last)
                        {
                            grow.Duration = new Windows.UI.Xaml.Duration(LastAnimationTime);
                        }
                        else
                        {
                            grow.Duration = new Windows.UI.Xaml.Duration(AnimationTime);
                        }
                        bar.BeginAnimation(grow, "Height");
                        start += 30;
                    }
                }
            }
            finally
            {
                layoutBusy = false;
            }
        }

        private void Label_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
