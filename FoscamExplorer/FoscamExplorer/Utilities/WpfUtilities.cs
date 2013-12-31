using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if WINDOWS_PHONE
using Windows.Storage;
using System.Windows.Controls;
using Windows.Storage.Streams;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using Windows.ApplicationModel;
using System.Windows.Media.Animation;
#else
using Windows.Graphics.Imaging;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel;
using Windows.Foundation;
#endif

namespace FoscamExplorer
{
    public static class WpfUtilities
    {

#if WINDOWS_PHONE

        internal static Storyboard CreateFadeAnimation(this FrameworkElement element, TimeSpan duration, double from, double to)
        {
            ExponentialEase ee = new ExponentialEase()
            {
                EasingMode = EasingMode.EaseOut,
                Exponent = 1.5,
            };

            DoubleAnimation anim = new DoubleAnimation()
            {
                Duration = new Duration(duration),
                From = from,
                To = to,
                EasingFunction = ee,
            };

            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(anim);

            return storyboard;
        }

#endif

        public async static Task<BitmapSource> LoadImageAsync(Stream imageStream)
        {
            if (imageStream == null)
            {
                return null;
            }

#if WINDOWS_PHONE
            BitmapImage bmp = null;
            try
            {
                bmp = new BitmapImage();
                bmp.SetSource(imageStream);
            }
            catch (Exception ex)
            {
                Log.WriteLine("LoadImageAsync failed: " + ex.Message);
            }
            await Task.Delay(1);
#else
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
#endif
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

#if WINDOWS_PHONE
        internal static bool TryGetValue(this ResourceDictionary dictionary, string key, out object value)
        {
            return dictionary.TryGetValue(key, out value);
        }
#endif

#if !WINDOWS_PHONE
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
#endif

    }

#if !WINDOWS_PHONE
    
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
#endif
}
