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
using System.Windows.Input;

namespace Clocks.Controls
{
    public class DataValue
    {
        public double Value { get; set; }

        public Color Color { get; set; }

        public string Tooltip { get; set; }
    }

    /// <summary>
    /// Interaction logic for BarGraph.xaml
    /// </summary>
    public partial class BarGraph : Canvas
    {
        int columnWidth = 20;
        List<StackedBar> series = new List<StackedBar>();
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
        /// <param name="capacity">The total number of DataValues you will add to this series</param>
        public StackedBar AddSeries(int capacity)
        {
            var s = new StackedBar(capacity);
            s.Changed += new EventHandler(OnBarChanged);
            this.Children.Add(s);
            series.Add(s);
            this.size = new Size(ActualWidth, ActualHeight);
            OnScaleChanged();
            return s;
        }

        void OnBarChanged(object sender, EventArgs e)
        {
            StackedBar bar = (StackedBar)sender;
            if (bar.Height != 0)
            {
                if (bar.Sum * bar.YScale > bar.Height)
                {
                    this.size = new Size(this.ActualWidth, this.ActualHeight);
                    OnScaleChanged();
                }
                else
                {
                    bar.Animate();
                }
            }
        }

        public void RemoveSeries(StackedBar bar)
        {
            this.Children.Remove(bar);
            series.Remove(bar);
        }

        public void Clear()
        {
            foreach (var s in this.series)
            {
                this.Children.Remove(s);
            }
            this.series.Clear();
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
                StackedBar bar = this.series[i];
                max = Math.Max(max, bar.Sum);
            }

            if (max == 0)
            {
                return;
            }

            double width = columnWidth - 2;
            double scale = s.Height / max;

            double x = 0;
            for (int i = 0; i < n; i++)
            {
                StackedBar bar = this.series[i];
                bar.Width = width;
                bar.Height = s.Height;
                bar.YScale = scale;
                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, 0);
                bar.Animate();
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
                        StackedBar bar = this.series[i];
                        Canvas.SetLeft(bar, Canvas.GetLeft(bar) + alignment);
                    }
                }
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
    /// A StackedBar is one bar in the BarGraph which stacks a bunch of datavalues in the bar showing the color of each. 
    /// </summary>
    public class StackedBar : UserControl
    {
        List<DataValue> data = new List<DataValue>();
        Rectangle bar;
        double sum;
        int capacity;
        bool updating;
        ToolTip tip;
        public event EventHandler Changed;

        public StackedBar(int capacity)
        {            
            this.capacity = capacity;
            bar = new Rectangle();  
            this.tip = new ToolTip() {
                Background = Brushes.DarkSlateBlue,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse
            };
            bar.VerticalAlignment = VerticalAlignment.Bottom;
            bar.ToolTip = this.tip;
            this.Content = bar;
        }

        public void Clear()
        {
            data.Clear();
            sum = 0;
            OnDataChanged();
        }

        public void AddDataValue(DataValue d)
        {
            data.Add(d);
            sum += d.Value;
            if (!updating)
            {
                OnDataChanged();
            }

            CreateGradientBrush();
        }

        void CreateGradientBrush()
        { 
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);
            double offset = 0;
            for (int i = 0, n = data.Count; i < n; i++)
            {
                var d = data[i];
                brush.GradientStops.Add(new GradientStop(d.Color, offset));
                offset += d.Value / Sum;
                brush.GradientStops.Add(new GradientStop(d.Color, offset));
            }
            bar.Fill = brush;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point pos = e.GetPosition(this);
            double my = pos.Y;
            double y = this.Height;
            DataValue hit = null;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                DataValue d = data[i];
                double y2 = y - (d.Value * this.YScale);
                if (my <= y && my >= y2)
                {
                    hit = d;
                    break;
                }
                y = y2;
            }
            if (hit != null)
            {
                tip.Background = new SolidColorBrush(hit.Color);
                tip.Content = hit.Tooltip;
            }

            base.OnMouseMove(e);
        }

        public int Count
        {
            get
            {
                return data.Count;
            }
        }

        public double YScale { get; set; }
        
        public static TimeSpan BarAnimationTime = TimeSpan.FromSeconds(1);


        public void Animate()
        {
            if (this.Sum != 0)
            {
                bar.Width = this.Width;
                bar.Height = this.sum * this.YScale;
                double h = this.Sum * this.YScale;
                DoubleAnimation animation = new DoubleAnimation();
                animation.From = 0;
                animation.To = h;
                Debug.WriteLine("Animating bar to height {0} from StackedBar of height {1}", h, this.Height);
                animation.Duration = new Duration(BarAnimationTime);
                animation.Completed += new EventHandler(OnAnimationCompleted);
                bar.BeginAnimation(Rectangle.HeightProperty, animation);                
            }
        }

        void OnAnimationCompleted(object sender, EventArgs args)
        {
        }

        private void OnDataChanged()
        {
            OnChanged();
        }

        public double Sum
        {
            get
            {
                return sum;
            }
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