using Clocks.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Windows.Foundation;

namespace Clocks.Controls
{

    /// <summary>
    /// Interaction logic for BarGraph.xaml
    /// </summary>
    public partial class BarGraph : Canvas
    {
        int columnWidth = 20;
        bool showLevels;
        List<BarSeries> series = new List<BarSeries>();
        Size size;

        public BarGraph()
        {
            this.SizeChanged += new SizeChangedEventHandler(OnSizeChanged);
        }

        public int ColumnWidth
        {
            get
            {
                return columnWidth;
            }
            set
            {
                columnWidth = value;
            }
        }

        /// <summary>
        /// Add a series to the graph, each series represents one bar in the graph
        /// </summary>
        /// <param name="capacity">The total number of data points you will add to this series</param>
        public BarSeries AddSeries(int capacity)
        {
            var s = new BarSeries(capacity);
            s.Changed += new EventHandler(OnBarChanged);
            this.Children.Add(s.Bar);
            this.Children.Add(s.Level);
            series.Add(s);
            this.size = new Size(ActualWidth, ActualHeight);
            OnScaleChanged();
            return s;
        }

        void OnBarChanged(object sender, EventArgs e)
        {
            OnLevelChanged();
            BarSeries bar = (BarSeries)sender;
            if (bar.AvailableHeight != 0)
            {
                if (bar.Height > bar.MaxHeight)
                {
                    this.size = new Size(this.ActualWidth, this.ActualHeight);
                    OnScaleChanged();
                }
                else
                {
                    bar.Animate(bar.AvailableHeight, bar.MaxHeight);
                }
            }
        }

        public void RemoveSeries(BarSeries bar)
        {
            this.Children.Remove(bar.Bar);
            this.Children.Remove(bar.Level);
            series.Remove(bar);
        }

        public void Clear()
        {
            foreach (var s in this.series)
            {
                this.Children.Remove(s.Bar);
                this.Children.Remove(s.Level);
            }
            this.series.Clear();
        }

        double SumOf(int n)
        {
            double r = 0;
            for (int i = 0; i <= n; i++)
            {
                r += i;
            }
            return r;
        }

        public HorizontalAlignment HorizontalContentAlignment
        {
            get
            {
                return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty);
            }
            set
            {
                SetValue(HorizontalContentAlignmentProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for HorizontalContentAlignment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HorizontalContentAlignmentProperty = DependencyProperty.Register("HorizontalContentAlignment", typeof(HorizontalAlignment), typeof(BarGraph), new PropertyMetadata(HorizontalAlignment.Left));

        internal void OnScaleChanged()
        {
            Size s = this.size;
            int n = this.series.Count;
            if (s == Size.Empty || s.Width == 0 || s.Height == 0 || double.IsInfinity(s.Width) || double.IsInfinity(s.Height) || n == 0)
            {
                return; // not ready yet.
            }
            double max = 0;
            
            for (int i = 0; i < n; i++)
            {
                BarSeries bar = this.series[i];
                max = Math.Max(max, bar.Height);
            }

            double width = columnWidth - 2;
            double scale = s.Height / max;

            double x = 0;
            for (int i = 0; i < n; i++)
            {
                BarSeries bar = this.series[i];
                Rectangle r = bar.Bar;
                r.Width = width;
                r.Height = bar.Height * scale;
                Canvas.SetLeft(r, x);
                Canvas.SetTop(r, s.Height);
                bar.Animate(s.Height, max);
                x += columnWidth;
            }

            if (x < s.Width)
            {
                double alignment = 0;
                switch (this.HorizontalContentAlignment)
                {
                    case HorizontalAlignment.Left:
                        alignment = 0;
                        break;
                    case HorizontalAlignment.Center:
                    case HorizontalAlignment.Stretch:
                        alignment = (s.Width - x) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        alignment = s.Width - x;
                        break;
                }
                if (alignment > 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        BarSeries bar = this.series[i];
                        Rectangle r = bar.Bar;
                        Canvas.SetLeft(r, Canvas.GetLeft(r) + alignment);
                    }
                }
            }
        }

        public bool ShowLevels
        {
            get
            {
                return this.showLevels;
            }
            set
            {
                this.showLevels = value;
                if (!value)
                    HideLevels();
            }
        }

        // The last column is adding data, so show where corresponding location is on other columns
        public void OnLevelChanged()
        {
            if (!ShowLevels)
                return;
            int n = this.series.Count;
            Size s = this.RenderSize;
            if (s == Size.Empty || s.Width == 0 || s.Height == 0 || double.IsInfinity(s.Width) || double.IsInfinity(s.Height) || n == 0)
            {
                return; // not ready yet.
            }
            BarSeries lastSeries = this.series[n - 1];
            int points = lastSeries.Count;
            for (int i = 0; i < n - 1; i++)
            {
                BarSeries bar = this.series[i];
                bar.SetLevel(points);
            }
        }

        void HideLevels()
        {
            foreach (BarSeries bar in this.series)
            {
                bar.Level.Height = 0;
            }
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.size = e.NewSize;
            animationDelay.StartDelayedAction("AnimateBars", () => { OnScaleChanged(); }, TimeSpan.FromMilliseconds(300));
        }

        DelayedActions animationDelay = new DelayedActions();
    }

    /// <summary>
    /// A BarSeries is one bar in the BarGraph which is the sum of all the given data points that make up the bar.
    /// The bar will grow as data points are added to this series.  The BarGraph can also animate side by side
    /// the relative position in two bars so you can see where you are in the new series relative to previous series.
    /// </summary>
    public class BarSeries
    {
        List<double> data = new List<double>();
        Rectangle bar;
        double sum;
        Rectangle level;
        int capacity;
        bool updating;
        public event EventHandler Changed;

        public BarSeries(int capacity)
        {
            this.capacity = capacity;
            bar = new Rectangle();            
            level = new Rectangle()
            {
                Fill = Brushes.White
            };
            bar.RenderTransform = new ScaleTransform()
            {
                ScaleX = 1,
                ScaleY = -1
            }; // flip it vertically.
        }

        public void Clear()
        {
            data.Clear();
            sum = 0;
            OnDataChanged();
        }

        public void AddDataPoint(double y)
        {
            data.Add(y);
            sum += y;
            if (!updating)
            {
                OnDataChanged();
            }
            TimeSpan elapsed = TimeSpan.FromSeconds((long)(sum / 1000));
            bar.ToolTip = new ToolTip() { Content = elapsed.ToString(), Background = Brushes.DarkSlateBlue };
        }

        public int Count
        {
            get
            {
                return data.Count;
            }
        }
        
        public static TimeSpan BarAnimationTime = TimeSpan.FromSeconds(1);

        public void Animate(double availableHeight, double max)
        {
            this.AvailableHeight = availableHeight;
            this.MaxHeight = max;
            if (this.Height != 0)
            {
                double h = (availableHeight * this.Height) / max;
                double current = bar.Height;
                if (double.IsNaN(current))
                    current = 0;
                DoubleAnimation animation = new DoubleAnimation();
                animation.From = 0;
                animation.To = h;
                animation.Duration = new Duration(BarAnimationTime);
                animation.Completed += new EventHandler(OnAnimationCompleted);
                bar.BeginAnimation(Rectangle.HeightProperty, animation);
                Debug.WriteLine("animate {0} to height {1}", bar.GetHashCode(), h);
            }
        }

        void OnAnimationCompleted(object sender, EventArgs args)
        {
        }

        private void OnDataChanged()
        {
            OnChanged();
        }

        public double Height
        {
            get
            {
                return sum;
            }
        }

        public Rectangle Bar
        {
            get
            {
                return bar;
            }
        }

        public Rectangle Level
        {
            get
            {
                return level;
            }
        }

        // space available for all bars
        public double AvailableHeight { get; set; }

        // max height of all bars used to scale this bar.
        public double MaxHeight { get; set; }

        public void SetLevel(int position)
        {
            level.Width = this.bar.Width;
            level.Height = 2;
            double y = GetHeightAt(position);
            Canvas.SetLeft(this.level, Canvas.GetLeft(this.bar));
            Canvas.SetTop(this.level, this.AvailableHeight - (this.AvailableHeight * y) / this.MaxHeight);
        }

        double GetHeightAt(int position)
        {
            double height = 0;
            for (int i = 0, n = data.Count; i < position && i < n; i++)
            {
                height += data[i];
            }
            return height;
        }

        void OnChanged()
        {
            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        public object UserData { get; set; }

        internal void BeginUpdate()
        {
            updating = true;
        }

        internal void EndUpdate()
        {
            updating = false;
            OnDataChanged();
        }

    }

}