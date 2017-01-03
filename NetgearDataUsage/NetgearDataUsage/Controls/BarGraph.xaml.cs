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
    public sealed partial class BarGraph : UserControl
    {
        DelayedActions layoutAction = new DelayedActions();
        List<double> values = new List<double>();
        int columnCount;
        bool layoutBusy;

        public BarGraph()
        {
            this.InitializeComponent();
            this.SizeChanged += BarGraph_SizeChanged;
            this.AnimationTime = TimeSpan.FromMilliseconds(100);
        }

        public TimeSpan AnimationTime { get; set; }

        private void BarGraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            layoutAction.StartDelayedAction("Layout", () => OnLayout(false), TimeSpan.FromMilliseconds(10));
        }

        public void SetColumnCount(int count)
        {
            this.columnCount = count;
        }

        public double TargetValue { get; set; }

        public List<double> DataValues
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

        private async void OnLayout(bool animate)
        {
            if (layoutBusy || this.columnCount == 0)
            {
                return;
            }
            layoutBusy = true;

            BarGrid.Children.Clear();
            BarGrid.ColumnDefinitions.Clear();
            double sum = values.Sum();
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

            bool first = true;
            int col = 0;
            foreach (var y in values)
            {
                Rectangle bar = new Rectangle();
                bar.Fill = this.Foreground;
                bar.Height = h * y / max;

                string tip = y.ToString("N0") + " of " + (step * (col + 1)).ToString("N0");
                ToolTipService.SetToolTip(bar, tip);
                if (first)
                {
                    first = false;
                    TargetLine.Y1 = this.ActualHeight - bar.Height;
                }
                bar.VerticalAlignment = VerticalAlignment.Bottom;
                Grid.SetColumn(bar, col++);
                BarGrid.Children.Add(bar);
            }
            TargetLine.X2 = this.ActualWidth - (colWidth / 2);
            TargetLine.Y2 = 0;

            if (animate)
            {
                foreach (Rectangle bar in BarGrid.Children)
                {
                    DoubleAnimation grow = new DoubleAnimation();
                    grow.EnableDependentAnimation = true;
                    grow.From = 0;
                    grow.To = bar.Height;
                    bar.Height = 0;
                    grow.Duration = new Windows.UI.Xaml.Duration(AnimationTime);
                    bar.BeginAnimation(grow, "Height");
                    await Task.Delay(30);
                }
            }

            layoutBusy = false;
        }
    }
}
