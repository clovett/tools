using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace XmlNotepad
{
    public sealed class Utilities
    {
        private Utilities() { }
        
        // Lighten up the given baseColor so it is easy to read on the system Highlight color background.
        public static Brush HighlightTextBrush(Color baseColor) {
            SolidBrush ht = SystemBrushes.Highlight as SolidBrush;
            Color selectedColor = ht != null ? ht.Color : Color.FromArgb(49, 106, 197);
            HLSColor cls = new HLSColor(baseColor);
            HLSColor hls = new HLSColor(selectedColor);
            int luminosity = (hls.Luminosity > 120) ? 20 : 220;
            return new SolidBrush(HLSColor.ColorFromHLS(cls.Hue, luminosity, cls.Saturation));
        }


    }
}
