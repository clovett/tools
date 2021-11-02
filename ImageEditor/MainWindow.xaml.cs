using LovettSoftware.Utilities;
using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using ImageEditor.Utilities;
using System.Windows.Media;
using System.IO;

namespace ImageEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DelayedActions delayedActions = new DelayedActions();
        string fileName;

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
            Microsoft.Win32.OpenFileDialog od = new OpenFileDialog();
            od.Filter = "Image Files (*.gif,*.png,*.jpg,*.bmp)|*.gif;*.png;*.jpg;*.bmp";
            od.CheckFileExists = true;
            if (od.ShowDialog() == true)
            {
                OpenImage(od.FileName);
            }
        }

        private void OpenImage(string fileName)
        {
            this.fileName = fileName;
            Uri uri = new Uri(fileName);
            var img = new BitmapImage(uri);
            if (img.Format != PixelFormats.Bgra32)
            {
                FormatConvertedBitmap formatted = new FormatConvertedBitmap();
                formatted.BeginInit();
                formatted.Source = img;
                formatted.DestinationFormat = PixelFormats.Bgra32;
                formatted.EndInit();
                ImageHolder.Source = formatted;
            }
            else
            {
                ImageHolder.Source = img;
            }
            utils = null;
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            ImageHolder.Source = null;
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
        private ImageUtils utils;

        private void OnSmoothing(object sender, RoutedEventArgs e)
        {
            if (utils == null && ImageHolder.Source is BitmapImage bitmap)
            {
                utils = new ImageUtils(bitmap);
            }
            if (utils != null) { 
                utils.SmoothEdges();
                ImageHolder.Source = utils.GetUpdated();
            }
        }

        private void OnSaveFile(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog od = new SaveFileDialog();
                od.Filter = "Image Files (*.gif,*.png,*.jpg,*.bmp)|*.gif;*.png;*.jpg;*.bmp";
                od.CheckPathExists = true;
                od.FileName = this.fileName;
                if (od.ShowDialog() == true)
                {
                    SaveImage(od.FileName);
                }
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveImage(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && ImageHolder.Source is BitmapSource bitmap)
            {
                BitmapEncoder encoder = null;
                switch (System.IO.Path.GetExtension(this.fileName).ToLowerInvariant())
                {
                    case ".png":
                        encoder = new PngBitmapEncoder();
                        break;
                    case ".gif":
                        encoder = new GifBitmapEncoder();
                        break;
                    case ".jpg":
                        encoder = new JpegBitmapEncoder();
                        break;
                    case ".bmp":
                        encoder = new BmpBitmapEncoder();
                        break;
                }
                if (encoder != null)
                {
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        encoder.Save(stream);
                    }
                }
            }
        }
    }
}
