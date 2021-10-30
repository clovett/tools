using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Windows.Controls;

namespace DependencyViewer {
    class DgmlGenerator : GraphGenerator {

        const string DgmlNamespace = "http://schemas.microsoft.com/vs/2009/dgml";

        public override string FileFilter {
            get {
                return "DGML Graphs (*.dgml)|*.dgml";
            }
        }

        public override string Label {
            get {
                return "Dgml Graph";
            }
        }

        public override void Prepare() {
            base.Prepare();            
        }

        public override void Create(Panel container) {
            base.Create(container);
            foreach (string file in FileNames) {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                LoadGraph(doc);
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

            IDictionary<string, GraphNode> groups = new Dictionary<string, GraphNode>();

            using (XmlTextWriter w = new XmlTextWriter(filename, Encoding.UTF8))
            {
                w.Formatting = Formatting.Indented;
                w.WriteStartElement("DirectedGraph", DgmlNamespace);

                w.WriteStartElement("Nodes", DgmlNamespace);
                foreach (GraphNode n in nodes)
                {                    
                    WriteNode(w, n, groups);
                }
                w.WriteEndElement(); //Nodes

                w.WriteStartElement("Links", DgmlNamespace);
                List<GraphEdge> allEdges = new List<GraphEdge>();
                foreach (IDictionary<GraphNode,GraphEdge> e in map.Values)
                {
                    allEdges.AddRange(e.Values);
                }
                

                // sort edges by weight
                Comparison<GraphEdge> byWeight = delegate(GraphEdge a, GraphEdge b)
                {
                    return b.Weight - a.Weight;
                };
                allEdges.Sort(byWeight);
                foreach (GraphEdge e in allEdges)
                {
                    w.WriteStartElement("Link", DgmlNamespace);
                    w.WriteAttributeString("Source", e.Source.Id);
                    w.WriteAttributeString("Target", e.Target.Id);
                    w.WriteAttributeString("Category", e.EdgeType);
                    if (e.Weight > 1)
                    {
                        w.WriteAttributeString("Weight", e.Weight.ToString());
                    }
                    WriteOptionalAttribute(w, "Label", e.Label);
                    w.WriteEndElement();
                }

                foreach (string id in groups.Keys)
                {
                    GraphNode g = groups[id];
                    foreach (GraphNode c in g.Nodes)
                    {
                        w.WriteStartElement("Link", DgmlNamespace);
                        w.WriteAttributeString("Source", id);
                        w.WriteAttributeString("Target", c.Id);
                        w.WriteAttributeString("Category", "Contains");
                        w.WriteEndElement(); // Link
                    }
                }

                w.WriteEndElement(); // Links
                w.WriteEndElement(); // DirectedGraph
            }
        }

        public static string GetFullId(GraphNode n)
        {
            if (n.Parent == null) return n.Id;
            return GetFullId(n.Parent) + "." + n.Id;
        }

        private static void WriteNode(XmlTextWriter w, GraphNode n, IDictionary<string, GraphNode> groups)
        {
            string id = GetFullId(n);

            w.WriteStartElement("Node", DgmlNamespace);
            bool hasChildren = n.Nodes != null && n.Nodes.Count > 0;
            w.WriteAttributeString("Id", n.Id);
            w.WriteAttributeString("Label", n.Label);
            WriteOptionalAttribute(w, "Category", n.NodeType);
            
            if (hasChildren)
            {
                w.WriteAttributeString("Group", "Expanded");
            }
            if (n.Tip != n.Id && n.Tip != n.NodeType)
            {
                WriteOptionalAttribute(w, "Tip", n.Tip);
            }
            w.WriteEndElement(); //Node
            
            if (hasChildren)
            {
                // then 'n' is a group node.
                if (!groups.ContainsKey(id))
                {                   
                    groups[id] = n;
                }
                foreach (GraphNode c in n.Nodes)
                {
                    WriteNode(w, c, groups);
                }
            }
        }

        static void WriteOptionalAttribute(XmlWriter w, string name, string value) {
            if (!string.IsNullOrEmpty(value)) {
                w.WriteAttributeString(name, value);
            }
        }

        IDictionary<string, GraphNode> nodemap = new Dictionary<string, GraphNode>();
        //==================================================================
        private void LoadGraph(XmlDocument doc) {
            nodemap.Clear();

            XmlElement root = doc.DocumentElement;
            foreach (XmlNode e in root.ChildNodes)
            {
                if (e.LocalName == "Nodes")
                {
                    LoadNodes((XmlElement)e);
                }
                else if (e.LocalName == "Links")
                {
                    LoadLinks((XmlElement)e);
                }
            }
        }

        bool IsVisible(XmlElement e)
        {
            if (e.HasAttribute("IsVisible")) {
                bool result = false;
                bool.TryParse(e.GetAttribute("IsVisible"), out result);
                return result;
            }
            if (e.HasAttribute("Visibility")) 
            {
                return string.Compare(e.GetAttribute("Visibility"), "Visible", StringComparison.OrdinalIgnoreCase) == 0;
            }
            return true; // default is visible.
        }

        private void LoadNodes(XmlElement nodes) {

            foreach (XmlNode n in nodes.ChildNodes) 
            {
                if (n.LocalName == "Node" && IsVisible((XmlElement)n)) {
                    GraphNode node = AddNode((XmlElement)n);

                    //string group = e.GetAttribute("Group");
                    //if (!string.IsNullOrEmpty(group))
                    //{
                    //    GraphNode parent = null;
                    //    parent = GetOrAddNode(group, null);
                    //    from = GetFullId(parent) + "." + from;
                    //    to = GetFullId(parent) + "." + to;
                    //}
                }
            }
        }

        private void LoadLinks(XmlElement links) 
        {
            foreach (XmlNode n in links.ChildNodes)
            {
                if (n.LocalName == "Link" && IsVisible((XmlElement)n))
                {
                    XmlElement e = (XmlElement)n;
                    string from = e.GetAttribute("Source");
                    string to = e.GetAttribute("Target");
                    string category = e.GetAttribute("Category");
                    
                    if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
                    {
                        GraphNode src = GetOrAddNode(from);
                        GraphNode target = GetOrAddNode(to);

                        GraphEdge edge = AddEdge(src, target, category);
                        if (edge != null)
                        {
                            edge.Tip = e.GetAttribute("Details");
                            edge.Label = e.GetAttribute("Label");
                        }
                    }
                }
            }
        }


        GraphNode AddNode(XmlElement e) {
            string id = e.GetAttribute("Id");
            if (string.IsNullOrEmpty(id))
            {
                id = e.GetAttribute("Id");
            }
            if (!string.IsNullOrEmpty(id)) {
                string name = e.GetAttribute("Label");
                if (string.IsNullOrEmpty(name))
                {
                    name = e.GetAttribute("Label");
                }
                string fullid = id;
                //GraphNode parent = null;
                //string group = e.GetAttribute("Group");
                //if (!string.IsNullOrEmpty(group))
                //{
                //    parent = GetOrAddNode(group, null);
                //    fullid = GetFullId(parent) + "." + id;
                //}

                GraphNode node = GetOrAddNode(fullid);
                node.Label = name;
                node.Id = id;
                node.NodeType = e.GetAttribute("Category");
                return node;
            }
            return null;
        }

        GraphNode GetOrAddNode(string id) {
            GraphNode node = null;
            if (nodemap.ContainsKey(id)) {
                node = nodemap[id];
            } else {
                node = new GraphNode();
                node.Id = id;
                nodemap[id] = node;
                AddNode(node);
            }
            return node;
        }
    }
}

