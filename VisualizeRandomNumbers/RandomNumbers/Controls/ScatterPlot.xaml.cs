using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace RandomNumbers.Controls
{
    public sealed partial class ScatterPlot : UserControl
    {
        List<DataValue> values;
        List<LineData> lines = new List<LineData>();

        public ScatterPlot()
        {
            this.InitializeComponent();

            this.SizeChanged += SimpleLineChart_SizeChanged;
        }

        void SimpleLineChart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateChart();
        }

        public event EventHandler<DataValue> Selected;


        public void SetData(IEnumerable<DataValue> values)
        {
            if (values == null)
            {
                this.values = new List<DataValue>();
            }
            else
            {
                this.values = new List<DataValue>(values);
            }
            if (this.ActualWidth != 0)
            {
                UpdateChart();
            }
        }



        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Minimum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(ScatterPlot), new PropertyMetadata(0.0, new PropertyChangedCallback(OnMinimumChanged)));

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScatterPlot)d).OnMinimumChanged();
        }

        private void OnMinimumChanged()
        {
            UpdateChart();
        }



        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(ScatterPlot), new PropertyMetadata(0.0, new PropertyChangedCallback(OnMaximumChanged)));

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScatterPlot)d).OnMaximumChanged();
        }

        private void OnMaximumChanged()
        {
            UpdateChart();
        }



        public double DotSize
        {
            get { return (double)GetValue(DotSizeProperty); }
            set { SetValue(DotSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DotSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DotSizeProperty =
            DependencyProperty.Register("DotSize", typeof(double), typeof(ScatterPlot), new PropertyMetadata(3.0, new PropertyChangedCallback(OnDotSizeChanged)));

        private static void OnDotSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScatterPlot)d).OnDotSizeChanged();
        }

        private void OnDotSizeChanged()
        {
            UpdateChart();
        }
        public void AddValue(DataValue dataValue)
        {
            if (values == null)
            {
                values = new List<DataValue>();
            }
            values.Add(dataValue);

            UpdateChart();
        }

        void UpdateChart()
        {
            List<Ellipse> dots = new List<Ellipse>();
            List<TextBlock> labels = new List<TextBlock>();
            List<Line> lineShapes = new List<Line>();
            if (PlotCanvas == null)
            {
                return; // not initialized yet
            }
            foreach (UIElement e in PlotCanvas.Children)
            {
                if (e is Ellipse)
                {
                    dots.Add((Ellipse)e);
                }
                else if (e is Line)
                {
                    lineShapes.Add((Line)e);
                }
                else if (e is TextBlock)
                {
                    labels.Add((TextBlock)e);
                }
            }
            tooltip = null;

            if (values != null && values.Count > 0)
            {

                double count = values.Count();
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

                double height = this.ActualHeight;
                double availableHeight = height - 50;
                double width = this.ActualWidth;

                LinearGradientBrush stroke = new LinearGradientBrush();
                stroke.StartPoint = new Point(0, 0);
                stroke.EndPoint = new Point(1, 0);
                double size = DotSize;

                foreach (DataValue d in values)
                {
                    // add graph segment
                    double y = availableHeight - ((d.Y - minY) * availableHeight / rangeY);
                    double x = ((d.X - minX) * width / rangeX);
                    Ellipse dot = null;
                    if (dots.Count > 0)
                    {
                        dot = dots[0];
                        dots.RemoveAt(0);
                    }

                    if (dot == null)
                    {
                        dot = new Ellipse();
                        PlotCanvas.Children.Add(dot);
                    }
                    dot.Width = size;
                    dot.Height = size;
                    dot.Fill = d.Color;
                    dot.Tag = d;
                    Canvas.SetLeft(dot, x);
                    Canvas.SetTop(dot, y);

                    // add label
                    if (!string.IsNullOrEmpty(d.Label))
                    {
                        TextBlock label = null;

                        if (labels.Count > 0)
                        {
                            label = labels[0];
                            labels.RemoveAt(0);
                        }

                        if (label == null)
                        {
                            label = new TextBlock();
                            PlotCanvas.Children.Add(label);
                        }
                        label.Text = d.Label;
                        label.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                        Canvas.SetLeft(label, x + 10);
                        Canvas.SetTop(label, y - 10);
                    }

                }

                foreach (LineData line in this.lines)
                {
                    double x1 = ((line.X1 - minX) * width / rangeX);
                    double y1 = availableHeight - ((line.Y1 - minY) * availableHeight / rangeY);
                    double x2 = ((line.X2 - minX) * width / rangeX);
                    double y2 = availableHeight - ((line.Y2 - minY) * availableHeight / rangeY);

                    Line lineShape = null;

                    if (lineShapes.Count > 0)
                    {
                        lineShape = lineShapes[0];
                        lineShapes.RemoveAt(0);
                    }
                    if (lineShape == null)
                    {
                        lineShape = new Line();
                        PlotCanvas.Children.Add(lineShape);
                    }

                    lineShape.StrokeThickness = size / 2;
                    lineShape.Stroke = line.Color;
                    lineShape.X1 = x1;
                    lineShape.Y1 = y1;
                    lineShape.X2 = x2;
                    lineShape.Y2 = y2;

                }
            }

            // remove unused elements.
            foreach (var e in dots)
            {
                PlotCanvas.Children.Remove(e);
            }
            foreach (var e in lineShapes)
            {
                PlotCanvas.Children.Remove(e);
            }
            foreach (var e in labels)
            {
                PlotCanvas.Children.Remove(e);
            }
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            if (Selected != null)
            {
                DataValue dv = FindDataValueAt(GetMousePositionInHostCoordinate());
                if (dv != null)
                {
                    Selected(this, dv);
                }
            }

            base.OnPointerPressed(e);
        }

        private Point GetMousePositionInHostCoordinate()
        {
            var w = Window.Current.CoreWindow;
            Point pos = w.PointerPosition;

            Point hostCoordinates = new Point(pos.X - w.Bounds.Left, pos.Y - w.Bounds.Top);
            return hostCoordinates;
        }

        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            DataValue dv = FindDataValueAt(GetMousePositionInHostCoordinate());

            if (dv != null)
            {
                ShowToolTip(e.GetCurrentPoint(PlotCanvas).Position, dv.Y.ToString("N2"));
            }
            else
            {
                HideToolTip();
            }

            base.OnPointerMoved(e);
        }

        private DataValue FindDataValueAt(Point pos)
        {
            DataValue dv = null;

            foreach (double s in new double[] { 1, 2, 3, 5 })
            {
                double s2 = s * 2;
                foreach (UIElement element in VisualTreeHelper.FindElementsInHostCoordinates(new Rect(pos.X - s, pos.Y - s, s2, s2), PlotCanvas, true))
                {
                    Ellipse dot = element as Ellipse;
                    if (dot != null)
                    {
                        dv = (DataValue)dot.Tag;
                        break;
                    }
                }
            }
            return dv;
        }

        private void HideToolTip()
        {
            if (tooltip != null)
            {
                PlotCanvas.Children.Remove(tooltip);
                tooltip = null;
            }
        }

        Border tooltip;

        private void ShowToolTip(Point pos, string text)
        {
            if (tooltip == null)
            {
                tooltip = new Border() { Background = new SolidColorBrush(Colors.LemonChiffon), BorderBrush = new SolidColorBrush(Colors.Maroon), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(2), Padding = new Thickness(5) };
                tooltip.Child = new TextBlock() { Foreground = new SolidColorBrush(Colors.Maroon) };
                PlotCanvas.Children.Add(tooltip);
            }
            TextBlock label = (TextBlock)tooltip.Child;
            label.Text = text;
            tooltip.UpdateLayout();

            double x = pos.X;
            if (x + tooltip.DesiredSize.Width > PlotCanvas.ActualWidth)
            {
                x = PlotCanvas.ActualWidth - tooltip.DesiredSize.Width;
            }
            Canvas.SetLeft(tooltip, x);

            double y = pos.Y - 16 - tooltip.DesiredSize.Height;
            if (y < 0)
            {
                y = pos.Y + 16;
            }
            Canvas.SetTop(tooltip, y);

        }

        public void AddLine(LineData line)
        {
            lines.Add(line);
            UpdateChart();
        }

        public void RemoveLine(LineData line)
        {
            lines.Remove(line);
            UpdateChart();
        }


        internal void Clear()
        {
            PlotCanvas.Children.Clear();
            lines.Clear();
        }

    }
}
