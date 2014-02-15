using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace WpfIconMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnSaveIcon(object sender, RoutedEventArgs e)
        {
            int width = (int)IconBorder.ActualWidth;
            int height= (int)IconBorder.ActualHeight;

            IconBorder.Arrange(new Rect(0, 0, width, height));
            IconBorder.UpdateLayout();

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(IconBorder);

            IconBorder.InvalidateArrange();

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            SaveFileDialog fs = new SaveFileDialog();
            fs.Filter = "PNG Files (*.png)|*.png";
            if (fs.ShowDialog() == true)
            {
                using (var stream = fs.OpenFile())
                {
                    encoder.Save(stream);
                }
            }
        }

        private void OnCharacterChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
