using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Xml;

namespace Microsoft.SvgEditorPackage.View
{
    public class SvgRect : SvgElement
    {
        //<rect x=".01cm" y=".01cm" width="4.98cm" height="3.98cm"
        //        fill="none" stroke="blue" stroke-width=".02cm" />        
        public SvgRect()
        {
        }

        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            XElement rect = this.Element;
            if (rect != null)
            {
                Canvas.SetLeft(this, rect.GetLength("x"));
                Canvas.SetTop(this, rect.GetLength("y"));
                this.Width = rect.GetLength("width");
                this.Height = rect.GetLength("height");
            }
        }

        public override void Update()
        {
            base.Update();

            XElement rect = this.Element;
            rect.SetAttributeValue("x", XmlConvert.ToString(Canvas.GetLeft(this)));
            rect.SetAttributeValue("y", XmlConvert.ToString(Canvas.GetTop(this)));
            rect.SetAttributeValue("width", XmlConvert.ToString(this.Width));
            rect.SetAttributeValue("height", XmlConvert.ToString(this.Height));
        }

        public override Rect Resize(Rect bounds)
        {
            double width = bounds.Width;
            double height = bounds.Height;
            double x = bounds.Left;
            double y = bounds.Top;

            Canvas.SetLeft(this, x);
            Canvas.SetTop(this, y);
            this.Width = width;
            this.Height = height;

            return bounds;
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Brush fill = (Brush)GetValue(Shape.FillProperty);
            Brush stroke = (Brush)GetValue(Shape.StrokeProperty);

            Pen pen = null;
            if (stroke != null)
            {
                object strokeWidth = GetValue(Shape.StrokeThicknessProperty);
                if (strokeWidth is double)
                {
                    pen = new Pen(stroke, (double)strokeWidth);
                }
            }

            XElement rect = this.Element;
            double rx = rect.GetLength("rx");
            double ry = rect.GetLength("ry");

            if (fill != null || pen != null)
            {
                drawingContext.DrawRoundedRectangle(fill, pen, new Rect(0, 0, this.Width, this.Height), rx, ry);
            }
        }
    }
}
