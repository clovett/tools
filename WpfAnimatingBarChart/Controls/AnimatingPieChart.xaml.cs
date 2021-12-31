using LovettSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LovettSoftware.Charts
{
    /// <summary>
    /// Interaction logic for AnimatingPieChart.xaml
    /// </summary>
    public partial class AnimatingPieChart : UserControl
    {
        DelayedActions actions = new DelayedActions();

        public AnimatingPieChart()
        {
            InitializeComponent();

            this.IsVisibleChanged += OnVisibleChanged;
        }

        private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OnDelayedUpdate();
        }

        public List<ChartDataValue> Series
        {
            get { return (List<ChartDataValue>)GetValue(SeriesProperty); }
            set { SetValue(SeriesProperty, value); }
        }

        public static readonly DependencyProperty SeriesProperty =
            DependencyProperty.Register("PieSeries", typeof(List<ChartDataValue>), typeof(AnimatingBarChart), new PropertyMetadata(null, OnSeriesChanged));

        private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AnimatingPieChart)d).OnSeriesChanged(e.NewValue);
        }

        private void OnSeriesChanged(object newValue)
        {
            if (newValue == null)
            {
                ResetVisuals();
            }
            else
            {
                OnDelayedUpdate();
            }
        }

        private void ResetVisuals()
        {
        }

        private void OnDelayedUpdate()
        {
            actions.StartDelayedAction("update", UpdateChart, TimeSpan.FromMilliseconds(10));
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            actions.StartDelayedAction("update", UpdateChart, TimeSpan.FromMilliseconds(10));
            return base.ArrangeOverride(arrangeBounds);
        }

        public event EventHandler<ChartDataValue> PieSliceHover;
        public event EventHandler<ChartDataValue> PieSliceClicked;


        private void UpdateChart()
        {
            // PieSlice.Point
            var duration = new Duration(TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(AngleProperty, new DoubleAnimation() { From = 0, To = 185 * Math.PI / 180, Duration = duration });
        }



        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(AnimatingPieChart), new PropertyMetadata(0.0, new PropertyChangedCallback(OnAngleChanged)));

        private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AnimatingPieChart)d).OnAngleChanged(e.NewValue);
        }

        private void OnAngleChanged(object newValue)
        {
            if (newValue is double d)
            {
                var s = PieSlice.Size;
                double x = Math.Cos(d) * s.Width;
                double y = Math.Sin(d) * s.Height;
                var start = new Point(100, 100);
                PieSlice.SweepDirection = SweepDirection.Clockwise;
                PieSlice.IsLargeArc = (d >= Math.PI);
                PieSlice.Point = new Point() { X = x + start.X, Y = y + start.Y };
            }
        }
    }
}