using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace ImageEditor.Utilities
{
    class ImageUtils
    {
        private int width, height;
        private BitmapSource bitmap;
        private int bytesPerPixel;
        int stride;
        byte[] pixels;
        private BitmapPalette palette;

        public ImageUtils(BitmapSource input)
        {

            bitmap = input;
            width = bitmap.PixelWidth;
            height = bitmap.PixelHeight;
            ReadImage();
        }

        void ReadImage()
        {
            int bitsPerPixel = bitmap.Format.BitsPerPixel;
            stride = (bitmap.PixelWidth * bitsPerPixel + 7) / 8;
            bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
            if (bitmap.Format != PixelFormats.Bgra32)
            {
                throw new Exception("This algorithm requires Bgra32 format");
            }
            this.palette = bitmap.Palette;
            int bufferSize = bytesPerPixel * width * height;
            this.pixels = new byte[bufferSize];
            bitmap.CopyPixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }

        int smoothingAmount = 0;

        public void SmoothEdges()
        {
            smoothingAmount++;

            // scan rows for first and last pixels and blend those edges.
            for (int y = 0; y < height; y++)
            {
                int count = smoothingAmount;
                // scan from the left
                for (int x = 0; x < stride; x += bytesPerPixel)
                {
                    int index = (y * stride) + x;

                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];
                    byte a = pixels[index + 3];
                    if (a > 0)
                    {
                        // ok, this is the first non-transparent pixel from the left, so blend it!
                        pixels[index + 3] /= 2;
                        if (--count == 0)
                        {
                            break;
                        }
                    }
                }

                count = smoothingAmount;
                // scan from the right
                for (int x = stride - bytesPerPixel; x >= 0; x -= bytesPerPixel)
                {
                    int index = (y * stride) + x;
                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];
                    byte a = pixels[index + 3];
                    if (a > 0)
                    {
                        // ok, this is the first non-transparent pixel from the right, so blend it!
                        pixels[index + 3] /= 2;
                        if (--count == 0)
                        {
                            break;
                        }
                    }
                }
            }
            // scan vertical columns for first and last pixels and blend those edges.
            for (int x = 0; x < stride; x += bytesPerPixel)
            {
                int count = smoothingAmount;
                // scan from the top
                for (int y = 0; y < height; y++)
                {
                    int index = (y * stride) + x;

                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];
                    byte a = pixels[index + 3];
                    if (a > 0)
                    {
                        // ok, this is the first non-transparent pixel from the left, so blend it!
                        pixels[index + 3] /= 2;
                        if (--count == 0)
                        {
                            break;
                        }
                    }
                }

                count = smoothingAmount;
                // scan from the bottom
                for (int y = height - 1; y >= 0; y--)
                {
                    int index = (y * stride) + x;
                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];
                    byte a = pixels[index + 3];
                    if (a > 0)
                    {
                        // ok, this is the first non-transparent pixel from the left, so blend it!
                        pixels[index + 3] /= 2;
                        if (--count == 0)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public BitmapSource GetUpdated()
        {
            return BitmapSource.Create(width, height, 96, 90, PixelFormats.Bgra32, this.palette, this.pixels, this.stride);
        }
    }
}
