using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace DependencyViewer {

    public enum GraphDirection { None, BottomToTop, TopToBottom, LeftToRight, RightToLeft }

    public interface IGraph {
        void Clear();
        double LayerSeparation { get; set; }
        double NodeSeparation { get; set; }
        double Left { get; }
        double Top { get; }
        double Right { get; }
        double Bottom { get; }
        Rect BoundingBox { get; }
        double Width { get; }
        double Height { get; }
        void CalculateLayout();
        double AspectRatio { get; set; }
        GraphDirection Direction { get; set; }
        IEdge AddEdge(INode from, INode to);
        INode AddNode(string id);
        INode FindNode(string id);
        IEnumerable<INode> Nodes { get; }
        IEnumerable<IEdge> Edges { get; }
        object UserData { get; set; }
    }

    public interface IEdge {
        Geometry Line { get; }
        int Weight { get; set; }
        INode Source { get; set; }
        INode Target { get; set; }
        object UserData { get; set; }
        double Thickness { get; set; }
        double ArrowHeadSize { get; set; }
        bool BiDirectional { get; set; }
        Point LabelCenter { get; }
        double LabelWidth { get; }
        double LabelHeight { get; }
        void SetLabel(double width, double height);
    }

    public interface INode {
        void SetBoundaryCurve(Geometry shape, double width, double height);
        string Id { get; set; }
        object UserData { get; set; }
        Point Center { get; }
        double Width { get; }
        double Height { get; }
    }
}
