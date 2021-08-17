using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace ToonBuilder.ColorPicker
{
    public class ColorPickerItem 
    {
        public Color Color { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Color.ToString(); // we want the hex value in the edit box...
        }
    }
}
