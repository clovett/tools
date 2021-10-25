using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using System.Windows.Documents;

namespace DependencyViewer {

    class Xps {
        internal static void SaveXps(FrameworkElement diagram, string filename) {
            System.IO.Packaging.Package package = System.IO.Packaging.Package.Open(filename, System.IO.FileMode.Create);
            XpsDocument xpsDoc = new XpsDocument(package);

            XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);

            // zero the VisualOffset
            Size s = diagram.RenderSize;
            Transform t = diagram.LayoutTransform;
            Point p = t.Transform(new Point(s.Width, s.Height));
            diagram.Arrange(new Rect(0, 0, p.X, p.Y));

            ScrollViewer scroller = diagram.Parent as ScrollViewer;
            scroller.Content = null;

            FixedPage fp = new FixedPage();
            fp.Width = p.X;
            fp.Height = p.Y;
            // Must add the inherited styles before we add the diagram child!
            fp.Resources.MergedDictionaries.Add(diagram.Resources);
            fp.Children.Add(diagram);
            xpsWriter.Write(fp);

            // put the diagram back into the scroller.
            fp.Children.Remove(diagram);
            scroller.Content = diagram;

            package.Close();
        }

    }
}
