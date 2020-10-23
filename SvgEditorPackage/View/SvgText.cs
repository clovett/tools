using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.SvgEditorPackage.View
{
    public class SvgText : SvgElement
    {
        //<text id="TextElement" x="50" y="50" style="font-family:Verdana;font-size:24;"> It's SVG!</text>
        public SvgText()
        {
        }

        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            XElement text = this.Element;
            if (text != null)
            {
                Canvas.SetLeft(this, text.GetLength("x"));
                Canvas.SetTop(this, text.GetLength("y"));
            }
        }

        public override void Update()
        {
            base.Update();

            XElement rect = this.Element;
            rect.SetAttributeValue("x", XmlConvert.ToString(Canvas.GetLeft(this)));
            rect.SetAttributeValue("y", XmlConvert.ToString(Canvas.GetTop(this)));
        }

        public override Rect Resize(Rect bounds)
        {
            double width = bounds.Width;
            double height = bounds.Height;
            double x = bounds.Left;
            double y = bounds.Top;

            Canvas.SetLeft(this, x);
            Canvas.SetTop(this, y);

            return bounds;
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            FontFamily family = (FontFamily)this.GetValue(TextBlock.FontFamilyProperty);
            double fontSize = (double)this.GetValue(TextBlock.FontSizeProperty);

            // todo: parse font-style, font-weight, etc.
            FontStyle style = (FontStyle)this.GetValue(TextBlock.FontStyleProperty);
            FontWeight weight = (FontWeight)this.GetValue(TextBlock.FontWeightProperty);
            FontStretch stretch = (FontStretch)this.GetValue(TextBlock.FontStretchProperty);

            Brush fill = (Brush)this.GetValue(Shape.FillProperty);
            if (fill == null)
            {
                fill = Brushes.Black;
            }
            // todo: I think "stroke" needs to turn into an outlined text path.

            if (fill != null)
            {
                FormattedText ft = new FormattedText(Element.Value, CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface(family, style, weight, stretch), fontSize, fill);
                drawingContext.DrawText(ft, new Point(0,0));
            }
        }
    }
}
