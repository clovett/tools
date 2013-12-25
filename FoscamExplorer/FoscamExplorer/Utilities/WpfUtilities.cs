using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.ApplicationModel;

namespace FoscamExplorer
{
    public static class WpfUtilities
    {
        public static void Beep(MediaElement player)
        {
            PlaySound(player, "Beep.wav");
        }

        public static async void PlaySound(MediaElement player, string soundFileName)
        {
            StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            StorageFile file = await folder.GetFileAsync(soundFileName);
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                player.SetSource(stream, file.ContentType);
                player.Play();
            }

        }

        public static BitmapSource LoadImage(string relativeName)
        {
            Uri uri = new Uri(App.BaseUri, relativeName);
            return new BitmapImage(uri);            
        }

        public async static Task<BitmapSource> LoadImageAsync(Stream imageStream)
        {
            if (imageStream == null)
            {
                return null;
            }

            var stream = await ConvertStream(imageStream);

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            // convert to a writable bitmap so we can get the PixelBuffer back out later...
            // in case we need to edit and/or re-encode the image.
            WriteableBitmap bmp = new WriteableBitmap((int)decoder.PixelHeight, (int)decoder.PixelWidth);

            // remember the format of this image
            PixelBufferObject.SetBitmapPixelFormat(bmp, decoder.BitmapPixelFormat);
            PixelBufferObject.SetBitmapAlphaMode(bmp, decoder.BitmapAlphaMode);

            stream.Seek(0);
            bmp.SetSource(stream);

            imageStream.Dispose();
            return bmp;
        }

        public async static Task<BitmapSource> CreateImageAsync(string base64image)
        {
            if (string.IsNullOrEmpty(base64image))
            {
                return null;
            }

            byte[] bytes = Convert.FromBase64String(base64image);
            MemoryStream stream = new MemoryStream(bytes);

            return await LoadImageAsync(stream);
        }

        internal async static Task<string> Base64EncodeImageAsync(WriteableBitmap image)
        {
            using (InMemoryRandomAccessStream stm = new InMemoryRandomAccessStream())
            {
                await stm.WriteAsync(image.PixelBuffer);
                await stm.FlushAsync();
                stm.Seek(0);

                // see what format we have
                BitmapPixelFormat format = PixelBufferObject.GetBitmapPixelFormat(image);
                BitmapAlphaMode alpha = PixelBufferObject.GetBitmapAlphaMode(image);

                using (var stream = stm.AsStreamForRead())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        byte[] pixels = ms.ToArray();

                        // now re-encode these pixels.
                        InMemoryRandomAccessStream encoded = new InMemoryRandomAccessStream();
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, encoded);
                        encoder.SetPixelData(format, alpha, (uint)image.PixelWidth, (uint)image.PixelHeight, 96, 96, pixels);
                        await encoder.FlushAsync();

                        using (MemoryStream output = new MemoryStream())
                        {
                            encoded.Seek(0);
                            encoded.AsStreamForRead().CopyTo(output);
                            return Convert.ToBase64String(output.ToArray());
                        }
                    }
                }
            }
        }

        internal static T FindResource<T>(this FrameworkElement e, string name)
        {
            object value = null;
            DependencyObject d = e;
            while (d != null)
            {
                FrameworkElement f = d as FrameworkElement;
                if (f != null)
                {
                    if (f.Resources.TryGetValue(name, out value))
                    {
                        return (T)value;
                    }
                }
                d = VisualTreeHelper.GetParent(d);
            }

            if (App.Current.Resources.TryGetValue(name, out value))
            {
                return (T)value;
            }

            return default(T);
        }

        internal static async Task<IRandomAccessStream> ConvertStream(Stream stream)
        {
            // note: randomAccessStream.AsStreamForWrite is broken, so I use iBuffer instead.

            byte[] block = new byte[stream.Length];
            int len = await stream.ReadAsync(block, 0, block.Length);
            IBuffer buffer = block.AsBuffer();

            InMemoryRandomAccessStream ras = new InMemoryRandomAccessStream();
            using (var write = ras.GetOutputStreamAt(0))
            {
                await write.WriteAsync(buffer);
                await write.FlushAsync();
                await ras.FlushAsync();
            }

            return ras;
        }

        internal async static Task<BitmapSource> CreateThumbnail(Stream stream, uint maxWidth, uint maxHeight)
        {
            var randomAccessStream = await ConvertStream(stream);

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

            uint width = decoder.PixelWidth;
            uint height = decoder.PixelHeight;
            double scale = 1;
            if (width > maxWidth || height > maxHeight)
            {
                scale = Math.Min((double)maxWidth / (double)width, (double)maxHeight / (double)height);
            }
            else
            {
                stream.Seek(0, SeekOrigin.Begin);
                return await LoadImageAsync(stream);
            }

            width = (uint)(scale * width);
            height = (uint)(scale * height);

            BitmapTransform transform = new BitmapTransform()
            {
                ScaledWidth = width,
                ScaledHeight = height
            };

            var pixelData = await decoder.GetPixelDataAsync(
                decoder.BitmapPixelFormat,
                decoder.BitmapAlphaMode,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            byte[] pixelDataByte = pixelData.DetachPixelData();
            
            InMemoryRandomAccessStream resizedStream = new InMemoryRandomAccessStream();

            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, resizedStream);
            encoder.SetPixelData(decoder.BitmapPixelFormat, decoder.BitmapAlphaMode, width, height, 96, 96, pixelDataByte);            
            await encoder.FlushAsync();

            // convert to a writable bitmap so we can get the PixelBuffer back out later...
            // in case we need to edit and/or re-encode the image.
            WriteableBitmap bmp = new WriteableBitmap((int)width, (int)height);
            // remember the format of this image
            PixelBufferObject.SetBitmapPixelFormat(bmp, decoder.BitmapPixelFormat);
            PixelBufferObject.SetBitmapAlphaMode(bmp, decoder.BitmapAlphaMode);

            resizedStream.Seek(0);
            bmp.SetSource(resizedStream);

            return bmp;
        }


    }

    public class PixelBufferObject : DependencyObject
    {


        public static BitmapPixelFormat GetBitmapPixelFormat(DependencyObject obj)
        {
            return (BitmapPixelFormat)obj.GetValue(BitmapPixelFormatProperty);
        }

        public static void SetBitmapPixelFormat(DependencyObject obj, BitmapPixelFormat value)
        {
            obj.SetValue(BitmapPixelFormatProperty, value);
        }

        // Using a DependencyProperty as the backing store for BitmapPixelFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BitmapPixelFormatProperty =
            DependencyProperty.RegisterAttached("BitmapPixelFormat", typeof(BitmapPixelFormat), typeof(PixelBufferObject), new PropertyMetadata(BitmapPixelFormat.Unknown));



        public static BitmapAlphaMode GetBitmapAlphaMode(DependencyObject obj)
        {
            return (BitmapAlphaMode)obj.GetValue(BitmapAlphaModeProperty);
        }

        public static void SetBitmapAlphaMode(DependencyObject obj, BitmapAlphaMode value)
        {
            obj.SetValue(BitmapAlphaModeProperty, value);
        }

        // Using a DependencyProperty as the backing store for BitmapAlphaMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BitmapAlphaModeProperty =
            DependencyProperty.RegisterAttached("BitmapAlphaMode", typeof(BitmapAlphaMode), typeof(PixelBufferObject), new PropertyMetadata(BitmapAlphaMode.Ignore));

        
        
    }
}
