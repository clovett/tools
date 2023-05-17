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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;

namespace WpfGifBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DelayedActions delayedActions = new DelayedActions();
        AnimatedGif gif = new AnimatedGif();

        public MainWindow()
        {
            InitializeComponent();
            RestoreSettings();

            UiDispatcher.Initialize();

            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;

            Settings.Instance.PropertyChanged += OnSettingsChanged;
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog od = new OpenFileDialog();
            od.Filter = "App Files (*.*)|*.*";
            od.CheckFileExists = true;
            od.Multiselect = false;
            if (od.ShowDialog() == true)
            {
                OpenGif(od.FileName);
            }
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

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            Play();
        }

        void Play() 
        { 
            if (gif == null)
            {
                return;
            }

            delayedActions.CancelDelayedAction("render");
            ThumbnailPanel.Children.Clear();
            image = new Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Margin = new Thickness(10);
            ThumbnailPanel.Children.Add(image);

            RenderNextFrame();
        }

        Image image;

        private void OpenGif(string file)
        {
            gif = new AnimatedGif();
            gif.ReadMetadata(file);
            this.Play();
        }

        private void RenderNextFrame()
        {
            if (gif == null)
            {
                return;
            }
            var frame = gif.GetNextFrame();
            if (frame != null)
            {
                image.Source = frame.Bitmap;
                image.Width = frame.Bitmap.Width;
                image.Height = frame.Bitmap.Height;
                delayedActions.StartDelayedAction("render", RenderNextFrame, TimeSpan.FromMilliseconds(frame.Delay));
            }
        }


        private void OnEdit(object sender, RoutedEventArgs e)
        {
            if (gif == null)
            {
                return;
            }
            delayedActions.CancelDelayedAction("render");
            ThumbnailPanel.Children.Clear();

            var size = this.gif.Size;
            foreach (var frame in gif.OriginalFrames)
            {
                Image image = new Image();
                image.Margin = new Thickness(10);
                image.Source = frame;
                image.Width = size.Width;
                image.Height = size.Height;
                ThumbnailPanel.Children.Add(image);
            }
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            if (gif != null)
            {
                SaveFileDialog sd = new SaveFileDialog();
                if (sd.ShowDialog() == true)
                {
                    gif.SaveToFile(sd.FileName);
                }
            }
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            this.Clear();
        }

        void Clear() 
        { 
            delayedActions.CancelDelayedAction("render");
            ThumbnailPanel.Children.Clear();
            image = null;
            gif = null;
        }

    }
}
