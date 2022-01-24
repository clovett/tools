using LovettSoftware.Utilities;
using ModernWpf.Controls;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Effects;

namespace WpfAppTemplate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DelayedActions delayedActions = new DelayedActions();

        public MainWindow()
        {
            InitializeComponent();
            RestoreSettings();

            UiDispatcher.Initialize();

            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;

            Settings.Instance.PropertyChanged += OnSettingsChanged;
        }

        private void OnOpen(object sender, ExecutedRoutedEventArgs e)
        {
            OpenDialog();
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            OpenDialog();
        }

        private void OpenDialog() { 
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "Image files|*.jpg;*,png;*.bmp";
            fd.CheckFileExists = true;
            fd.Multiselect = false;
            if (fd.ShowDialog() == true)
            {
                Open(fd.FileName);
            }
        }

        Line line;

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this.ImageHolder);
            if (line == null)
            {
                line = new Line() { Stroke = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0)), StrokeThickness = 2 };
                line.X1 = pos.X;
                line.Y1 = pos.Y;
                line.X2 = pos.X;
                line.Y2 = pos.Y;
                LightningCanvas.Children.Add(line);
            }
            LightningCanvas.CaptureMouse();
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (line != null)
            {
                var pos = e.GetPosition(this.ImageHolder);
                line.X2 = pos.X;
                line.Y2 = pos.Y;
            }
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (line != null)
            {
                AddLightning(line.X1, line.Y1, line.X2, line.Y2);
                LightningCanvas.Children.Remove(line);
                line = null;
            }
            LightningCanvas.ReleaseMouseCapture();
        }

        Random rand = new Random(Environment.TickCount);
        List<Point[]> branches = new List<Point[]>();
        double thickness;
        byte a = 0x80;
        byte r = 0xff;
        byte g = 0xff;
        byte b = 0xff;

        private void AddLightning(double x1, double y1, double x2, double y2)
        {
            a = 0x80;
            r = 0xff;
            g = 0xff;
            b = 0xff;
            branches = new List<Point[]>();
            thickness = 5;

            var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            var branch = AddLightningTrunk(x1, y1, x2, y2, 100, brush, thickness);
            branches.Add(branch);
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(AddFork));
        }

        void AddFork()
        {          
            a -= 5;
            r -= 10;
            g -= 20;
            foreach(var fork in branches.ToArray())
            {
                int last = fork.Length * 3 / 4;
                int step = (last / 5); // create 5 forks
                if (step <= 1)
                {
                    break;
                }
                int count = 0;
                for (int x = 0; count < 5; x += rand.Next((step * 3) / 4, step) + 1, count++)
                {
                    Point end = fork[x + step];
                    var subfork = AddLightningFork(fork[x], end, fork.Length / 2, new SolidColorBrush(Color.FromArgb(a, r, g, b)), thickness);
                    branches.Add(subfork);
                }
            }
            thickness--;
            if (thickness > 0)
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(AddFork));
            }
        }

        private Point[] AddLightningFork(Point start, Point end, int numPoints, SolidColorBrush brush, double thickness)
        {
            Vector v = new Vector(end.X - start.X, end.Y - start.Y);
            var length = v.Length;
            var points = new List<Point>();
            var len = length / numPoints;
            double angle = Math.Atan2(v.Y, v.X);
            for (int i = 1; i < numPoints; i++)
            {
                points.Add(start);
                angle = GetRandomAngle(angle);
                end.X = start.X + len * Math.Cos(angle);
                end.Y = start.Y + len * Math.Sin(angle);
                // v = new Vector(end.X - start.X, end.Y - start.Y);
                start = end;
            }

            Polyline poly = new Polyline() { Points = new PointCollection(points), Stroke = brush, StrokeThickness = thickness };
            poly.Effect = new BlurEffect() { KernelType = KernelType.Gaussian, Radius = thickness};
            LightningCanvas.Children.Add(poly);
            return points.ToArray();
        }

        private Point[] AddLightningTrunk(double x1, double y1, double x2, double y2, int numPoints, Brush brush, double thickness)
        { 
            var points = new List<Point>();
            // always start the lightning at the start point.
            var start = new Point(x1, y1);
            for (int i = 1; i <= numPoints; i++)
            {
                // point towards the end point.
                points.Add(start);
                Vector v = new Vector(x2 - start.X, y2 - start.Y);
                var len = v.Length * i / numPoints;
                double angle = Math.Atan2(v.Y, v.X);
                if (i < numPoints)
                {
                    // purturb the angle of this vector by a random amount +/- 30 degrees.
                    angle = GetRandomAngle(angle);
                }
                start.X = start.X + len * Math.Cos(angle);
                start.Y = start.Y + len * Math.Sin(angle);
            }

            Polyline poly = new Polyline() { Points = new PointCollection(points), Stroke = brush, StrokeThickness = thickness };
            poly.Effect = new BlurEffect() { KernelType = KernelType.Gaussian, Radius = thickness };
            LightningCanvas.Children.Add(poly);
            return points.ToArray();
        }

        private double GetRandomAngle(double angle, double delta = 30)
        {
            angle = angle * 180 / Math.PI;
            var perturbation = (rand.NextDouble() - 0.5) * (delta * 2);
            return (angle + perturbation) * Math.PI / 180.0;
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (line != null)
            {
                LightningCanvas.Children.Remove(line);
                line = null;
            }
            base.OnLostMouseCapture(e);
        }

        private void Open(string fileName)
        {
            ImageHolder.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath(fileName)));
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            LightningCanvas.Children.Clear();
        }

        private void OnSettings(object sender, RoutedEventArgs e)
        {
            XamlExtensions.Flyout(AppSettingsPanel);
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            var bounds = this.RestoreBounds;
            Settings.Instance.WindowLocation = bounds.TopLeft;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var bounds = this.RestoreBounds;
            Settings.Instance.WindowSize = bounds.Size;
        }

        private void RestoreSettings()
        {
            Settings settings = Settings.Instance;
            if (settings.WindowLocation.X != 0 && settings.WindowSize.Width != 0 && settings.WindowSize.Height != 0)
            {
                // make sure it is visible on the user's current screen configuration.
                var bounds = new System.Drawing.Rectangle(
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.X),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowLocation.Y),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Width),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(settings.WindowSize.Height));
                var screen = System.Windows.Forms.Screen.FromRectangle(bounds);
                bounds.Intersect(screen.WorkingArea);

                this.Left = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.X);
                this.Top = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Y);
                this.Width = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Width);
                this.Height = XamlExtensions.ConvertToDeviceIndependentPixels(bounds.Height);
            }

            UpdateTheme();

            this.Visibility = Visibility.Visible;
        }

        void UpdateTheme()
        {
            var theme = ModernWpf.ApplicationTheme.Light;
            switch (Settings.Instance.Theme)
            {
                case AppTheme.Dark:
                    theme = ModernWpf.ApplicationTheme.Dark;
                    break;
                default:
                    break;
            }
            ModernWpf.ThemeManager.Current.ApplicationTheme = theme;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Theme")
            {
                UpdateTheme();
            }
            saveSettingsPending = true;
            delayedActions.StartDelayedAction("SaveSettings", OnSaveSettings, TimeSpan.FromSeconds(2));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (saveSettingsPending)
            {
                // then we need to do synchronous save and cancel any delayed action.
                OnSaveSettings();
            }

            base.OnClosing(e);
        }

        void OnSaveSettings()
        {
            if (saveSettingsPending)
            {
                saveSettingsPending = false;
                delayedActions.CancelDelayedAction("SaveSettings");
                if (Settings.Instance != null)
                {
                    try
                    {
                        Settings.Instance.Save();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private bool saveSettingsPending;

    }
}
