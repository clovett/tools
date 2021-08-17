using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;

namespace ToonBuilder
{
    /// <summary>
    /// This class pops up a radial menu, where each menu item is represented by a 16x16 icon.
    /// It computes the radius necessary to show all items and smoothly animates the items to 
    /// that location.
    /// </summary>
    public class RadialMenu : Canvas
    {
        public RadialMenu()
        {
        }

        public void Add(RadialMenuItem item)
        {
            Children.Add(item);
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            PositionItems();
        }

        public void PositionItems()
        {            
            int count = this.Children.Count;
            if (count == 0) return;

            double angle = 90;
            double radius = 40;
            if (count > 4) {
                angle = 360 / count;
                double sinAngle = Math.Sin(angle * Math.PI / 180);
                radius = Math.Max(40, 20 / sinAngle);
            }

            double a = -90;
            foreach (RadialMenuItem item in this.Children) 
            {
                double rads = a * Math.PI / 180;
                double cx = radius * Math.Sin(rads);
                double cy = radius * Math.Cos(rads);
                Canvas.SetLeft(item, cx);
                Canvas.SetTop(item, cy);
                a += angle;
            }


        }


    }

    public class RadialMenuItem : Button
    {
        public RadialMenuItem()
        {
        }

        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(RadialMenuItem), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    }
}
