using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ToonBuilder.ColorPicker
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        public ColorPickerWindow()
        {
            InitializeComponent();
            this.ColorPicker1.ColorChanged += new EventHandler<ColorChangedEventArgs>(ColorPicker1_ColorChanged);
        }

        void ColorPicker1_ColorChanged(object sender, ColorChangedEventArgs e)
        {
            if (ColorChanged != null) 
            {
                ColorChanged(this, e);
            }
        }

        public Color SelectedColor
        {
            get { return ColorPicker1.CurrentColor; }
            set { this.ColorPicker1.CurrentColor = value; }
        }

        public event EventHandler<ColorChangedEventArgs> ColorChanged;
    }
}
