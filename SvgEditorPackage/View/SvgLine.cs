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

namespace Microsoft.SvgEditorPackage.View
{
    public class SvgLine : SvgElement
    {
        double x1;
        double y1;
        double x2;
        double y2;

        // <line x1="0" y1="0" x2="300" y2="300" style="stroke:rgb(99,99,99);stroke-width:2"/>
        public SvgLine()
        {
        }

        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            XElement line = this.Element;
            if (line != null)
            {
                x1 = line.GetLength("x1");
                y1 = line.GetLength("y1");
                x2 = line.GetLength("x2");
                y2 = line.GetLength("y2");
                this.Width = Math.Abs(x2 - x1);
                this.Height = Math.Abs(y2 - y1);
            }
        }


        public override void Update()
        {
            base.Update();

            XElement line = this.Element;
            line.SetAttributeValue("x1", XmlConvert.ToString(x1));
            line.SetAttributeValue("y1", XmlConvert.ToString(y1));
            line.SetAttributeValue("x2", XmlConvert.ToString(x2));
            line.SetAttributeValue("y2", XmlConvert.ToString(y2));
        }

        public override Rect Resize(Rect bounds)
        {
            x1 = bounds.Left;
            y1 = bounds.Top;
            x2 = bounds.Right;
            y2 = bounds.Bottom;


            this.Width = Math.Abs(x2 - x1);
            this.Height = Math.Abs(y2 - y1);

            InvalidateVisual();
            return bounds;
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

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
            if (pen != null)
            {
                drawingContext.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
            }
        }
    }
}
