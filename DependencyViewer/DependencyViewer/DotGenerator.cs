using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Windows.Controls;
using System.IO;

namespace DependencyViewer {
    class DotGenerator : GraphGenerator {

        public override string FileFilter {
            get {
                return "DOT Graphs (*.dot)|*.dot";
            }
        }

        public override string Label {
            get {
                return "DOT Graph";
            }
        }

        public override void Prepare() {
            base.Prepare();            
        }

        public override void Create(Panel container) {
            base.Create(container);
            foreach (string file in FileNames) {
                LoadGraph(file);
            }
        }

        public static void SaveGraph(GraphGenerator gen, string filename) {            
            // sort nodes by # edges.
            List<GraphNode> nodes = new List<GraphNode>(gen.Nodes);
            EdgeMap map = gen.Edges;

            Comparison<GraphNode> comparer = delegate(GraphNode a, GraphNode b) {
                if (a == b) return 0;
                if (!map.ContainsKey(a)) return 1;
                if (!map.ContainsKey(b)) return -1;
                return map[b].Count - map[a].Count;
            };
            nodes.Sort(comparer);

            using (TextWriter w = new StreamWriter(filename, false, Encoding.UTF8))
            {
                w.WriteLine("digraph G {");
                
                foreach (GraphNode n in nodes)
                {
                    w.WriteLine(string.Format("\"{0}\" [ label=\"{1}\" ]",
                        n.Id, n.Label));
                }

                // sort edges by weight
                List<GraphEdge> allEdges = new List<GraphEdge>();
                foreach (IDictionary<GraphNode, GraphEdge> e in map.Values)
                {
                    allEdges.AddRange(e.Values);
                }
                Comparison<GraphEdge> byWeight = delegate(GraphEdge a, GraphEdge b)
                {
                    return b.Weight - a.Weight;
                };
                allEdges.Sort(byWeight);

                foreach (GraphEdge e in allEdges)
                {
                    w.Write(string.Format("\"{0}\" -> \"{1}\"",
                            e.Source.Id, e.Target.Id));
                    if (!string.IsNullOrEmpty(e.Label))
                    {
                        w.Write(" [label=\"{0}\"]", e.Label);
                    }
                    w.WriteLine();
                }

                w.WriteLine("}"); // end of graph
            }
        }


        IDictionary<string, GraphNode> nodemap;
        //==================================================================
        private void LoadGraph(string fileName) {
            
            this.nodemap = new Dictionary<string, GraphNode>();

            // todo: .dot parser...
            throw new Exception("Load .dot files is not implemented yet...");
        }

        GraphNode GetOrAddNode(string id, GraphNode parent) {
            GraphNode node = null;
            if (nodemap.ContainsKey(id)) {
                node = nodemap[id];
            } else {
                node = new GraphNode();
                node.Id = id;
                nodemap[id] = node;
                if (parent != null)
                {
                    parent.Nodes.Add(node);
                    node.Parent = parent;
                } else {
                    AddNode(node);
                }
            }
            return node;
        }
    }
}

