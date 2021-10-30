using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Markup;
using System.Printing;

namespace DependencyViewer {
    class Image {

        internal static void Save(FrameworkElement diagram, string filename) {
            try {
                BitmapEncoder enc = null;
                string ext = System.IO.Path.GetExtension(filename);

                switch (ext.ToLower()) {
                    case ".bmp":
                        enc = new BmpBitmapEncoder();
                        break;
                    case ".gif":
                        enc = new GifBitmapEncoder();
                        break;
                    case ".xaml":
                        using (StreamWriter sw = new StreamWriter(filename, false, System.Text.Encoding.UTF8)) {
                            XamlWriter.Save(diagram, sw);
                        }
                        break;
                    case ".xps":
                        Xps.SaveXps(diagram, filename);
                        break;
                    case ".png":
                        enc = new PngBitmapEncoder();
                        break;
                    case ".jpg":
                        enc = new JpegBitmapEncoder();
                        break;
                    case ".dot":
                        break;
                }
                if (enc != null) {                    
                    using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write)) {
                        enc.Frames.Add(GetBitmap(diagram));
                        enc.Save(fs);
                    }
                }

            } catch (System.Exception e) {
                MessageBox.Show(e.ToString(), "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static BitmapFrame GetVisibleBitmap(FrameworkElement diagram) {
            FrameworkElement parent = diagram.Parent as FrameworkElement;
            Size s = parent.RenderSize;
            Transform t = diagram.RenderTransform;
            Point offset = t.Transform(new Point(0, 0));
            // Find out how big the diagram is after it is scaled.
            Point a = t.Transform(new Point(diagram.ActualWidth, diagram.ActualHeight));
            a.X -= offset.X;
            a.Y -= offset.Y;
            // Clip it to visible bounds.
            if (a.X > s.Width) a.X = s.Width;
            if (a.Y > s.Height) a.Y = s.Height;
            
            RenderTargetBitmap rmi = new RenderTargetBitmap((int)a.X, (int)a.Y, 1 / 96, 1 / 96, PixelFormats.Pbgra32);
            rmi.Render(diagram);
            return BitmapFrame.Create(rmi);
        }

        public static BitmapFrame GetBitmap(FrameworkElement diagram) {
            // reset VisualOffset to (0,0).
            Transform t = diagram.RenderTransform;

            diagram.RenderTransform = null;
            Point p = new Point(diagram.ActualWidth, diagram.ActualHeight);
            diagram.Arrange(new Rect(0, 0, p.X, p.Y));
            RenderTargetBitmap rmi = new RenderTargetBitmap((int)p.X, (int)p.Y, 1 / 96, 1 / 96, PixelFormats.Pbgra32);
            rmi.Render(diagram);

            diagram.RenderTransform = t;
            diagram.InvalidateArrange();
            diagram.InvalidateVisual();
            return BitmapFrame.Create(rmi);
        }

        public static void Print(PrintDialog dlg, FrameworkElement diagram, string description) {
            Transform t = diagram.RenderTransform;
            
            diagram.RenderTransform = null;
            Point p = new Point(diagram.ActualWidth, diagram.ActualHeight);
            diagram.Arrange(new Rect(20, 20, p.X, p.Y));

            dlg.PrintVisual(diagram, description);

            diagram.RenderTransform = t;
            diagram.InvalidateArrange();
            diagram.InvalidateVisual();
        }

    }
}
