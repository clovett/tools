using System;
using System.Collections.Generic;
using System.IO;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace RandomNumbers.Controls
{

    public sealed partial class SimpleLineChart : UserControl
    {
        List<DataValue> values;
        private TransformGroup _transformGroup;
        private MatrixTransform _previousTransform;
        private CompositeTransform _compositeTransform;
        private DispatcherTimer _updateTimer;

        public SimpleLineChart()
        {
            this.InitializeComponent();
            this.Background = new SolidColorBrush(Colors.Transparent); // ensure we get manipulation events no matter where user presses.
            this.SizeChanged += SimpleLineChart_SizeChanged;
            this.ManipulationMode = ManipulationModes.Scale | ManipulationModes.TranslateInertia | ManipulationModes.TranslateX;

            InitTransform();
        }

        void SimpleLineChart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateChart();
        }

        protected override void OnManipulationStarting(ManipulationStartingRoutedEventArgs e)
        {
            var mode = e.Mode;
        }

        void InitTransform()
        {
            _transformGroup = new TransformGroup();
            _compositeTransform = new CompositeTransform();
            _previousTransform = new MatrixTransform()
            {
                Matrix = Matrix.Identity
            };

            _transformGroup.Children.Add(_previousTransform);
            _transformGroup.Children.Add(_compositeTransform);
        }

        protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
        {
            _previousTransform.Matrix = _transformGroup.Value;

            Point center = _previousTransform.TransformPoint(
                new Point(e.Position.X, e.Position.Y));

            _compositeTransform.CenterX = center.X;
            _compositeTransform.CenterY = center.Y;            
        }

        protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
        {
            _compositeTransform.Rotation = (e.Cumulative.Rotation * 180) / Math.PI;
            _compositeTransform.ScaleX = e.Cumulative.Scale;
            _compositeTransform.TranslateX = e.Cumulative.Translation.X;
            StartUpdateTimer();
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

        protected override void OnManipulationInertiaStarting(ManipulationInertiaStartingRoutedEventArgs e)
        {
        }

        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            StopUpdateTimer();
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

            Brush stroke = null;
            
            double maxY = double.MinValue;
            double minY = double.MaxValue;

            double maxX = double.MinValue;
            double minX = double.MaxValue;


            foreach (DataValue d in values)
            {
                maxY = Math.Max(maxY, d.Y);
                minY = Math.Min(minY, d.Y);
                maxX = Math.Max(maxX, d.X);
                minX = Math.Min(minX, d.X);
            }


            int len = values.Count;
            double rangeY = maxY - minY;
            if (rangeY == 0)
            {
                rangeY = 1; // avoid divide by zero
            }
            double rangeX = maxX - minX;
            if (rangeX == 0)
            {
                rangeX = 1; // avoid divide by zero
            }


            for (int i = 0; i < len; i++)
            {
                if (i < 0 || i >= len)
                {
                    continue;
                }
                DataValue d = values[i];
                if (stroke == null)
                {
                    stroke = d.Color;
                }

                // add graph segment
                double y = availableHeight - ((d.Y - minY) * availableHeight / rangeY);
                double x = ((d.X - minX) * width / rangeX);

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
            Graph.Stroke = stroke;
        }
    }

}
