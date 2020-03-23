using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace ScreenCopy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int index;
        Settings settings;
        ObservableCollection<Thumbnail> thumbnails = new ObservableCollection<Thumbnail>();

        public MainWindow()
        {
            InitializeComponent();

            settings = Settings.Instance;

            if (string.IsNullOrEmpty(settings.Directory))
            {
                Directory.Text = System.IO.Path.GetTempPath();   
            }
            else
            {
                Directory.Text = settings.Directory;
            }

            ThumbnailView.ItemsSource = thumbnails;
        }

        protected override void OnClosed(EventArgs e)
        {
            settings.Directory = Directory.Text;
            settings.Save();
            base.OnClosed(e);
        }

        private object originalContent;
        private bool stopped;
        private bool repeat;
        private double delaySeconds = 1;

        private void OnSnap(object sender, RoutedEventArgs e)
        {
            if ((string)SnapButton.Content == "Stop")
            {
                this.stopped = true;
                SnapButton.Content = originalContent;
                return;
            }
            else
            {
                this.stopped = false;
                originalContent = SnapButton.Content;
                if (this.repeat)
                {
                    SnapButton.Content = "Stop";
                }
            }

            Task.Run(TakeSnapshots);
        }

        private async Task TakeSnapshots()
        {
            try
            {
                while (!this.stopped)
                {
                    if (this.delaySeconds > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(this.delaySeconds));
                    }

                    if (this.stopped)
                    {
                        return;
                    }

                    index++;

                    // this part has to happen back on UI thread.
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        SoundPlayer.Source = ResolveAsset("assets/shutter.mp3");
                        var nowait = Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SoundPlayer.Play();
                        }));

                        GrabScreenToFile(System.IO.Path.Combine(Directory.Text, "image" + index + ".png"), System.Drawing.Imaging.ImageFormat.Png, !this.repeat);
                    }));

                    if (!this.repeat)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Creating Screenshot", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return;
        }

        Uri ResolveAsset(string relativePath)
        {
            Uri baseUri = new Uri(this.GetType().Assembly.Location);
            Uri resolved = new Uri(baseUri, relativePath);
            return resolved;
        }

        private void GrabScreenToFile(string fileName, System.Drawing.Imaging.ImageFormat imgFormat, bool showThumbnail)
        {
            
            string fullPath = System.IO.Path.GetFullPath(fileName);
            string dirName = System.IO.Path.GetDirectoryName(fullPath);
            System.IO.Directory.CreateDirectory(dirName);

            ForceDelete(fullPath);

            int width = NativeMethods.ConvertFromDeviceIndependentPixels(System.Windows.SystemParameters.PrimaryScreenWidth);
            int height = NativeMethods.ConvertFromDeviceIndependentPixels(System.Windows.SystemParameters.PrimaryScreenHeight);

            IntPtr hwndDesktop = NativeMethods.GetDesktopWindow();
            IntPtr hdc = NativeMethods.GetWindowDC(hwndDesktop);

            using (Graphics g = Graphics.FromHdc((IntPtr)hdc))
            {

                // create a device context we can copy to
                IntPtr hdcDest = NativeMethods.CreateCompatibleDC(hdc);

                // create a bitmap we can copy it to,
                // using GetDeviceCaps to get the width/height
                IntPtr hBitmap = NativeMethods.CreateCompatibleBitmap(hdc, width, height);

                // select the bitmap object
                IntPtr hOld = NativeMethods.SelectObject(hdcDest, hBitmap);

                // bit copy
                NativeMethods.BitBlt(hdcDest, 0, 0, width, height, hdc, 0, 0, 0x00CC0020);

                // restore selection
                NativeMethods.SelectObject(hdcDest, hOld);

                // get a .NET image object for it
                using (System.Drawing.Image img = System.Drawing.Image.FromHbitmap((IntPtr)hBitmap))
                {

                    // free Bitmap object
                    NativeMethods.DeleteObject(hBitmap);

                    // clean up 
                    NativeMethods.DeleteDC(hdcDest);
                    NativeMethods.ReleaseDC(hwndDesktop, hdc);

                    img.Save(fullPath, imgFormat);

                    if (showThumbnail)
                    {
                        AddThumbnail(fullPath, img);
                    }
                }
            }
        }

        private void AddThumbnail(string path, System.Drawing.Image img)
        {
            using (var smallImage = img.GetThumbnailImage(200, 200, new System.Drawing.Image.GetThumbnailImageAbort(() => { return false; }), IntPtr.Zero))
            {

                var thumbnail = new Thumbnail()
                {
                    Path = path,
                    Image = ConvertToWpfImage(smallImage)
                };
                thumbnails.Add(thumbnail);

            }
        }

        private ImageSource ConvertToWpfImage(System.Drawing.Image smallImage)
        {
            MemoryStream ms = new MemoryStream();
            smallImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            BitmapImage result = new System.Windows.Media.Imaging.BitmapImage();
            result.BeginInit();
            result.StreamSource = new MemoryStream(ms.ToArray());
            result.EndInit();

            return result;
        }

        private void ForceDelete(string fileName)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    MakeReadWrite(fileName);
                    File.Delete(fileName);
                }
                catch (Exception)
                {
                }
            }
        }

        private void MakeReadWrite(string fileName)
        {
            if ((File.GetAttributes(fileName) & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
            {
                File.SetAttributes(fileName, File.GetAttributes(fileName) & ~System.IO.FileAttributes.ReadOnly);
            }
        }


        private void OnThumbnailSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                Thumbnail nail = e.AddedItems[0] as Thumbnail;
                string path = nail.Path;
                NativeMethods.OpenUrl(IntPtr.Zero, new Uri(path));
            }
        }

        private void OnListViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                List<Thumbnail> toRemove = new List<Thumbnail>();
                foreach (Thumbnail item in ThumbnailView.SelectedItems)
                {
                    toRemove.Add(item);
                }

                foreach (var item in toRemove)
                {
                    thumbnails.Remove(item);
                }
            }
        }

        private void OnRepeatChanged(object sender, RoutedEventArgs e)
        {
            this.repeat = (this.CheckBoxRepeat.IsChecked == true);
        }

        private void OnDelayChanged(object sender, TextChangedEventArgs e)
        {
            string delay = TextBoxDelay.Text;
            double seconds = 0;
            if (double.TryParse(delay, out seconds))
            {
                this.delaySeconds = seconds;
            }
        }
    }
}
