using System;
using System.Collections.Generic;
using System.IO;
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
using VideoThumbnailMaker.Utilities;

namespace VideoThumbnailMaker
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

            Microsoft.Win32.OpenFileDialog fo = new Microsoft.Win32.OpenFileDialog();
            fo.Filter = "mp4 files (*.mp4)|*.mp4";
            fo.CheckFileExists = true;
            fo.Multiselect = false;
            if (Settings.Instance.LastFile != null)
            {
                fo.InitialDirectory = System.IO.Path.GetDirectoryName(Settings.Instance.LastFile);
            }
            if (fo.ShowDialog() == true)
            {
                OpenFile(fo.FileName);
            }
            OpenButton.IsEnabled = true;
        }

        private void OpenFile(string fileName)
        {
            Settings.Instance.LastFile = fileName;
            synching = true;
            Player.LoadedBehavior = MediaState.Play;
            Player.Source = new Uri(fileName);
            VideoSlider.Value = 0;
            synching = false;
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            Player.Source = null;
        }

        private void OnSettings(object sender, RoutedEventArgs e)
        {
            XamlExtensions.Flyout(AppSettingsPanel);
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            SavePosition();
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SavePosition();
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
            settings.PropertyChanged += OnSettingsChanged;
            if (settings.LastFile != null)
            {
                Player.Source = new Uri(settings.LastFile);
            }
            this.Visibility = Visibility.Visible;
            ThumbColumn.Width = new GridLength(Settings.Instance.ThumbnailWidth + 20);
            this.loaded = true;
        }

        bool loaded;

        void SavePosition()
        {
            if (loaded)
            {
                var bounds = this.RestoreBounds;
                Settings settings = Settings.Instance;
                settings.WindowLocation = bounds.TopLeft;
                settings.WindowSize = bounds.Size;
            }
        }

        private void OnSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ThumbnailWidth")
            {
                ThumbColumn.Width = new GridLength(Settings.Instance.ThumbnailWidth + 20);
            }
            delayedActions.StartDelayedAction("SaveSettings", SaveSettings, TimeSpan.FromMilliseconds(500));
        }

        async void SaveSettings()
        {
            Settings settings = Settings.Instance;
            await settings.SaveAsync();
        }

        private void OnSliderMoved(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!synching)
            {
                Pause();
                Player.Position = TimeSpan.FromMilliseconds(e.NewValue);
                delayedActions.StartDelayedAction("create thumbnail", CreateThumbnail, TimeSpan.FromMilliseconds(100));
            }
        }

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            Player.LoadedBehavior = MediaState.Manual;
            Player.Play();
            playing = true;
            SyncVideo();
        }

        private void OnPause(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        private void Pause()
        {
            Player.LoadedBehavior = MediaState.Manual;
            Player.Pause();
            playing = false;
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            VideoSlider.Maximum = Player.NaturalDuration.TimeSpan.TotalMilliseconds;
            CreateThumbnail();
        }

        private void OnSyncVideo()
        {
            double position = Player.Position.TotalMilliseconds;
            synching = true;
            VideoSlider.Value = position;
            synching = false;
            CreateThumbnail();
            SyncVideo();
        }

        private void SyncVideo()
        {
            if (playing)
            {
                delayedActions.StartDelayedAction("sync video", OnSyncVideo, TimeSpan.FromMilliseconds(100));
            }
        }

        bool playing;
        bool synching;

        private void OnSave(object sender, RoutedEventArgs e)
        {
            SaveThumbnail();
        }

        private void CreateThumbnail()
        {
            int width = (int)Player.ActualWidth;
            int height = (int)Player.ActualHeight;
            if (width == 0 || height ==  0)
            {
                Thumbnail.Source = null;
                return;
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            Player.Arrange(new Rect(0, 0, width, height));
            bitmap.Render(Player);
            Player.InvalidateArrange();
            MainGrid.InvalidateArrange(); // reposition the player back to where it belongs.

            Thumbnail.Source = bitmap;
            Thumbnail.Width = Settings.Instance.ThumbnailWidth;
            Thumbnail.Height = Thumbnail.Width * (double)height / (double)width;
        }

        private void SaveThumbnail()
        {
            int width = (int)Thumbnail.ActualWidth;
            int height = (int)Thumbnail.ActualHeight;

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(Thumbnail);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            string dir = System.IO.Path.GetDirectoryName(Settings.Instance.LastFile);
            string basename = System.IO.Path.GetFileNameWithoutExtension(Settings.Instance.LastFile);
            string thumbnail = System.IO.Path.Combine(dir, basename + ".png");
            using (FileStream fs = new FileStream(thumbnail, FileMode.Create, FileAccess.Write))
            { 
                encoder.Save(fs);
            }
        }
    }
    }
