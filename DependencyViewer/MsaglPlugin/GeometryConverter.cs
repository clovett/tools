using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Media;
using Point = System.Windows.Point;
using MPoint = Microsoft.Msagl.Point;
using System.Diagnostics;

namespace MsaglPlugin {

    class GeometryConverter {

        internal static Microsoft.Msagl.Splines.ICurve ConvertGeometryToICurve(GeneralTransform gt, Geometry g, double width, double height)
        {
            Microsoft.Msagl.Splines.ICurve c = ConvertGeometryToICurve(g, width, height);
            Transform t = gt as Transform;
            if (t != null) {
                Matrix m = t.Value;
                if (!m.IsIdentity) {
                    return c.Transform(new Microsoft.Msagl.Splines.PlaneTransformation(m.M11, m.M12, m.OffsetX, m.M21, m.M22, m.OffsetY));
                }
            }
            return c;
        }

        internal static Microsoft.Msagl.Splines.ICurve ConvertGeometryToICurve(Geometry g, double width, double height)
        {
            //double d = Microsoft.Msagl.Routing.PointSize;
            //Pen pen = g.Pen;
            //if (pen != null) pen = new Pen(Brushes.Black, 1);

            // todo: make this work... and apply v.RenderTransform.
            //g = g.GetWidenedPathGeometry(pen);

            RectangleGeometry rect = g as RectangleGeometry;
            if (rect != null) {
                return Microsoft.Msagl.Splines.CurveFactory.CreateBox(width, height, rect.RadiusX, rect.RadiusY, new MPoint(0, 0));
            }
            EllipseGeometry ellipse = g as EllipseGeometry;
            if (ellipse != null) {
                double xr = width / 2;
                double yr = height / 2;
                return Microsoft.Msagl.Splines.CurveFactory.CreateEllipse(xr, yr, new MPoint(0, 0));
            }
            LineGeometry line = g as LineGeometry;
            if (line != null) {
                return Microsoft.Msagl.Splines.CurveFactory.CreateBox(width, height, 0, 0, new MPoint(0, 0));
            }

            //CombinedGeometry combo = g as CombinedGeometry;
            //if (combo != null) {
            //    g = combo.GetWidenedPathGeometry(pen);
            //}

            //StreamGeometry stm = g as StreamGeometry;
            //if (stm != null) {
            //    g = stm.GetWidenedPathGeometry(pen);
            //}

            PathGeometry p = g as PathGeometry;
            if (p != null) {
                Microsoft.Msagl.Splines.Curve c = new Microsoft.Msagl.Splines.Curve();
                foreach (PathFigure f in p.Figures) {
                    Point start = f.StartPoint;
                    foreach (PathSegment seg in f.Segments) {
                        LineSegment l = seg as LineSegment;
                        if (l != null) {
                            Point pt = l.Point;
                            c.AddSegment(new Microsoft.Msagl.Splines.LineSegment(start.X, start.Y, pt.X, pt.Y));
                            start = pt;
                            continue;
                        }
                        PolyBezierSegment ps = seg as PolyBezierSegment;
                        if (ps != null) {
                            for (int i = 0, n = ps.Points.Count; i + 3 < n; i += 3) {
                                start = AddBezierSegment(c, start, ps.Points[i + 0], ps.Points[i + 1], ps.Points[i + 3]);
                            }
                            continue;
                        }

                        BezierSegment bs = seg as BezierSegment;
                        if (bs != null) {
                            start = AddBezierSegment(c, start, bs.Point1, bs.Point2, bs.Point3);
                            continue;
                        }

                        ArcSegment arc = seg as ArcSegment;
                        if (arc != null) {
                            start = AddArcSegment(c, start, arc);
                            continue;
                        }
                        PolyLineSegment pls = seg as PolyLineSegment;
                        if (pls != null) {
                            foreach (Point pt in pls.Points) {
                                c.AddSegment(new Microsoft.Msagl.Splines.LineSegment(Convert(start), Convert(pt)));
                                start = pt;
                            }
                            continue;
                        }
                        PolyQuadraticBezierSegment qs = seg as PolyQuadraticBezierSegment;
                        if (qs != null) {
                            for (int i = 0, n = qs.Points.Count; i + 2 < n; i += 2) {
                                start = AddQuadraticSegment(c, start, qs.Points[i], qs.Points[i + 1]);
                            }
                        }

                        QuadraticBezierSegment quad = seg as QuadraticBezierSegment;
                        if (quad != null) {
                            start = AddQuadraticSegment(c, start, quad.Point1, quad.Point2);
                        }

                        Debug.Assert(false, "Unexpected PathSegment type: " + seg.GetType().FullName);
                    }
                }
                // Now scale up the curve to fit the bounding box for the label.
                return c.Transform(new Microsoft.Msagl.Splines.PlaneTransformation(width, 0, 0, 0, height, 0));
            } else {
                Debug.Assert(false, "Unexpected geometry type: " + g.GetType().FullName);
            }

            // default for unexpected geometries.
            return Microsoft.Msagl.Splines.CurveFactory.CreateBox(width, height, 0, 0, new MPoint(0, 0));
        }

        static MPoint Convert(Point p) {
            return new MPoint(p.X, p.Y);
        }

        static Point AddArcSegment(Microsoft.Msagl.Splines.Curve c, Point start, ArcSegment arc)
        {
            double angle = arc.RotationAngle;
            double w = arc.Size.Width;
            double h = arc.Size.Height;
            MPoint a = new MPoint(w * Math.Cos(angle), w * Math.Sin(angle));
            MPoint b = new MPoint(h * Math.Cos(angle), h * Math.Sin(angle));
            Point end = arc.Point;
            double parStart = start.X == 0 ? Math.Sign(start.Y) * Math.PI / 2 : Math.Atan(start.Y / start.X);
            double parEnd = end.X == 0 ? Math.Sign(end.Y) * Math.PI / 2 : Math.Atan(end.Y / end.X);
            c.AddSegment(new Microsoft.Msagl.Splines.Ellipse(parStart - angle, parEnd - angle, a, b, new MPoint(0, 0)));
            return end;
        }

        static Point AddBezierSegment(Microsoft.Msagl.Splines.Curve c, Point start, Point x, Point y, Point z)
        {
            c.AddSegment(new Microsoft.Msagl.Splines.CubicBezierSegment(
                    Convert(start),
                    Convert(x),
                    Convert(y),
                    Convert(z)));
            return z;
        }

        static Point AddQuadraticSegment(Microsoft.Msagl.Splines.Curve c, Point s, Point a, Point b)
        {
            MPoint start = Convert(s);
            MPoint p1 = Convert(a);
            MPoint p2 = Convert(b);
            // To convert to CubicBezierSegment we take 2/3rds of the vector from start to p1
            // and 2/3rds of the vector from p2 to p1 and make those the control points.
            MPoint v1 = p1 - start;
            v1.Normalize();
            v1 *= 2;
            v1 /= 3;
            MPoint v2 = p1 - p2;
            v2.Normalize();
            v2 *= 2;
            v2 /= 3;
            MPoint control1 = start + v1;
            MPoint control2 = p2 + v2;
            c.AddSegment(new Microsoft.Msagl.Splines.CubicBezierSegment(
                    start,
                    control1,
                    control2,
                    p2));
            return b;
        }
    }
}
