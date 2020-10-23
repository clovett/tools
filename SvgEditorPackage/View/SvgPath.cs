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
    public class SvgPath : SvgElement
    {
        Geometry geometry;

        // <path fill="rgb(90, 173, 109)" d="M 23 117 C -92 -5 -35 -103 43 -25 133 -86 174 2 22 117" />    
        public SvgPath()
        {
        }

        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            geometry = null;
            try
            {
                XElement path = this.Element;
                if (path != null)
                {
                    geometry = PathGeometry.Parse((string)path.Attribute("d"));
                }
            }
            catch (Exception)
            {
                // todo: error handling (draw an error icon or something, with tooltip error message).
            }
            if (geometry != null)
            {
                Rect bounds = geometry.Bounds;
                this.Width = bounds.Width;
                this.Height = bounds.Height;
            }
        }


        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            XElement path = this.Element;
            if (geometry != null)
            {

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
                    drawingContext.DrawGeometry(fill, pen, geometry);
                }
            }
        }
    }
}
