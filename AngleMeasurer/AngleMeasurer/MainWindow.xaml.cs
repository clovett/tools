using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AngleMeasurer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly static RoutedUICommand CommandClear = new RoutedUICommand("F5", "Clear", typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();
        }

        Line current;
        Line angular;
        Path arc;
        ArcSegment arcSegment;
        TextBlock label;

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (angular != null)
            {
                // done with this one
                angular = null;
                label = null;
                arc = null;
                arcSegment = null;
                current = null;
                return;
            }

            Point pos = e.GetPosition(Scratch);
            current = new Line() { Stroke = Brushes.Red, StrokeThickness = 1.0, X1 = pos.X, Y1 = pos.Y, X2 = pos.X, Y2 = pos.Y };
            Scratch.Children.Add(current);
            Scratch.CaptureMouse();
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(Scratch);
            if (angular != null)
            {
                angular.X2 = pos.X;
                angular.Y2 = pos.Y;

                Point start = new Point(current.X1, current.Y1);

                Point arcStart = new Point(current.X1 + (current.X2 - current.X1) / 2, current.Y1 + (current.Y2 - current.Y1) / 2);

                Vector v1 = new Vector(arcStart.X - current.X1, arcStart.Y - current.Y1);
                Vector v2 = new Vector(angular.X2 - angular.X1, angular.Y2 - angular.Y1);
                double angle = Vector.AngleBetween(v1, v2);
                label.Text = angle.ToString();

                v2.Normalize();
                v2 *= v1.Length;
                Point point = start + v2;
                Debug.WriteLine("new vector {0},{1}", v2.X, v2.Y);
                arcSegment.Point = point;
                arcSegment.SweepDirection = (angle > 0) ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;

                try
                {
                    PathGeometry g = (PathGeometry)arc.Data;
                    Point tangent;
                    g.GetPointAtFractionLength(0.5, out point, out tangent);

                    Canvas.SetLeft(label, point.X);
                    Canvas.SetTop(label, point.Y);
                }
                catch
                {

                }

            }
            else if (current != null)
            {
                Debug.WriteLine("current moving to {0},{1}", pos.X, pos.Y);
                current.X2 = pos.X;
                current.Y2 = pos.Y;
            }

        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (current == null)
            {
                return;
            }
            Point pos = e.GetPosition(Scratch);
            angular = new Line() { Stroke = Brushes.Red, StrokeThickness = 1.0, X1 = current.X1, Y1 = current.Y1, X2 = pos.X, Y2 = pos.Y };
            Scratch.Children.Add(angular);

            PathFigure arcFigure = new PathFigure();
            Vector size = new Vector((current.X2 - current.X1) / 2, (current.Y2 - current.Y1) / 2);
            Point arcStart = new Point(current.X1 + size.X, current.Y1 + size.Y);
            arcFigure.StartPoint = arcStart;
            double length = size.Length;
            arcSegment = new ArcSegment(arcStart, new Size(length * 2, length * 2), 0, false, SweepDirection.Clockwise, true);
            arcFigure.Segments.Add(arcSegment);
            arc = new Path()
            {
                Data = new PathGeometry(new PathFigure[] { arcFigure }),
                Stroke = Brushes.Red,
                StrokeThickness = 1.0
            };
            Scratch.Children.Add(arc);

            label = new TextBlock()
            {
                Foreground = Brushes.Red,
                Text = "0",
                Background = Brushes.White
            };
            Debug.WriteLine("Arm vector is {0},{1}", size.X, size.Y);
            Canvas.SetLeft(label, arcStart.X);
            Canvas.SetTop(label, arcStart.Y);
            Scratch.Children.Add(label);
        }

        private void OnCanvasLostCapture(object sender, MouseEventArgs e)
        {
        }

        private void OnCanvasKeyDown(object sender, KeyEventArgs e)
        {
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Scratch.Children.Clear();
                current = null;
                angular = null;
                arc = null;
                arcSegment = null;
                label = null;
            }
        }

        private void Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    ImageHolder.Source = image;
                }
            }
        }

        private void ClipboardHasData(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsImage() ||
                Clipboard.ContainsData(DataFormats.Xaml) ||
                Clipboard.ContainsData(DataFormats.Rtf) ||
                Clipboard.ContainsData(DataFormats.Text);
        }

        private void OnClear(object sender, ExecutedRoutedEventArgs e)
        {
            Scratch.Children.Clear();
        }
    }
}
