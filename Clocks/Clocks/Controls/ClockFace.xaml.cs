using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Clocks.Controls
{
    /// <summary>
    /// Interaction logic for ClockFace.xaml
    /// </summary>
    public partial class ClockFace : UserControl
    {
        List<Line> hourNotches = new List<Line>();
        List<Line> minuteNotches = new List<Line>();
        List<Label> hourLabels = new List<Label>();
        Line hourHand;
        Line minuteHand;
        Line secondHand;
        Ellipse pivot;

        public ClockFace()
        {
            InitializeComponent();
        }

        const double HourNotchSize = 1 / 30.0; // 1/30th of the face size.
        const double HourNotchThickness = 3;
        const double MinuteNotchSize = 1 / 50.0; // 1/50th of the face size.
        const double MinuteNotchThickness = 1;

        const double HourHandThickness = 10;
        const double MinuteHandThickness = 5;
        const double SecondHandThickness = 2;
        const double HourHandLength = 50; // percent of radius
        const double MinuteHandLength = 70;
        const double SecondHandLength = 90;
        const double PivotSize = 24;

        static Brush HourHandBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x50));
        static Brush MinuteHandBrush = new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x90));
        static Brush SecondHandBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xF0));
        static Brush PivotBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x50));

        Size gridSize;

        private void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            gridSize = e.NewSize;
            LayoutHands();
        }

        void LayoutHands()
        {
            Size s = gridSize;
            double faceSize = Math.Min(s.Width, s.Height);
            ClockEllipse.Width = ClockEllipse.Height = faceSize;
            Canvas.SetTop(ClockEllipse, (s.Height - faceSize) / 2);
            Canvas.SetLeft(ClockEllipse, (s.Width - faceSize) / 2);

            if (hourNotches.Count == 0)
            {
                hourHand = new Line() { Stroke = HourHandBrush, StrokeThickness = HourHandThickness, StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap =  PenLineCap.Round };
                ClockCanvas.Children.Add(hourHand);

                minuteHand = new Line() { Stroke = MinuteHandBrush, StrokeThickness = MinuteHandThickness, StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round };
                ClockCanvas.Children.Add(minuteHand);

                secondHand = new Line() { Stroke = SecondHandBrush, StrokeThickness = SecondHandThickness, StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round };
                ClockCanvas.Children.Add(secondHand);

                pivot = new Ellipse() { Width = PivotSize, Height = PivotSize, Fill = PivotBrush };
                ClockCanvas.Children.Add(pivot);
                
                for (var i = 0; i < 12; i++)
                {
                    var notch = new Line() { Stroke = Brushes.White, StrokeThickness = HourNotchThickness, StrokeEndLineCap = PenLineCap.Round };
                    hourNotches.Add(notch);
                    ClockCanvas.Children.Add(notch);

                    var label = new Label() { Foreground = Brushes.White, Content = (i == 0 ? "12" : i.ToString()) };
                    hourLabels.Add(label);
                    ClockCanvas.Children.Add(label);
                }
                for (var i = 0; i < 60; i++)
                {
                    var notch = new Line() { Stroke = Brushes.White, StrokeThickness = MinuteNotchThickness, StrokeEndLineCap = PenLineCap.Round };
                    minuteNotches.Add(notch);
                    ClockCanvas.Children.Add(notch);
                }
            }

            Point center = new Point(s.Width / 2, s.Height / 2);

            var radius = faceSize / 2;
            Canvas.SetLeft(pivot, center.X - pivot.Width / 2);
            Canvas.SetTop(pivot, center.Y - pivot.Height / 2);

            for (var i = 0; i < 12; i++)
            {
                Line notch = hourNotches[i];
                var angle = (double)i * 360.0 / 12;
                RotateTransform rotate = new RotateTransform(angle, center.X, center.Y);
                Point p1 = rotate.Transform(new Point(center.X, center.Y - radius));
                Point p2 = rotate.Transform(new Point(center.X, center.Y - radius + faceSize * HourNotchSize));
                notch.X1 = p1.X;
                notch.Y1 = p1.Y;
                notch.X2 = p2.X;
                notch.Y2 = p2.Y;

                Label label = hourLabels[i];
                label.UpdateLayout();

                Point p3 = rotate.Transform(new Point(center.X, 
                                            center.Y - radius + faceSize * HourNotchSize + (label.DesiredSize.Height / 2)));
                Canvas.SetLeft(label, p3.X - (label.DesiredSize.Width / 2));
                Canvas.SetTop(label, p3.Y - (label.DesiredSize.Height / 2));

            }

            for (var i = 0; i < 60; i++)
            {
                Line notch = minuteNotches[i];
                var angle = (double)i * 360.0 / 60;
                RotateTransform rotate = new RotateTransform(angle, center.X, center.Y);
                Point p1 = rotate.Transform(new Point(center.X, center.Y - radius));
                Point p2 = rotate.Transform(new Point(center.X, center.Y - radius + faceSize * MinuteNotchSize));
                notch.X1 = p1.X;
                notch.Y1 = p1.Y;
                notch.X2 = p2.X;
                notch.Y2 = p2.Y;
            }

            double seconds = (Time.Hour * 3600) + (Time.Minute * 60) + Time.Second;
            
            RotateHand(hourHand, seconds * 360.0 / (12*3600.0), HourHandLength);
            RotateHand(minuteHand, seconds * 360.0 / 3600.0, MinuteHandLength);
            RotateHand(secondHand, seconds * 360.0 / 60.0, SecondHandLength);
        }

        DateTime time;

        public DateTime Time {
            get { return this.time; }
            set { this.time = value;
                if (ClockEllipse != null)
                {
                    LayoutHands();
                }
            }
        }

        void RotateHand(Line hand, double angle, double length)
        {
            angle = angle % 360;
            var radius = ClockEllipse.Width / 2;
            Size s = gridSize;
            Point center = new Point(s.Width / 2, s.Height / 2);
            RotateTransform rotate = new RotateTransform(angle, center.X, center.Y);
            Point p1 = rotate.Transform(new Point(center.X, center.Y));
            Point p2 = new Point(center.X, center.Y - (radius * length / 100));
            p2 = rotate.Transform(p2);
            hand.X1 = p1.X;
            hand.Y1 = p1.Y;
            hand.X2 = p2.X;
            hand.Y2 = p2.Y;
        }
    }
}
