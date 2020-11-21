using LovettSoftware.Utilities;
using System;
using System.Windows;

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
            RestoreSettings();

            InitializeComponent();

            UiDispatcher.Initialize();

            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {

        }

        private void OnClear(object sender, RoutedEventArgs e)
        {

        }

        private void OnSettings(object sender, RoutedEventArgs e)
        {
            XamlExtensions.Flyout(AppSettingsPanel);
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            delayedActions.StartDelayedAction("SaveWindowLocation", SavePosition, TimeSpan.FromMilliseconds(1000));
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            delayedActions.StartDelayedAction("SaveWindowLocation", SavePosition, TimeSpan.FromMilliseconds(1000));
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
        }

        async void SavePosition()
        {
            var bounds = this.RestoreBounds;

            Settings settings = await ((App)App.Current).LoadSettings();
            settings.WindowLocation = bounds.TopLeft;
            settings.WindowSize = bounds.Size;
            await settings.SaveAsync();
        }
    }
}
