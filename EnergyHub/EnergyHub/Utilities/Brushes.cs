using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace EnergyHub.Utilities
{
    public static class Brushes
    {
        public static Brush Red
        {
            get { return new SolidColorBrush() { Color = Colors.Red }; }
        }
        public static Brush Gray
        {
            get { return new SolidColorBrush() { Color = Colors.Gray }; }
        }
        public static Brush Green
        {
            get { return new SolidColorBrush() { Color = Colors.Green }; }
        }
        public static Brush Salmon
        {
            get { return new SolidColorBrush() { Color = Color.FromArgb(0xFF, 0xFF, 0x6F, 0x4F) }; }
        }
    }
}
