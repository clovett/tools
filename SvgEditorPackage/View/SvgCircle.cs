using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.XmlEditor;

namespace Microsoft.SvgEditorPackage.View
{
    public class SvgCircle : SvgElement
    {
        double radius;
        double cx;
        double cy;

        //<circle cx="100" cy="50" r="40" stroke="black" stroke-width="2" fill="red"/>
        public SvgCircle()
        {
        }

        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            XElement circle = this.Element;
            if (circle != null)
            {
                radius = circle.GetLength("r");
                cx = circle.GetLength("cx");
                cy = circle.GetLength("cy");
                Canvas.SetLeft(this, cx - radius);
                Canvas.SetTop(this, cy - radius);
                this.Width = this.Height = radius * 2;
            }
        }

        public override void Update()
        {
            base.Update();

            XElement circle = this.Element;
            circle.SetAttributeValue("cx", XmlConvert.ToString(cx));
            circle.SetAttributeValue("cy", XmlConvert.ToString(cy));
            circle.SetAttributeValue("r", XmlConvert.ToString(radius));
        }

        public override Rect Resize(Rect bounds)
        {
            radius = Math.Min(bounds.Width, bounds.Height) / 2;
            cx = bounds.Left + (bounds.Width / 2); 
            cy = bounds.Top + (bounds.Height / 2);

            Canvas.SetLeft(this, cx - radius);
            Canvas.SetTop(this, cy - radius);
            this.Width = this.Height = radius * 2;

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
                drawingContext.DrawEllipse(fill, pen, new Point(radius, radius), radius, radius);
            }
        }
    }
}
