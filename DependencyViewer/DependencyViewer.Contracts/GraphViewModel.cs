using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace DependencyViewer {

    public interface IHasGraphEdge {
        GraphEdge Model { get; set; }
    }

    public interface IHasGraphNode {
        GraphNode Model { get; set; }
    }

    public class GraphEdge {
        string label;
        GraphNode source;
        GraphNode target;
        TextBlock text;
        int weight;
        string type;
        string tip;
        bool bidi; // bi-directional.

        public string Label { get { return this.label; } set { this.label = value; } }
        public string Tip { get { return this.tip; } set { this.tip = value; } }

        public TextBlock Text { get { return this.text; } set { this.text = value; } }
        public GraphNode Source { get { return this.source; } set { this.source = value; } }
        public GraphNode Target { get { return this.target; } set { this.target = value; } }
        public int Weight { get { return this.weight; } set { this.weight = value; } }
        public string EdgeType { get { return this.type; } set { this.type = value; } }
        public bool BiDirectional { get { return this.bidi; } set { this.bidi = value; } }

    }

    public class GraphNode {
        string id;
        string label;
        TextBlock text;
        string type;
        string access; 
        string tip;
        GraphNode parent;
        object userdata;

        ObservableCollection<GraphNode> nodes;
        ObservableCollection<GraphEdge> edges;

        public string Id { get { return this.id; } set { this.id = value; } }
        public string Label { get { return this.label; } set { this.label = value; } }
        public string Tip { get { return this.tip; } set { this.tip = value; } }
        public TextBlock Text { get { return this.text; } set { this.text = value; } }
        public string NodeType { get { return type; } set { type = value; } }
        public string NodeAccess { get { return access; } set { access = value; } }
        public GraphNode Parent { get { return parent; } set { parent = value; } }
        public object UserData { get { return this.userdata; } set { this.userdata = value; } }

        // if a node has child nodes then it is a subgraph by definition.
        public ObservableCollection<GraphNode> Nodes {
            get {
                if (this.nodes == null) this.nodes = new ObservableCollection<GraphNode>();
                return this.nodes;
            }
            set { this.nodes = value; }
        }

        public ObservableCollection<GraphEdge> Edges {
            get {
                if (this.edges == null) this.edges = new ObservableCollection<GraphEdge>();
                return this.edges;
            }
            set { this.edges = value; }
        }

    }
}