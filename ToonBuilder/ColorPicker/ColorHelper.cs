using System;
using System.Windows.Media;

namespace ToonBuilder.ColorPicker
{
    internal class ColorHelper
    {
        private const byte MIN = 0;
        private const byte MAX = 255;

        public static Color GetColorFromPosition(int position)
        {
            byte mod = (byte)(position % MAX);
            byte diff = (byte)(MAX - mod);
            byte alpha = 255;

            switch (position / MAX)
            {
                case 0: return Color.FromArgb(alpha, MAX, mod, MIN);
                case 1: return Color.FromArgb(alpha, diff, MAX, MIN);
                case 2: return Color.FromArgb(alpha, MIN, MAX, mod);
                case 3: return Color.FromArgb(alpha, MIN, diff, MAX);
                case 4: return Color.FromArgb(alpha, mod, MIN, MAX);
                case 5: return Color.FromArgb(alpha, MAX, MIN, diff);
                default: return Colors.Black;
            }
        }

        static byte GetMod(byte c)
        {
            return (byte)(c % 255);
        }



        /// <summary>
        /// Return a Color String in hex format #AABBCCDD
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string GetHexCode(Color c)
        {
            return string.Format("#{0}{1}{2}{3}",
                c.A.ToString("X2"),
                c.R.ToString("X2"),
                c.G.ToString("X2"),
                c.B.ToString("X2")
                );
        }



        /// <summary>
        /// Return null if the hex string representation of color is not perfectly convertable to a Color value
        /// </summary>
        /// <param name="colorInHexFormat"></param>
        /// <returns></returns>
        public static Color? HexToColor(String colorInHexFormat)
        {


            //   "#AA524742"
            if ((colorInHexFormat.StartsWith("#")) &&
                    (
                        (colorInHexFormat.Length == 7)  // #BBCCDD
                        ||
                        (colorInHexFormat.Length == 9)  // #AABBCCDD
                    )
                )
            {
                try
                {
                    int offsetForRGBtext = 1;

                    byte a = Byte.MaxValue;
                    byte r = 0;
                    byte g = 0;
                    byte b = 0;

                    if (colorInHexFormat.Length == 9)
                    {
                        a = Byte.Parse(colorInHexFormat.Substring(offsetForRGBtext, 2), System.Globalization.NumberStyles.HexNumber);
                        offsetForRGBtext = 3;
                    }

                    r = Byte.Parse(colorInHexFormat.Substring(offsetForRGBtext + 0, 2), System.Globalization.NumberStyles.HexNumber);
                    g = Byte.Parse(colorInHexFormat.Substring(offsetForRGBtext + 2, 2), System.Globalization.NumberStyles.HexNumber);
                    b = Byte.Parse(colorInHexFormat.Substring(offsetForRGBtext + 4, 2), System.Globalization.NumberStyles.HexNumber);


                    return Color.FromArgb(a, r, g, b);
                }
                catch
                {
                    // Survive any bad string parsing
                    // this way the user can make corrections
                }
            }

            return null;
        }



        /// <summary>
        ///         
        /// </summary>
        /// <param name="alpha">Alpha channel component</param>
        /// <param name="h">hue  component</param>
        /// <param name="s">Saturation component</param>
        /// <param name="v">Value component</param>
        /// <returns></returns>
        public static Color ConvertHsvToRgb(byte alpha, float h, float s, float v)
        {
            h = h / 360;

            if (s > 0)
            {
                if (h >= 1)
                {
                    h = 0;
                }

                h = 6 * h;

                int hueFloor = (int)Math.Floor(h);
                byte a = (byte)Math.Round(MAX * v * (1.0 - s));
                byte b = (byte)Math.Round(MAX * v * (1.0 - (s * (h - hueFloor))));
                byte c = (byte)Math.Round(MAX * v * (1.0 - (s * (1.0 - (h - hueFloor)))));
                byte d = (byte)Math.Round(MAX * v);

                switch (hueFloor)
                {
                    case 0: return Color.FromArgb(alpha, d, c, a);
                    case 1: return Color.FromArgb(alpha, b, d, a);
                    case 2: return Color.FromArgb(alpha, a, d, c);
                    case 3: return Color.FromArgb(alpha, a, b, d);
                    case 4: return Color.FromArgb(alpha, c, a, d);
                    case 5: return Color.FromArgb(alpha, d, a, b);
                    default: return Color.FromArgb(alpha, 0, 0, 0);
                }
            }
            else
            {
                byte d = (byte)(v * MAX);
                return Color.FromArgb(alpha, d, d, d);
            }

        }
        /// <summary>
        /// HSV (hue, saturation, value) to RGB
        /// </summary>
        /// <param name="hue"></param>
        /// <param name="saturation"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Color ConvertHsvToRgb(float hue, float saturation, float value)
        {
            return ConvertHsvToRgb(255, hue, saturation, value);
        }

        public struct HSV
        {
            public double Hue;
            public double Saturation;
            public double Value;
        }

        /// <summary>
        /// Color to HSV (hue, saturation, value)
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static HSV ColorToHSV(Color color)
        {
            HSV hsv = new HSV();

            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));
            hsv.Hue = GetHue(color);
            hsv.Saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            hsv.Value = max / 255d;

            return hsv;
        }

        public static double GetHue(Color c)
        {
            if (c.R == c.G && c.G == c.B)
                return 0.0f; // 0 makes as good an UNDEFINED value as any

            float r = (float)c.R / 255.0f;
            float g = (float)c.G / 255.0f;
            float b = (float)c.B / 255.0f;

            float max, min;
            float delta;
            double hue = 0.0f;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            delta = max - min;

            if (r == max)
            {
                hue = (g - b) / delta;
            }
            else if (g == max)
            {
                hue = 2 + (b - r) / delta;
            }
            else if (b == max)
            {
                hue = 4 + (r - g) / delta;
            }
            hue *= 60;

            if (hue < 0.0f)
            {
                hue += 360.0f;
            }
            return hue;
        }


    }
}

