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
using System.Reflection;
using System.Globalization;

namespace ToonBuilder.ColorPicker
{
    public class ColorChangedEventArgs : EventArgs
    {
        public Color NewColor { get; set; }
    }

    /// <summary>
    /// Interaction logic for ColorPickerControl.xaml
    /// </summary>
    public partial class ColorPickerControl : UserControl
    {

        #region PROPERTIES PRIVATES

        bool _hueVerticalSliderMouseDown;
        bool _swatchSelectorMouseDown;
        float _selectedHue;
        double _navigatorRectangleSelectorX;
        double _navigatorRectangleSelectorY;

        Color _currentColor;

        public Color CurrentColor
        {
            get { return _currentColor; }
            set
            {
                if (_currentColor != value)
                {
                    _currentColor = value;

                    ColorHelper.HSV hsv = ColorHelper.ColorToHSV(_currentColor);
                    int screenPosition = (int)(hsv.Hue * (HuePicker.Height / 360)); 

                    MoveToCurrentColor();
                    MoveHueTracker(screenPosition);

                    // Hue tracker changes the color, so we need to set it again.
                    _currentColor = value;

                    UpdateAllElementOfTheColorPickerControl();
                }
            }
        }

        bool _colorWasEditedManuallyByRGB = false;
        bool _colorWasEditedManuallyByHexValue = false;


        #endregion


        #region EVENTS

        public event EventHandler<ColorChangedEventArgs> ColorChanged;

        #endregion



        /// <summary>
        /// Single control UI for selecting a solid Color
        /// </summary>
        public ColorPickerControl()
        {
            InitializeComponent();

            // Track the user input for the Saturation & Brightness
            ColorRangeRectangle.MouseLeftButtonDown += new MouseButtonEventHandler(OnNavigatorRectangle_MouseLeftButtonDown);
            ColorRangeRectangle.MouseMove += new MouseEventHandler(OnNavigatorRectangle_MouseMove);
            ColorRangeRectangle.MouseLeftButtonUp += new MouseButtonEventHandler(OnNavigatorRectangle_MouseLeftButtonUp);

            // Track user input for the vertical Hue color range 
            HuePicker.MouseLeftButtonDown += new MouseButtonEventHandler(OnHuePicker_MouseLeftButtonDown);
            HuePicker.MouseMove += new MouseEventHandler(OnHuePicker_MouseMove);
            HuePicker.MouseLeftButtonUp += new MouseButtonEventHandler(OnHuePicker_MouseLeftButtonUp);
           
            // Track user input for the little triangle
            HueSelector.MouseLeftButtonDown += new MouseButtonEventHandler(OnHuePicker_MouseLeftButtonDown);
            HueSelector.MouseMove += new MouseEventHandler(OnHuePicker_MouseMove);
            HueSelector.MouseLeftButtonUp += new MouseButtonEventHandler(OnHuePicker_MouseLeftButtonUp);
            
            // User can also use the individual Edit Box
            ColorEditBoxR.ColorPartValueChanged += new ColorRgbaEditBox.ColorPartValueChangedHandler(OnColorEditBoxChanged);
            ColorEditBoxG.ColorPartValueChanged += new ColorRgbaEditBox.ColorPartValueChangedHandler(OnColorEditBoxChanged);
            ColorEditBoxB.ColorPartValueChanged += new ColorRgbaEditBox.ColorPartValueChangedHandler(OnColorEditBoxChanged);
            ColorEditBoxA.ColorPartValueChanged += new ColorRgbaEditBox.ColorPartValueChangedHandler(OnColorEditBoxChanged);


            // Set color range selector to the upper right corner
            _navigatorRectangleSelectorX = (int)ColorRangeRectangle.Width;
            _navigatorRectangleSelectorY = 0;
            _selectedHue = 0;

            List<ColorPickerItem> list = new List<ColorPickerItem>();
            foreach (PropertyInfo pi in typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.Public))
            {
                Color c = (Color)pi.GetValue(null, null);
                list.Add(new ColorPickerItem() { Name = pi.Name, Color = c });
            }
            HexValue.ItemsSource = list;

            CurrentColor = Colors.Red;
        }

        #region EVENTS RECEIVED FROM EDIT BOXES

        void OnColorEditBoxChanged(byte value)
        {
            _colorWasEditedManuallyByRGB = true;

            CurrentColor = Color.FromArgb(ColorEditBoxA.Value, ColorEditBoxR.Value, ColorEditBoxG.Value, ColorEditBoxB.Value);

            _colorWasEditedManuallyByRGB = false;
        }


        void OnHexValue_LostFocus(object sender, RoutedEventArgs e)
        {
            _colorWasEditedManuallyByHexValue = true;

            Color? convertedColor = ColorHelper.HexToColor(HexValue.Text);
            if (convertedColor != null)
            {
                CurrentColor = (Color)convertedColor;
            }

            _colorWasEditedManuallyByHexValue = false;
        }

        #endregion


        #region EVENTS RECEIVED FROM Hue Control

        void OnHuePicker_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            _hueVerticalSliderMouseDown = true;
            int yPos = (int)e.GetPosition((UIElement)sender).Y;

            MoveHueTracker(yPos);

            this.HuePicker.CaptureMouse();
        }


        void OnHuePicker_MouseMove(object sender, MouseEventArgs e)
        {
            if (_hueVerticalSliderMouseDown)
            {
                double newPosition = e.GetPosition((UIElement)sender).Y;

                if (newPosition < 0)
                {
                    newPosition = 0;
                }
                if (newPosition > HuePicker.Height)
                {
                    newPosition = HuePicker.Height;
                }

                MoveHueTracker(newPosition);

            
            }
        }


        void OnHuePicker_MouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            _hueVerticalSliderMouseDown = false;
            this.HuePicker.ReleaseMouseCapture();
        }

        #endregion


        #region EVENTS RECEIVED FROM NavigationRectangle Control

        void OnNavigatorRectangle_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            _swatchSelectorMouseDown = true;
            Point pos = e.GetPosition((UIElement)sender);

            _navigatorRectangleSelectorX = (int)pos.X;
            _navigatorRectangleSelectorY = (int)pos.Y;

            ChangeCurrentColor();

            UpdateAllElementOfTheColorPickerControl();

            this.ColorRangeRectangle.CaptureMouse();
        }

        void OnNavigatorRectangle_MouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            _swatchSelectorMouseDown = false;
            this.ColorRangeRectangle.ReleaseMouseCapture();
        }

        void OnNavigatorRectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_swatchSelectorMouseDown)
            {
                Point pos = e.GetPosition((UIElement)sender);
                _navigatorRectangleSelectorX = (int)pos.X;

                //-------------------------------------------------------------
                // Stay in the boundery of the rectangle
                //
                if (_navigatorRectangleSelectorX < 0)
                {
                    _navigatorRectangleSelectorX = 0;
                }
                if (_navigatorRectangleSelectorX > ColorRangeRectangle.Width)
                {
                    _navigatorRectangleSelectorX = ColorRangeRectangle.Width;
                }


                _navigatorRectangleSelectorY = (int)pos.Y;
                if (_navigatorRectangleSelectorY < 0)
                {
                    _navigatorRectangleSelectorY = 0;
                }

                if (_navigatorRectangleSelectorY > ColorRangeRectangle.Height)
                {
                    _navigatorRectangleSelectorY = ColorRangeRectangle.Height;
                }


                ChangeCurrentColor();

                UpdateAllElementOfTheColorPickerControl();
            }
        }

        private void ChangeCurrentColor()
        {
            float componentSaturation = (float)(_navigatorRectangleSelectorX / RectRangeSolidColor.Width);
            float componentValue = 1 - (float)(_navigatorRectangleSelectorY / RectRangeSolidColor.Height);
            _currentColor = ColorHelper.ConvertHsvToRgb(_currentColor.A, _selectedHue, componentSaturation, componentValue);
        }

        private void MoveToCurrentColor()
        {
            ColorHelper.HSV hsv = ColorHelper.ColorToHSV(_currentColor);

            _navigatorRectangleSelectorX = hsv.Saturation * RectRangeSolidColor.Width;
            _navigatorRectangleSelectorY = (1 - hsv.Value) * RectRangeSolidColor.Height;
            MoveNavigatorCircleUsingXY(_navigatorRectangleSelectorX, _navigatorRectangleSelectorY);
        }

        #endregion


        /// <summary>
        /// Update all UI element to reflect the current active color
        /// </summary>
        private void UpdateAllElementOfTheColorPickerControl()
        {

            MoveNavigatorCircleUsingXY(_navigatorRectangleSelectorX, _navigatorRectangleSelectorY);

            UpdateTexturalRepresentationOfColor();

            UpdatePreviewSampleColor();

        }

        /// <summary>
        /// Place the target (little circle) onto the gradiant brightness range rectangle
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void MoveNavigatorCircleUsingXY(double x, double y)
        {
            _navigatorRectangleSelectorX = x;
            _navigatorRectangleSelectorY = y;

            ColoRangesMouseTargetCircle.SetValue(Canvas.LeftProperty, _navigatorRectangleSelectorX - (ColoRangesMouseTargetCircle.Height / 2));
            ColoRangesMouseTargetCircle.SetValue(Canvas.TopProperty, _navigatorRectangleSelectorY - (ColoRangesMouseTargetCircle.Height / 2));
        }


        /// <summary>
        /// Using the active color we populate the 5 edit boxes
        /// </summary>
        private void UpdateTexturalRepresentationOfColor()
        {

            //-----------------------------------------------------------------
            // Update the 4 edit boxes for RGBA
            //
            if (_colorWasEditedManuallyByRGB == false)
            {
                ColorEditBoxR.Value = CurrentColor.R;
                ColorEditBoxG.Value = CurrentColor.G;
                ColorEditBoxB.Value = CurrentColor.B;
                ColorEditBoxA.Value = CurrentColor.A;
            }


            //-----------------------------------------------------------------
            // Update the HEX value edit box
            //
            if (_colorWasEditedManuallyByHexValue == false)
            {
                HexValue.Text = ColorHelper.GetHexCode(CurrentColor);
            }
        }

        /// <summary>
        /// Paint the preview box at the bottom with the current active color
        /// </summary>
        private void UpdatePreviewSampleColor()
        {
            PreviewOfCurrentColor.Fill = new SolidColorBrush(CurrentColor);

            if (ColorChanged != null)
            {
                ColorChanged(this, new ColorChangedEventArgs() { NewColor = CurrentColor });
            }
        }


        /// <summary>
        /// Move the two triangles up or down the vertical HUE color selector
        /// </summary>
        /// <param name="yPos"></param>
        private void MoveHueTracker(double yPos)
        {
            int huePosition = (int)(yPos / HuePicker.Height * 255);
            int gradientStops = 6;

            Color pureColor = ColorHelper.GetColorFromPosition(huePosition * gradientStops);

            // Change the vertical position of the two triangle for selection the Hue 
            double newPosition = yPos - (HueSelector.Height / 2);

            HueSelector.SetValue(Canvas.TopProperty, newPosition);

            _selectedHue = (float)(yPos / HuePicker.Height) * 360;


            ChangeCurrentColor();
            RectRangeSolidColor.Fill = new SolidColorBrush(Color.FromArgb(255, pureColor.R, pureColor.G, pureColor.B));

            UpdateAllElementOfTheColorPickerControl();
        }

        private void OnHexValueSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ColorPickerItem item = (ColorPickerItem)HexValue.SelectedItem;
            if (item != null)
            {
                CurrentColor = item.Color;
            }
        }

    }
}
