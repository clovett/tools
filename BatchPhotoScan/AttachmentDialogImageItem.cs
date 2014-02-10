using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Walkabout.Utilities;

namespace BatchPhotoScan
{
    /// <summary>
    /// This class implements the abstract AttachmentDialogItem by
    /// wrapping an Image as the content
    /// </summary>
    class AttachmentDialogImageItem : AttachmentDialogItem
    {
        Image image;

        public AttachmentDialogImageItem(BitmapSource source)
        {
            this.image = new Image();
            this.image.Source = source;
            this.AddVisualChild(image);
        }

        public AttachmentDialogImageItem(string filePath, bool tempFile)
        {
            if (!tempFile)
            {
                this.FileName = filePath;
            }
            BitmapSource frame = LoadImage(filePath);

            if (frame.Format != PixelFormats.Pbgra32)
            {
                frame = ConvertImageFormat(frame);
            }

            this.image = new Image();
            this.image.Source = frame;
            this.AddVisualChild(image);
        }

        private static BitmapFrame LoadImage(string filePath)
        {
            // Load it into memory first so we stop the BitmapDecoder from locking the file.
            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[16000];
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int len = fs.Read(buffer, 0, buffer.Length);
                while (len > 0)
                {
                    ms.Write(buffer, 0, len);
                    len = fs.Read(buffer, 0, buffer.Length);
                }
            }
            ms.Seek(0, SeekOrigin.Begin);

            BitmapDecoder decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.None);
            BitmapFrame frame = decoder.Frames[0];

            return frame;
        }

        public override void Save(string fileName)
        {
            string dir = Path.GetDirectoryName(fileName);
            string fullName = Path.Combine(dir, System.IO.Path.GetFileNameWithoutExtension(fileName) + this.FileExtension);
            this.FileName = fullName;
            BitmapSource source = this.Bitmap;
            var frame = BitmapFrame.Create(source);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(frame);

            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            using (FileStream fs = new FileStream(fullName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                encoder.Save(fs);
            }

            TempFilesManager.RemoveTempFile(fileName);
        }

        private BitmapSource ConvertImageFormat(BitmapSource image)
        {
            int width = image.PixelWidth;
            int height = image.PixelHeight;
            if (image.Format != PixelFormats.Pbgra32)
            {
                // then copy the image to a format we can handle.
                RenderTargetBitmap copy = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                Image img = new Image();
                img.Source = image;
                img.Width = width;
                img.Height = height;
                img.Arrange(new Rect(0, 0, width, height));
                copy.Render(img);
                return copy;
            }
            return image;
        }

        public override FrameworkElement Content
        {
            get { return image; }
        }

        public BitmapSource Bitmap
        {
            get { return (BitmapSource)image.Source; }
            set { image.Source = value; }
        }

        public override void Copy()
        {
            Clipboard.SetImage((BitmapSource)image.Source);
        }


        public override FrameworkElement CloneContent()
        {
            return new Image()
            {
                Source = image.Source,
                Width = image.Source.Width,
                Height = image.Source.Height
            };
        }

        public override string FileExtension
        {
            get { return ".png"; }
        }

        public override bool LiveResizable
        {
            get { return false; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
            {
                return image;
            }
            return null;
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            image.Measure(availableSize);
            return new Size(image.Source.Width, image.Source.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            image.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return finalSize;
        }

        public override Rect ResizeLimit
        {
            get { return new Rect(0, 0, image.Source.Width, image.Source.Height); }
        }

        public override void Resize(Rect bounds)
        {
            BitmapSource bitmap = (BitmapSource)this.Bitmap;

            double dpiX = bitmap.DpiX / 96;
            double dpiY = bitmap.DpiY / 96;

            Rect adjusted = new Rect(bounds.Left * dpiX, bounds.Top * dpiY, bounds.Width * dpiX, bounds.Height * dpiY);

            if (adjusted.Height == 0)
            {
                adjusted.Height = bitmap.Height;
            }
            if (adjusted.Width == 0)
            {
                adjusted.Width = bitmap.Width;
            }
            if (adjusted.Left + adjusted.Width > bitmap.Width)
            {
                adjusted.Width = bitmap.Width - adjusted.Left;
            }
            if (adjusted.Top + adjusted.Height > bitmap.Height)
            {
                adjusted.Height = bitmap.Height - adjusted.Top;
            }

            var cropped = new CroppedBitmap(bitmap, new Int32Rect((int)adjusted.Left, (int)adjusted.Top, (int)adjusted.Width, (int)adjusted.Height));
            this.Bitmap = cropped;
        }

        internal void RotateImage(double degrees)
        {
            ImageSource source = image.Source;

            // then copy the image to a format we can handle.
            double w = source.Width;
            double h = source.Height;

            RenderTargetBitmap copy = new RenderTargetBitmap((int)h, (int)w, 96, 96, PixelFormats.Pbgra32);
            Image img = new Image();
            img.Source = source;
            img.Width = w;
            img.Height = h;
            // rotate the angle about 0,0
            RotateTransform rotate = new RotateTransform(degrees, 0, 0);

            Rect bounds = new Rect(0, 0, w, h);
            // figure out where the bounds ends up with this rotation.
            Rect transformed = rotate.TransformBounds(bounds);

            // keep the bounds in non-negative territory by adjusting with a translate.
            TransformGroup group = new TransformGroup();
            group.Children.Add(rotate);
            group.Children.Add(new TranslateTransform(transformed.X < 0 ? -transformed.X : 0,
                transformed.Y < 0 ? -transformed.Y : 0));

            img.RenderTransform = group;
            img.Arrange(bounds);
            copy.Render(img);

            image.Source = copy;
        }

    }
}
