using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Dashboard
{

    public class RadialDialDataValue : DependencyObject
    {
        public RadialDialDataValue()
        {           
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                Angle = 45;
            }
        }



        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(RadialDialDataValue), new PropertyMetadata(0.0));



        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Caption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register("Caption", typeof(string), typeof(RadialDialDataValue), new PropertyMetadata(null));




        public string ValueLabel
        {
            get { return (string)GetValue(ValueLabelProperty); }
            set { SetValue(ValueLabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValueLabel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueLabelProperty =
            DependencyProperty.Register("ValueLabel", typeof(string), typeof(RadialDialDataValue), new PropertyMetadata(null));

        
    }

}
