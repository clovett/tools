using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.SvgEditorPackage.View
{
    public static class SvgUtil
    {
        /// <summary>
        /// Parse an attribute as an svg "length" where:
        /// </summary>
        /// <param name="e">The XElement containing the attribute</param>
        /// <param name="attributeName">The XAttribute to lookup</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double GetLength(this XElement e, string attributeName, double defaultValue = 0)
        {
            XAttribute a = e.Attribute(attributeName);
            if (a != null)
            {
                // cm = centimeters
                string value = (string)a;
                if (!string.IsNullOrEmpty(value))
                {
                    return value.ParseLength(defaultValue);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// length ::= number ("em" | "ex" | "px" | "in" | "cm" | "mm" | "pt" | "pc" | "%")?
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static double ParseLength(this string length, double defaultValue = 0)
        {
            double factor = 1.0;
            int suffix = 0;
            length = length.Trim();
            if (length.EndsWith("em"))
            {
                // toto: should be relative to current inherited font-size 
                suffix = 2;
            }
            else if (length.EndsWith("ex"))
            {
                // toto: should be relative to current inherited font x-height 
                suffix = 2;
            }
            else if (length.EndsWith("px"))
            {
                factor = 1;
                suffix = 2;
            }
            else if (length.EndsWith("in"))
            {
                factor = 90;
                suffix = 2;
            }
            else if (length.EndsWith("cm"))
            {
                factor = 35.43307;
                suffix = 2;
            }
            else if (length.EndsWith("mm"))
            {
                factor = 3.543307;
                suffix = 2;
            }
            else if (length.EndsWith("pt"))
            {
                factor = 1.25;
                suffix = 2;
            }
            else if (length.EndsWith("pc"))
            {
                factor = 15;
                suffix = 2;
            }
            else if (length.EndsWith("%"))
            {
                // todo: must be relative to nearest containing viewport size, and we'd need to know if this
                // is an X-coordinate or a Y-coordinate so we can take a percentage of the actual-width or actual-height.
                suffix = 1;
            }
            length = length.Substring(0,length.Length-suffix);

            double result;
            if (double.TryParse(length, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result * factor;
            }
            return defaultValue;
        }

        public static Brush GetBrush(this XElement element, string attributeName)
        {
            XAttribute a = element.Attribute(attributeName);
            if (a != null)
            {
                // cm = centimeters
                string value = (string)a;
                if (!string.IsNullOrEmpty(value))
                {
                    return value.ParseBrush();
                }
            }
            return null;
        }

        public static Brush ParseBrush(this string value)
        {
            string lower = value.ToLowerInvariant().Trim();

            if (lower.Length == 0) return null;
            if (lower == "none") return null;
            // todo: support "current animated value"
            if (lower == "currentColor") return Brushes.Black;


            if (lower.StartsWith("rgb("))
            {
                return new SolidColorBrush(ParseRgbColor(lower));
            }
            else
            {
                // should be #AAFF22 format or a well known color name.
                try
                {
                    ColorConverter cc = new ColorConverter();
                    Color color = (Color)cc.ConvertFromInvariantString(lower);
                    return new SolidColorBrush(color);
                }
                catch
                {
                }
            }
            return null;
        }

        //  "rgb(" wsp* integer comma integer comma integer wsp* ")"
        //  "rgb(" wsp* integer "%" comma integer "%" comma integer "%" wsp* ")"
        public static Color ParseRgbColor(string value)
        {
            int current = 0;
            int[] components = new int[3];
            int pos = 0;

            for (int i = 4, n = value.Length; i < n && pos < 3;  i++)
            {
                char c = value[i];
                if (Char.IsWhiteSpace(c))
                {
                    // skip
                }
                else if (c == '%')
                {
                    current = (current * 255) / 100;
                }
                else if (c == ',' || c == ')')
                {
                    components[pos++] = current;
                    current = 0;
                }
                else if (char.IsDigit(c))
                {
                    // todo: watch for overflow.
                    int digit = Convert.ToInt32(c) - Convert.ToInt32('0');
                    current = (current * 10) + digit;
                } 
                else 
                {
                    // todo: error handling.
                }
            }
            if (pos < 3 && current != 0)
            {
                components[pos++] = current;
            }

            return Color.FromRgb((byte)components[0], (byte)components[1], (byte)components[2]);
        }

        public static FontFamily GetFontFamily(XElement element)
        {
            string value = (string)element.Attribute("font-family");
            if (value == null) return null;

            string fontName = value.Trim().ToLowerInvariant();
            if (fontName == "inherit")
            {
                return null;
            }
            else
            {
                return ParseFontFamily(fontName);
            }
        }

        public static FontFamily ParseFontFamily(this string value)
        {
            FontFamilyConverter cc = new FontFamilyConverter();
            return (FontFamily)cc.ConvertFromInvariantString(value);
        }

        // transform="translate(-10,-20) scale(2) rotate(45) translate(5,10)"
        public static Transform ParseTransform(this string value)
        {
            List<Transform> list = new List<Transform>();
            int? letters = null;
            int? digits = null;
            string token = null;
            List<double> numbers = new List<double>();

            for (int i = 0, n = value.Length; i < n; i++)
            {
                char c = value[i];
                if (Char.IsWhiteSpace(c))
                {
                    // skip
                }
                else if (c == '(')
                {
                    if (letters.HasValue)
                    {
                        token = value.Substring(letters.Value, i - letters.Value);
                        letters = null;
                    }
                }
                else if (c == ',' || c == ')')
                {
                    if (digits.HasValue)
                    {
                        string num = value.Substring(digits.Value, i - digits.Value);
                        double result = 0;
                        double.TryParse(num, out result);
                        numbers.Add(result);
                        digits = null;
                    }
                    if (c == ')')
                    {
                        switch (token)
                        {
                            case "translate":
                                if (numbers.Count > 0)
                                {
                                    double tx = numbers[0];
                                    double ty = numbers.Count > 1 ? numbers[1] : 0;
                                    list.Add(new TranslateTransform(tx, ty));
                                }
                                break;
                            case "scale":
                                if (numbers.Count > 0)
                                {
                                    double sx = numbers[0];
                                    double sy = numbers.Count > 1 ? numbers[1] : sx;
                                    list.Add(new ScaleTransform(sx, sy));
                                }
                                break;
                            case "rotate":
                                if (numbers.Count > 0)
                                {
                                }
                                break;
                            case "matrix":
                                if (numbers.Count == 6)
                                {
                                    list.Add(new MatrixTransform(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]));
                                }
                                break;
                            case "skewX":
                                // todo:
                                break;
                            case "skewY":
                                // todo:
                                break;
                        }
                        numbers.Clear();
                    }
                }
                else if (char.IsDigit(c) || c == '.')
                {
                    // number argument
                    if (digits == null)
                    {
                        digits = i;
                    }
                }
                else if (char.IsLetter(c))
                {
                    // token.
                    if (letters == null)
                    {
                        letters = i;
                    }
                }
            }

            if (list.Count == 1)
            {
                return list[0];
            }

            TransformGroup group = new TransformGroup();
            group.Children = new TransformCollection(list);
            return group;
        }
    }
}
