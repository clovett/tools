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
    public class SvgEllipse : SvgElement
    {
        double radiusX;
        double radiusY;
        double cx;
        double cy;

        // <ellipse cx="300" cy="150" rx="200" ry="80"/>
        public SvgEllipse()
        {
        }

        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            XElement ellipse = this.Element;
            if (ellipse != null)
            {
                radiusX = ellipse.GetLength("rx");
                radiusY = ellipse.GetLength("ry");
                cx = ellipse.GetLength("cx");
                cy = ellipse.GetLength("cy");
                Canvas.SetLeft(this, cx - radiusX);
                Canvas.SetTop(this, cy - radiusY);
                this.Width = this.Height = (radiusX + radiusY);
            }
        }

        public override void Update()
        {
            base.Update();

            XElement ellipse = this.Element;
            ellipse.SetAttributeValue("cx", XmlConvert.ToString(cx));
            ellipse.SetAttributeValue("cy", XmlConvert.ToString(cy));
            ellipse.SetAttributeValue("rx", XmlConvert.ToString(radiusX));
            ellipse.SetAttributeValue("ry", XmlConvert.ToString(radiusY));
        }

        public override Rect Resize(Rect bounds)
        {
            radiusX = bounds.Width / 2;
            radiusY = bounds.Height / 2;
            cx = bounds.Left + (bounds.Width / 2);
            cy = bounds.Top + (bounds.Height / 2);

            Canvas.SetLeft(this, cx - radiusX);
            Canvas.SetTop(this, cy - radiusY);
            this.Width = radiusX * 2;
            this.Height = radiusY * 2;

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

            if (fill != null || pen != null)
            {
                drawingContext.DrawEllipse(fill, pen, new Point(radiusX, radiusY), radiusX, radiusY);
            }
        }
    }
}
