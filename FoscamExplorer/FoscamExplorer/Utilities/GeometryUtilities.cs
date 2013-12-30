using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if WINDOWS_PHONE
using System.Windows.Media;
using System.Windows;
#else
using Windows.Foundation;
using Windows.UI.Xaml.Media;
#endif

namespace FoscamExplorer
{
    static class GeometryUtilities
    {
        public static PathGeometry CreateFatArrow(Point start, Point end, double thickness, double headSize, double headDepth)
        {
            PathGeometry g = new PathGeometry();
            PathFigure f = new PathFigure();
            f.IsClosed = true;
            f.IsFilled = true;
            g.Figures.Add(f);

            Vector v = new Vector(start, end);
            double length = v.Length;

            v.Normalize();
            Vector normal = v.Normal();

            // arrowhead base
            Point headBase = end + (-v * headDepth);

            if (length < headDepth)
            {
                headBase = start;
            }

            // the base of the arrowhead
            Vector baseVector = normal * headSize;
            Point baseTip1 = headBase - baseVector;
            Point baseTip2 = headBase + baseVector;

            f.StartPoint = baseTip1;
            f.Segments.Add(new LineSegment() { Point = end });
            f.Segments.Add(new LineSegment() { Point = baseTip2 });

            if (length < headDepth)
            {
                // just do the triangle then
            }
            else
            {
                // keep going and include the stem of the arrowhead

                Vector stemVector = normal * thickness;
                
                Point baseStem1 = headBase - stemVector;
                Point baseStem2 = headBase + stemVector;

                Point startStem1 = start - stemVector;
                Point startStem2 = start + stemVector;


                f.Segments.Add(new LineSegment() { Point = baseStem2 });
                f.Segments.Add(new LineSegment() { Point = startStem2 });
                f.Segments.Add(new LineSegment() { Point = startStem1 });
                f.Segments.Add(new LineSegment() { Point = baseStem1 });
            }

            return g;
        }

        public static bool IsAlmost(this double d, double v, int decimalPlaces = 5)
        {
            double diff = Math.Round(v - d, decimalPlaces);
            return (diff == 0);
        }

    }
}
