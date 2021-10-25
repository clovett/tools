using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Microsoft.Msagl;
using DependencyViewer;
using System.Diagnostics;
using Point = System.Windows.Point;

namespace MsaglPlugin {

    public class MsaglGraph : IGraph {
        GeometryGraph graph = new GeometryGraph();
        GraphDirection direction;
        object userdata;        

        #region IGraph Members

        public void Clear()
        {
            graph.Edges.Clear();
            graph.NodeMap.Clear();
            graph.BoundingBox = new Microsoft.Msagl.Splines.Rectangle(0, 0, 0, 0);
        }

        public double LayerSeparation {
            get { return graph.LayerSeparation; }
            set { graph.LayerSeparation = value; }
        }

        public double NodeSeparation {
            get { return graph.NodeSeparation; }
            set { graph.NodeSeparation = value; }
        }

        public double AspectRatio {
            get { return graph.AspectRatio; }
            set { graph.AspectRatio = value; }
        }

        public double Left {
            get { return graph.Left; }
        }

        public double Top {
            get { return graph.Top; }
        }

        public double Right {
            get { return graph.Right; }
        }

        public double Bottom {
            get { return graph.Bottom; }
        }

        public Rect BoundingBox {
            get {
                Microsoft.Msagl.Splines.Rectangle r = this.graph.BoundingBox;
                return new Rect(r.Left, r.Bottom, r.Width, r.Height);
            }
        }

        public double Width {
            get { return graph.Width; }
        }

        public double Height {
            get { return graph.Height; }
        }

        public object UserData {
            get { return userdata; }
            set { userdata = value; }
        }


        public void CalculateLayout() {
            graph.CalculateLayout();
        }

        public GraphDirection Direction {
            get { return this.direction; }
            set {
                this.direction = value;
                switch (this.direction) {
                    case GraphDirection.None:
                    case GraphDirection.BottomToTop: // MSAGL is upside-down from WPF coordinate system, so top to bottom is bottom to top!
                        this.graph.Transformation = Rotation(0);
                        break;
                    case GraphDirection.LeftToRight:
                        this.graph.Transformation = Rotation(Math.PI / 2);
                        break;
                    case GraphDirection.RightToLeft:
                        this.graph.Transformation = Rotation(-Math.PI / 2);
                        break;

                    case GraphDirection.TopToBottom:
                        this.graph.Transformation = Rotation(Math.PI);
                        break;

                    default:
                        throw new InvalidOperationException();//"unexpected layout direction");
                }
            }
        }


        public IEdge AddEdge(INode from, INode to) {
            MsaglEdge edge = new MsaglEdge((MsaglNode)from, (MsaglNode)to);
            graph.AddEdge(edge.Edge);
            return edge;
        }

        public INode AddNode(string id) {
            MsaglNode node = new MsaglNode(id);
            graph.AddNode(node.Node);
            return node;
        }

        public INode FindNode(string id) {
            Node n = graph.FindNode(id);
            if (n != null) {
                return n.UserData as MsaglNode;
            }
            return null;
        }

        public IEnumerable<INode> Nodes {
            get {
                foreach (Node n in graph.NodeMap.Values) {
                    yield return n.UserData as MsaglNode;
                }
            }
        }

        public IEnumerable<IEdge> Edges {
            get {
                foreach (Edge e in graph.Edges) {
                    yield return e.UserData as MsaglEdge;
                }
            }
        }
        #endregion

        Microsoft.Msagl.Splines.PlaneTransformation Rotation(double angle) {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Microsoft.Msagl.Splines.PlaneTransformation(cos, -sin, 0, sin, cos, 0);
        }
    }

    class MsaglEdge : IEdge {
        Edge edge;
        MsaglNode to;
        MsaglNode from;
        object userdata;
        double thickness;
        bool bidi;
        double arrowHeadSize = 12;
        const double ArrowAngle = 30.0; //degrees

        public MsaglEdge(MsaglNode from, MsaglNode to) {
            this.from = from;
            this.to = to;
            edge = new Edge(from.Node, to.Node);
            edge.UserData = this;
        }

        internal Edge Edge { get { return edge; } }

        #region IEdge Members

        public Geometry Line {
            get {
                return CreateGeometry();
            }
        }

        public bool BiDirectional
        {
            get { return bidi; }
            set { bidi = value; edge.ArrowheadAtSource = value; }
        }

        public double ArrowHeadSize {
            get { return arrowHeadSize; }
            set { arrowHeadSize = value; }
        }

        public double Thickness {
            get { return this.thickness; }
            set { this.thickness = value; }
        }

        public int Weight {
            get { return edge.Weight; }
            set { edge.Weight = value; }
        }

        public INode Source {
            get { return from; }
            set { from = (MsaglNode)value; }
        }

        public INode Target {
            get { return to; }
            set { to = (MsaglNode)value; }
        }

        public object UserData {
            get { return userdata; }
            set { userdata = value; }
        }

        public Point LabelCenter {
            get { return this.Point(edge.Label.Center); }
        }

        public double LabelWidth {
            get { return edge.Label.Width; }
        }

        public double LabelHeight {
            get { return edge.Label.Height; }
        }

        public void SetLabel(double width, double height)
        {
            edge.Label = new Label(width, height, null);
        }

        #endregion


        Geometry CreateGeometry() {
            // NOTE: StreamGeometry is more efficient for non-animating shapes.
            StreamGeometry stm = new StreamGeometry();
            using (StreamGeometryContext ctx = stm.Open()) {

                Microsoft.Msagl.Splines.Curve curve = edge.Curve as Microsoft.Msagl.Splines.Curve;
                if (curve == null) return stm;

                if (edge.ArrowheadAtSource)
                {
                    Microsoft.Msagl.Point start = curve.Start;
                    Microsoft.Msagl.Point end = edge.ArrowheadAtSourcePosition;
                    AddArrow(ctx, ref start, ref end);
                }
                ctx.BeginFigure(Point(curve.Start), false, false);
                if (edge.ArrowheadAtSource)
                {
                    ctx.LineTo(Point(edge.ArrowheadAtSourcePosition), true, true);
                }
                foreach (Microsoft.Msagl.Splines.ICurve seg in curve.Segments)
                {
                    Microsoft.Msagl.Splines.CubicBezierSegment bezSeg = seg as Microsoft.Msagl.Splines.CubicBezierSegment;
                    if (bezSeg != null)
                    {
                        ctx.BezierTo(Point(bezSeg.B(1)), Point(bezSeg.B(2)), Point(bezSeg.B(3)), true, true);
                        continue;
                    }
                    Microsoft.Msagl.Splines.LineSegment lineSeg = seg as Microsoft.Msagl.Splines.LineSegment ;
                    if (lineSeg != null) {
                        ctx.LineTo(Point(lineSeg.End), true, true);
                        continue;
                    }
                    Debug.Assert(false, "Unexpected segment type: " + seg.GetType().FullName);
                }

                if (edge.ArrowheadAtTarget) {
                    ctx.LineTo(Point(edge.ArrowheadAtTargetPosition), true, true);
                    Microsoft.Msagl.Point start = curve.End;
                    Microsoft.Msagl.Point end = edge.ArrowheadAtTargetPosition;
                    AddArrow(ctx, ref start, ref end);
                }
            }
            stm.Freeze();
            return stm;
        }


        private void ArrowHead(StreamGeometryContext ctx, ref Microsoft.Msagl.Point start, ref Microsoft.Msagl.Point end)
        {
            Vector v = Point(start) - Point(end);
            v.Normalize();
            double size = this.ArrowHeadSize * Math.Max(Math.Min(this.Thickness, 1.5), 0.25);
            v *= size; // depth of arrow

            double len = v.Length; // should = ArrowDepth;
            Point arrowBase = Point(end) + v;
            double radians = (ArrowAngle / 2) * Math.PI / 180;
            double headWidth = len * Math.Tan(radians);
            double dx = headWidth * v.Y / len;
            double dy = headWidth * v.X / len;
            Vector headDelta = new Vector(dx, -dy);

            Point arrowStart = arrowBase + headDelta;
            ctx.BeginFigure(arrowBase + headDelta, true, true);
            ctx.LineTo(Point(end), true, true);
            ctx.LineTo(arrowBase - headDelta, true, true);
            ctx.ArcTo(arrowStart, new Size(size, size), 10, false, SweepDirection.Clockwise, true, true);

        }

        private void AddArrow(StreamGeometryContext ctx, ref Microsoft.Msagl.Point start, ref Microsoft.Msagl.Point end) {

            if (Thickness > 0) {
                ArrowHead(ctx, ref start, ref end);
            } else {

                Microsoft.Msagl.Point dir = end - start;

                Microsoft.Msagl.Point h = dir;
                dir /= dir.Length;
                end -= dir * this.arrowHeadSize * 2;

                Microsoft.Msagl.Point s = new Microsoft.Msagl.Point(-dir.Y, dir.X);

                s *= h.Length * ((double)Math.Tan(ArrowAngle * 0.5f * (Math.PI / 180.0)));

                ctx.BeginFigure(Point(start + s), true, true);
                ctx.LineTo(Point(end), false, true);
                ctx.LineTo(Point(start - s), false, true);

            }
        }

        System.Windows.Point Point(Microsoft.Msagl.Point p2) {
            return new System.Windows.Point(p2.X, p2.Y);
        }


    }

    class MsaglNode : INode {
        Node node;
        double width;
        double height;
        object userdata;

        public MsaglNode(string id) {
            this.node = new Node();
            this.node.Id = id;
            this.node.UserData = this;
        }

        internal Node Node { get { return this.node; } }

        #region INode Members

        public void SetBoundaryCurve(Geometry g, double width, double height) {
            this.width = width;
            this.height = height;
            node.BoundaryCurve = GeometryConverter.ConvertGeometryToICurve(g, width, height);
        }

        public string Id {
            get { return node.Id; }
            set { node.Id = value; }
        }

        public object UserData {
            get { return userdata; }
            set { userdata = value; }
        }

        public Point Center {
            get {
                return new System.Windows.Point(node.Center.X, node.Center.Y);
            }
        }

        public double Width {
            get { return this.width; }
        }

        public double Height {
            get { return this.height; }
        }

        #endregion
    }
   
}
