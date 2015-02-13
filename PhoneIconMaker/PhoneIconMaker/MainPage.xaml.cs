using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Globalization;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PhoneIconMaker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }
        
        private async void OnSaveIcon(object sender, RoutedEventArgs e)
        {
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(IconBorder);

            // 1. Get the pixels
            IBuffer pixelBuffer = await bitmap.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();

            // 2. Write the pixels to a InMemoryRandomAccessStream
            var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

            encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync();

            stream.Seek(0);

            FileSavePicker fs = new FileSavePicker(); 
            fs.FileTypeChoices.Add("PNG Images", new List<string>() { ".png" });

            StorageFile file = await fs.PickSaveFileAsync();
            if (file != null)
            {
                using (Stream encodedImageStream = stream.AsStreamForRead())
                {
                    byte[] buffer = new byte[64000];

                    using (Stream fileStream = await file.OpenStreamForWriteAsync())
                    {
                        int len = encodedImageStream.Read(buffer, 0, buffer.Length);
                        while (len > 0)
                        {
                            fileStream.Write(buffer, 0, len);
                            len = encodedImageStream.Read(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
        }

        private void OnCharacterChanged(object sender, TextChangedEventArgs e)
        {
            string text = CharacterBox.Text;

            NumberFormatInfo info = new NumberFormatInfo();


        }
    }
}
