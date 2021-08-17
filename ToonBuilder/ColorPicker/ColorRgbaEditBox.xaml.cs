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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ToonBuilder.ColorPicker
{
    /// <summary>
    /// Interaction logic for ColorRgbaEditBox.xaml
    /// </summary>
    public partial class ColorRgbaEditBox : UserControl
    {
        public delegate void ColorPartValueChangedHandler(Byte value);

        public event ColorPartValueChangedHandler ColorPartValueChanged;


        public ColorRgbaEditBox()
        {
            InitializeComponent();
            Label = "C";
            ColorPart = 0;
            this.DataContext = this;

        }

        public String Label
        {
            get { return (String)this.GetValue(LabelProperty); }
            set { this.SetValue(LabelProperty, value); TheLabel.Content = value; }
        }

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
                    "Label",
                    typeof(String),
                    typeof(ColorRgbaEditBox),
                    new FrameworkPropertyMetadata("?", OnLabelPropertyChanged)
                    );


        private static void OnLabelPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ColorRgbaEditBox x = source as ColorRgbaEditBox;
            if (x != null)
            {
                x.TheLabel.Content = (string)e.NewValue;
            }

        }


        public int ColorPart
        {
            get { return (int)this.GetValue(ColorPartProperty); }
            set { this.SetValue(ColorPartProperty, value); }
        }


        public static readonly DependencyProperty ColorPartProperty = DependencyProperty.Register(
                    "ColorPart",
                    typeof(int),
                    typeof(ColorRgbaEditBox),
                    new FrameworkPropertyMetadata(0, OnColorPartPropertyChanged)
                    );

        private static void OnColorPartPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ColorRgbaEditBox x = source as ColorRgbaEditBox;
            if (x != null)
            {
            }
        }


        public Byte Value
        {
            get { return (Byte)this.GetValue(ColorPartValueProperty); }
            set
            {
                if (Value != value)
                {
                    this.SetValue(ColorPartValueProperty, value);
                }
            }
        }

        public static readonly DependencyProperty ColorPartValueProperty = DependencyProperty.Register(
                  "ColorPartValue",
                  typeof(Byte),
                  typeof(ColorRgbaEditBox),
                  new FrameworkPropertyMetadata((Byte)0, OnColorPartValuePropertyChanged)
                  );

        private static void OnColorPartValuePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            ColorRgbaEditBox x = source as ColorRgbaEditBox;
            if (x != null)
            {

                try
                {
                    Byte newValue = (Byte)e.NewValue;

                    Byte R = 0;
                    Byte G = 0;
                    Byte B = 0;

                    switch (x.ColorPart)
                    {
                        case 1:
                            R = newValue;
                            break;
                        case 2:
                            G = newValue;
                            break;
                        case 3:
                            B = newValue;
                            break;

                        default:
                            R = G = B = 0;
                            break;
                    }

                    x.ColorEditBox.Text = newValue.ToString();

                    LinearGradientBrush lb = new LinearGradientBrush(
                        Color.FromArgb(0, R, G, B),
                        Color.FromArgb(255, R, G, B),
                        0);

                    {
                        x.ColorPartRender.Background = lb;
                    }
                }
                catch
                {
                }
            }
        }

        private void ColorEditBox_TextChanged(object sender, TextChangedEventArgs e)
        {
           

        }


        /// <summary>
        /// Validate the value that the user has entered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorEditBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Byte b = Value;

            try
            {
                
                int rawValue = Convert.ToInt32(ColorEditBox.Text);
                if (rawValue < 0)
                {
                    // Cap the lowest possible value to Zero
                    b = 0;
                }
                else
                {
                    if (rawValue > Byte.MaxValue)
                    {
                        // Cap the highest value to 255
                        b = Byte.MaxValue;
                    }
                    else
                    {
                        // The value is in range 0 to 255
                        b = Convert.ToByte(rawValue);
                    }
                }
            }
            catch 
            {
                // This will catch any conversion error
            }

            ColorEditBox.Text = b.ToString();

            if (Value != b)
            {
                Value = b;

                if (ColorPartValueChanged != null)
                {
                    ColorPartValueChanged(Value);
                }
            }
        }



    }
}
