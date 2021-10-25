using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DependencyViewer{
    public class ImageGenerator : GraphGenerator {

        public ImageGenerator() {
        }

        public override void Prepare() {
            base.Prepare();
        }
        public override void Create(Panel container) {
            base.Create(container);

            foreach (string filename in this.FileNames) {
                System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                img.Source = new BitmapImage(new Uri(filename));
                img.Margin = new Thickness(10);
                container.Children.Add(img);
            }

        }

        public override string FileFilter {
            get {
                return "Images (*.gif;*.png;*.jpg;*.bmp)|*.gif;*.png;*.jpg;*.bmp";
            }
        }

        public override string Label {
            get { return "Images"; }
        }
    }
}
