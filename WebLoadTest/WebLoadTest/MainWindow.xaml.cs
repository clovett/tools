using LovettSoftware.Utilities;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebLoadTest.Controls;
using WebLoadTest.Utilities;

namespace WebLoadTest
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

            UiDispatcher.Initialize();

            RestoreSettings();

            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {

        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            if (test != null)
            {
                test.Stop();
                Chart.SetData(null);
                test.DataPoint -= OnDataPoint;
                test = null;
            }
        }

        private void OnSettings(object sender, RoutedEventArgs e)
        {
            XamlExtensions.Flyout(AppSettingsPanel);
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            delayedActions.StartDelayedAction("UpdateSettings", UpdateSettings, TimeSpan.FromMilliseconds(1000));
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            delayedActions.StartDelayedAction("UpdateSettings", UpdateSettings, TimeSpan.FromMilliseconds(1000));
        }

        private async void RestoreSettings()
        {
            Settings settings = await ((App)App.Current).LoadSettings();
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
            this.Visibility = Visibility.Visible;
            TextBoxUrl.Text = "" + settings.LastLocation;
        }

        async void UpdateSettings()
        {
            var bounds = this.RestoreBounds;

            Settings settings = ((App)App.Current).Settings;
            settings.WindowLocation = bounds.TopLeft;
            settings.WindowSize = bounds.Size;
            await settings.SaveAsync();
        }

        LoadTest test;
        DataSeries series;
        long next;

        private void OnRecord(object sender, RoutedEventArgs e)
        {
            if (test == null)
            {
                series = new DataSeries() { Name = "ResponseTimes", Values = new List<DataValue>() };
                test = new LoadTest();
                test.DataPoint += OnDataPoint;
            }
            next = 0;
            if (!string.IsNullOrEmpty(this.TextBoxUrl.Text))
            {
                var settings = ((App)App.Current).Settings;
                try
                {
                    test.Start(new Uri(this.TextBoxUrl.Text));
                    settings.LastLocation = TextBoxUrl.Text;
                    delayedActions.StartDelayedAction("UpdateSettings", UpdateSettings, TimeSpan.FromMilliseconds(10));
                } 
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Invalid URL");
                    this.TextBoxUrl.SelectAll();
                    this.TextBoxUrl.Focus();
                }
            }

        }

        private void OnDataPoint(object sender, TestResult e)
        {
            if (e.ResponseTimes != null && this.test != null)
            {
                delayedActions.StartDelayedAction("AddPoint", () =>
                {
                    foreach (var i in e.ResponseTimes)
                    {
                        double milliseconds = (double)i / 1000000;
                        series.Values.Add(new DataValue() { X = next++, Y = milliseconds });
                    }
                    if (series.Values.Count > 500)
                    {
                        series.Values.RemoveRange(0, series.Values.Count - 500);
                    }
                    Chart.SetData(series);
                }, TimeSpan.FromMilliseconds(10));
            }
        }
    }
}
