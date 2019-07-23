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

namespace ColorPicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool updatingRgb;
        bool updatingHex;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnHexChanged(object sender, TextChangedEventArgs e)
        {
            if (!updatingHex)
            {
                TextBox box = (TextBox)sender;
                ConvertHex(box.Text);
            }

        }

        private void OnRgbChanged(object sender, TextChangedEventArgs e)
        {
            if (!updatingRgb)
            {
                TextBox box = (TextBox)sender;
                ConvertRGB(box.Text);
            }
        }

        void ConvertRGB(string text)
        {
            List<string> parts = new List<string>();
            string part = "";
            foreach(char ch in text)
            {
                if (Char.IsDigit(ch))
                {
                    part += ch;
                }
                else
                {
                    parts.Add(part);
                    part = "";
                }
            }
            if (!string.IsNullOrEmpty(part))
            {
                parts.Add(part);
            }
            string result = "#";
            List<int> components = new List<int>();
            foreach (var item in parts)
            {
                int channel = 0;
                if (int.TryParse(item, out channel))
                {
                    if (channel > 255)
                    {
                        channel = 255;
                    }
                    else if (channel < 0)
                    {
                        channel = 0;
                    }
                    components.Add(channel);
                    string hex = channel.ToString("x");
                    if (hex.Length == 1)
                    {
                        hex = "0" + hex;
                    }
                    result += hex;
                }
            }
            byte red = 0;
            byte green = 0;
            byte blue = 0;
            if (components.Count > 0)
            {
                red = (byte)components[0];
            }
            if (components.Count > 1)
            {
                green = (byte)components[1];
            }
            if (components.Count > 2)
            {
                blue = (byte)components[2];
            }
            Swatch.Background = new SolidColorBrush(Color.FromArgb(0xff, red, green, blue));
            updatingHex = true;
            TextBoxHex.Text = result;
            updatingHex = false;
        }


        void ConvertHex(string text)
        {
        }
    }
}
