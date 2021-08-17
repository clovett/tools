using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.Windows.Threading;

namespace MyGarden
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer timer;
        int startTime;
        bool left;
        Trunk main;

        public MainWindow()
        {
            InitializeComponent();
            PlantTree();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                Clear();
                PlantTree();
            }
            base.OnKeyDown(e);
        }

        private void Clear()
        {
            RootContent.Children.Clear();
        }

        void PlantTree()
        {
            main = GrowTrunk(new Point(200, 800), new Point(200, 50), 20);

            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(600), DispatcherPriority.Normal, OnTick, this.Dispatcher);
            timer.Start();
            startTime = Environment.TickCount;

            var a = new DoubleAnimation(0.1, 1, new Duration(TimeSpan.FromSeconds(5)));

            a.Completed += (s, e) =>
            {
                timer.Stop();
            };

            this.BeginAnimation(NextLeafPositionProperty, a);
        }


        public double NextLeafPosition
        {
            get { return (double)GetValue(NextLeafPositionProperty); }
            set { SetValue(NextLeafPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NextLeafPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NextLeafPositionProperty =
            DependencyProperty.Register("NextLeafPosition", typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

        public void OnTick(object sender, EventArgs e)
        {
            if (left)
            {
                GrowLeaf(main, new Vector(100, -100), 1, NextLeafPosition, 25, 0.2, 0.25);
            }
            else
            {
                GrowLeaf(main, new Vector(-100, -100), 1, NextLeafPosition, 25, 0.2, 0.25);
            }

            left = !left;

            
        }

        private Trunk GrowTrunk(Point basePt, Point endPt, double curviness)
        {
            // <local:Trunk x:Name="Trunk" Stroke="Brown" StrokeThickness="2" BasePosition="200,300" TipPosition="200,100" Curviness="20"/>
            Trunk trunk = new Trunk();
            RootContent.Children.Add(trunk);

            trunk.Stroke = Brushes.Brown;
            trunk.StrokeThickness = 2;
            trunk.BasePosition = basePt;
            trunk.TipPosition = endPt;
            trunk.Curviness = curviness;

            Duration d = new Duration(TimeSpan.FromSeconds(5)); 

            trunk.BeginAnimation(Trunk.TrunkHeightProperty, new DoubleAnimation(0, 1, d));

            return trunk;
        }

        void GrowLeaf(Trunk trunk, Vector tipEnd, double trunkStartPosition, double trunkEndPosition, double width, double curviness, double pointiness)
        {
            Duration d = new Duration(TimeSpan.FromSeconds(5));

            Leaf leaf = new Leaf();
            leaf.Trunk = trunk;

            RootContent.Children.Add(leaf);


            leaf.BeginAnimation(Leaf.PositionOnTrunkProperty, new DoubleAnimation(trunkStartPosition, trunkEndPosition, d) { 
                EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut, Exponent = 3 } });
            leaf.BeginAnimation(Leaf.SpineProperty, new VectorAnimation(new Vector(0, 0), tipEnd, d));
            leaf.BeginAnimation(Leaf.LeafWidthProperty, new DoubleAnimation(1, 25, d) { EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseIn, Exponent = 1.2 } });
            leaf.BeginAnimation(Leaf.CurvinessProperty, new DoubleAnimation(0, curviness, d));
            leaf.BeginAnimation(Leaf.PointinessProperty, new DoubleAnimation(0, 0.25, d));

            SolidColorBrush brush = new SolidColorBrush(Colors.LightGreen);
            leaf.Path.Fill = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.LightGreen, Colors.Green, d));

            SolidColorBrush stroke = new SolidColorBrush(Colors.PaleGreen);
            leaf.Path.Stroke = stroke;
            leaf.StrokeThickness = 1;
            stroke.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Green, Colors.Green, d));
        }

        void ShowControlPoints(Path p, Point offset)
        {
            Transform t = p.RenderTransform;
            PathGeometry g = (PathGeometry)p.Data;
            int i = 0;
            foreach (PathFigure f in g.Figures)
            {
                ShowPoint(i++, f.StartPoint, offset, t);
                foreach (PathSegment s in f.Segments)
                {                    
                    LineSegment ls = s as LineSegment;
                    if (ls != null)
                    {
                        ShowPoint(i++, ls.Point ,offset, t);
                    }
                    else
                    {
                        BezierSegment bs = s as BezierSegment;
                        if (bs != null)
                        {
                            ShowPoint(i++, bs.Point1, offset, t);
                            ShowPoint(i++, bs.Point2  , offset, t);
                            ShowPoint(i++, bs.Point3, offset, t);
                        }
                        else
                        {
                            PolyBezierSegment ps = s as PolyBezierSegment;
                            if (ps != null)
                            {
                                foreach (Point pt in ps.Points)
                                {
                                    ShowPoint(i++, pt, offset, t);
                                }
                            }
                        }
                    }
                }
            }
        }

        void ShowPoint(int i, Point pt, Point offset, Transform rt) 
        {
            Ellipse r = new Ellipse();
            r.Fill = Brushes.Red;
            r.Width = 5;
            r.Height = 5;
            Canvas.SetLeft(r, pt.X + offset.X);
            Canvas.SetTop(r, pt.Y + offset.Y);
            r.RenderTransform = rt;
            RootContent.Children.Add(r);

            TextBlock text = new TextBlock();
            text.Text = i.ToString();
            Canvas.SetLeft(text, pt.X + 5 + offset.X);
            Canvas.SetTop(text, pt.Y + offset.Y);
            text.RenderTransform = rt;
            RootContent.Children.Add(text);

        }
    }
}
