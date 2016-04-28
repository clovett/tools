using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace LogViewer
{
    public class DataValue
    {
        public double X { get; set; }
        public double Y { get; set; }

        public string Label { get; set; }

    }

    public class Range
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
    }

    public sealed partial class SimpleLineChart : UserControl
    {
        private List<DataValue> values;
        private DispatcherTimer _updateTimer;
        private Range _yRange;
        private Range _xRange;

        public Range YRange { get { return _yRange; } set { _yRange = value; } }
        public Range XRange { get { return _xRange; } set { _xRange = value; } }

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
            UpdateChart();
        }


        public void SetData(IEnumerable<DataValue> values)
        {
            this.values = new List<DataValue>(values);

            if (this.ActualWidth != 0)
            {
                UpdateChart();
            }
        }

        void UpdateChart()
        {
            /*
            <Path x:Name="Graph" Data="M 0,0 L 10,0 20,3 30,5 40,10 50,12"
                   StrokeThickness="2">
                <Path.Stroke>
                    <LinearGradientBrush StartPoint="0,0" EndPoint="1,0">
                        <GradientStop Offset="0" Color="Green"/>
                        <GradientStop Offset="0.2" Color="Green"/>
                        <GradientStop Offset="0.4" Color="Green"/>
                        <GradientStop Offset="0.6" Color="YellowGreen"/>
                        <GradientStop Offset="0.8" Color="Orange"/>
                        <GradientStop Offset="1" Color="Orange"/>
                    </LinearGradientBrush>
                </Path.Stroke>
            </Path>
             */

            if (values == null || values.Count == 0)
            {
                Graph.Data = null;
                MinLabel.Text = "";
                MaxLabel.Text = "";
                return;
            }

            double count = values.Count();

            double height = this.ActualHeight;
            double availableHeight = height - 50;
            double width = this.ActualWidth;

            PathGeometry g = new PathGeometry();
            PathFigure f = new PathFigure();
            g.Figures.Add(f);

            double minY = double.MaxValue;
            double maxY = double.MinValue;
            double minX = double.MaxValue;
            double maxX = double.MinValue;

            int len = values.Count;
            for (int i = 0; i < len; i++)
            {
                if (i < 0 || i >= len) 
                {
                    continue;
                }
                DataValue d = values[i];
                double x = d.X;
                double y = d.Y;
                minY = Math.Min(y, minY);
                maxY = Math.Max(y, maxY);
                minX = Math.Min(x, minX);
                maxX = Math.Max(x, maxX);
            }

            if (_yRange != null)
            {
                minY = _yRange.Minimum;
                maxY = _yRange.Maximum;
            }
            if (_xRange != null)
            {
                minX = _xRange.Minimum;
                minY = _xRange.Maximum;
            }

            double yScale = maxY - minY;
            if (yScale == 0)
            {
                yScale = 1;
            }
            double xScale = maxX - minX;
            if (xScale == 0)
            {
                xScale = 1;
            }

            for (int i = 0; i < len; i++)
            {
                if (i < 0 || i >= len)
                {
                    continue;
                }
                DataValue d = values[i];

                // add graph segment
                double y = availableHeight - ((d.Y - minY) * availableHeight / yScale);
                double x = ((d.X - minX) * width / xScale);

                Point pt = new Point(x, y);
                if (i == 0)
                {
                    f.StartPoint = pt;
                }
                else
                {
                    f.Segments.Add(new LineSegment() { Point = pt });
                }
                
            }

            MinLabel.Text = minY.ToString("N2");
            MaxLabel.Text = maxY.ToString("N2");
            
            Graph.Data = g;
            Graph.Stroke = this.Stroke;
            Graph.StrokeThickness = this.StrokeThickness;

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




    }

}
