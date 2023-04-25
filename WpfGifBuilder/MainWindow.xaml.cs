using LovettSoftware.Utilities;
using Microsoft.Win32;
using ModernWpf.Controls;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfGifBuilder.Utilities;
using System.Security.Policy;

namespace WpfGifBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DelayedActions delayedActions = new DelayedActions();
        List<BitmapFrame> frames = new List<BitmapFrame>();

        public MainWindow()
        {
            InitializeComponent();
            RestoreSettings();

            UiDispatcher.Initialize();

            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;

            Settings.Instance.PropertyChanged += OnSettingsChanged;
        }

        private void OnAddFiles(object sender, RoutedEventArgs e)
        {
            OpenFileDialog od = new OpenFileDialog();
            od.Filter = "App Files (*.*)|*.*";
            od.CheckFileExists = true;
            od.Multiselect = true;
            if (od.ShowDialog() == true)
            {
                foreach (var file in od.FileNames)
                {
                    AddGifFrame(file);
                }
            }
        }

        private void AddGifFrame(string file)
        {

            BitmapDecoder bitmapDecoder = BitmapDecoder.Create(new Uri(file), BitmapCreateOptions.None, BitmapCacheOption.None);
            foreach (var frame in bitmapDecoder.Frames)
            {
                Image image = new Image();
                var snapshot = BitmapFrame.Create(frame);
                image.Source = snapshot;
                image.Width = 200;
                image.Margin = new Thickness(10);
                frames.Add(snapshot);
                ThumbnailPanel.Children.Add(image);
            }
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            ThumbnailPanel.Children.Clear();
            frames.Clear();
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

        private void OnBuild(object sender, RoutedEventArgs e)
        {
            FilmStrip film = new FilmStrip();
            foreach (var frame in this.frames)
            {                
                film.AddFrame(frame);
            }

            SaveFileDialog sd = new SaveFileDialog();
            if (sd.ShowDialog() == true)
            {
                film.SaveToFile(sd.FileName);
            }
        }

        private void OnEdit(object sender, RoutedEventArgs e)
        {

        }
    }
}
