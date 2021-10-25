using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.IO;

namespace DependencyViewer {
    using NodeCollection = System.Collections.ObjectModel.ObservableCollection<GraphNode>;
    using EdgeCollection = System.Collections.ObjectModel.ObservableCollection<GraphEdge>;

    public class GraphBuilder {
        IDictionary<GraphNode, GraphCanvas> graphs;
        List<GraphCanvas> result;
        GraphDirection direction;
        IDictionary<GraphEdge, bool> hidden;
        GraphGenerator gen;

        public GraphBuilder(GraphDirection direction) {
            this.direction = direction;
        }

        public void Build(Panel container, GraphGenerator gen) {
            graphs = new Dictionary<GraphNode, GraphCanvas>();
            result = new List<GraphCanvas>();
            hidden = new Dictionary<GraphEdge, bool>();
            this.gen = gen;

            BuildIndependentGraphs(gen);

            // Now split graphs that are too complex!
            double complexityRatio = gen.ComplexityRatio;
            if (complexityRatio != 0) {
                if (LimitComplexity(complexityRatio)) {
                    // then start over with new hidden edge collection.
                    graphs = new Dictionary<GraphNode, GraphCanvas>();
                    result = new List<GraphCanvas>();
                    BuildIndependentGraphs(gen);
                }
            }
            
            // add most complex graphs first.
            Comparison<GraphCanvas> comparer = delegate(GraphCanvas a, GraphCanvas b) {
                return b.Edges.Count - a.Edges.Count;
            };
            result.Sort(comparer);

            if (result.Count == 0) {
                GraphNode node = new GraphNode();
                node.Label = "<Graph is empty>";
                node.Id = "empty";
                AddGraph(node);
            }

            foreach (GraphCanvas graph in result) {
                container.Children.Add(graph);
                graph.OnGraphChanged();
            }
        }

        private bool LimitComplexity(double ratio) {
            bool rc = false;
            foreach (GraphCanvas c in result) {
                if (c.Edges.Count > 50 ) {
                    rc = HideEdges(ratio, new List<GraphEdge>(c.Edges));
                }
                foreach (GraphNode n in c.Nodes)
                {
                    // search for subgraphs.
                    if (n.Edges != null && n.Edges.Count > 50)
                    {
                        HideEdges(n, ratio);
                    }
                }
            }
            return rc;
        }

        private void HideEdges(GraphNode n, double ratio)
        {
            Comparison<GraphEdge> byWeight = delegate(GraphEdge a, GraphEdge b)
            {
                return b.Weight - a.Weight;
            };
            if (n.Edges != null && n.Edges.Count > 50)
            {
                List<GraphEdge> edges = new List<GraphEdge>(n.Edges);
                int max = (int)((double)edges.Count * ratio);
                edges.Sort(byWeight);
                
                System.Collections.ObjectModel.ObservableCollection<GraphEdge> newList = new System.Collections.ObjectModel.ObservableCollection<GraphEdge>();
                int count = 0;
                foreach (GraphEdge e in edges)
                {
                    newList.Add(e);
                    count++;
                    if (count == max) break;
                }
                n.Edges = newList;
            }
        }

        private bool HideEdges(double ratio, List<GraphEdge> edges)
        {
            Comparison<GraphEdge> byWeight = delegate(GraphEdge a, GraphEdge b)
            {
                return b.Weight - a.Weight;
            };
            int max = (int)((double)edges.Count * ratio);
            edges.Sort(byWeight);
            bool rc = false;
            for (int i = max, n = edges.Count; i < n; i++)
            {
                GraphEdge e = edges[i];
                hidden[e] = true;
                rc = true;
            }
            return rc;
        }

        private void BuildIndependentGraphs(GraphGenerator gen) {

            foreach (GraphNode node in gen.Nodes) {
                AddGraph(node);
            }

            EdgeMap map = gen.Edges;
            foreach (GraphNode node in map.Keys) {
                IDictionary<GraphNode, GraphEdge> edges = map[node];
                foreach (GraphNode target in edges.Keys) {
                    if (node.Parent == null && graphs.ContainsKey(node) && graphs.ContainsKey(target)) {
                        GraphEdge edge = edges[target];
                        if (!hidden.ContainsKey(edge)) {
                            GraphCanvas graph1 = graphs[node];
                            GraphCanvas graph2 = graphs[target];
                            EdgeCollection g1edges = graph1.Edges;
                            if (graph1 != graph2) {
                                // Time to merge graphs since they are now connected...
                                NodeCollection g1Nodes = graph1.Nodes;
                                foreach (GraphNode n in graph2.Nodes) {
                                    g1Nodes.Add(n);
                                    graphs[n] = graph1;
                                }
                                foreach (GraphEdge e in graph2.Edges) {
                                    g1edges.Add(e);
                                }
                                result.Remove(graph2);
                            }
                            g1edges.Add(edge);
                        }
                    }
                }
            }
        }

        private GraphCanvas AddGraph(GraphNode node) {
            // create new independent graph to be merged only when we discover an edge that connects it to something.
            GraphCanvas graph = new GraphCanvas();
            graph.Direction = this.direction;
            graph.ShowEdgeLabels = this.gen.ShowEdgeLabels;
            graph.Nodes.Add(node);
            graphs[node] = graph;
            result.Add(graph);
            return graph;
        }
    }
}
