using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
            InitializeComponent();

            Directory.Text = System.IO.Path.GetTempPath();
        }

        private void OnSnap(object sender, RoutedEventArgs e)
        {
            try
            {
                index++;
                ImageFile.Text = "";
                GrabScreenToFile(System.IO.Path.Combine(Directory.Text, "image" + index + ".png"), System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Creating Screenshot", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GrabScreenToFile(string fileName, System.Drawing.Imaging.ImageFormat imgFormat)
        {
            string fullPath = System.IO.Path.GetFullPath(fileName);

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

                    ImageFile.Text = System.IO.Path.GetFileName(fullPath);
                    ImageFile.Tag = fullPath;
                }
            }
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

        private void OnImageFileClick(object sender, MouseButtonEventArgs e)
        {
            string path = (string)ImageFile.Tag;
            NativeMethods.OpenUrl(IntPtr.Zero, new Uri(path));
        }
    }
}
