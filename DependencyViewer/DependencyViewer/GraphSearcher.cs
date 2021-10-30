using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DependencyViewer {
    internal class GraphSearcher : IFindTarget{
        EditingContext context;
        IList<UIElement> found = new List<UIElement>();
        int pos = -1;
        string lastExpr;

        public GraphSearcher(EditingContext context) {
            this.context = context;
            context.GraphReloaded += new EventHandler(OnGraphReloaded);
        }

        void OnGraphReloaded(object sender, EventArgs e) {
            found.Clear(); // clear stale references.
        }

        public delegate bool MatchElement(UIElement e);

        void FindAll(string toFind) {
            found.Clear();
            foreach (UIElement c in this.context.Container.Children) {
                GraphCanvas graph = c as GraphCanvas;
                if (graph != null) {
                    SearchGraph(toFind, graph);
                }
            }
        }

        private void SearchGraph(string toFind, GraphCanvas graph)
        {
            foreach (UIElement e in graph.GraphElements)
            {
                string label = GetLabel(e);
                if (StringContains(label, toFind))
                {
                    found.Add(e);
                }
                if (e is SubgraphShape)
                {
                    SubgraphShape s = (SubgraphShape)e;
                    SearchGraph(toFind, s.Canvas);
                }
            }
        }

        string GetLabel(UIElement e) {
            NodeShape node = e as NodeShape;
            if (node != null) {
                return node.Label;
            }
            EdgeShape edge = e as EdgeShape;
            if (edge != null) {
                return edge.Model.Label;
            }
            return null;
        }

        bool StringContains(string value, string toFind) {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.ToLowerInvariant().Contains(toFind.ToLowerInvariant())) return true;
            return false;
        }

        private void Select(UIElement e, bool select) {
            ISelectable selectable = e as ISelectable;
            if (selectable != null) {
                context.ClearSelection();
                context.AddSelection(selectable);
            }
        }


        #region IFindTarget Members

        public string FindNext(string toFind) {

            if (pos >= 0 && pos < found.Count && found.Count > 0) {
                UIElement e = found[pos];
                Select(e, false);
            }
            if (this.lastExpr != toFind || found.Count == 0) {
                FindAll(toFind);
                this.lastExpr = toFind;
                pos = -1;
            }
            pos++;
            if (pos >= found.Count) {
                pos = 0;
            }
            if (found.Count > 0) {
                UIElement e = found[pos];
                Select(e, true);
                return GetLabel(e);
            }
            return null;
        }

        public string FindNext() {
            if (!string.IsNullOrEmpty(this.lastExpr)) {
                return FindNext(this.lastExpr);
            }
            return null;
        }

        public int SelectAll(string toFind) {
            FindAll(toFind);
            this.lastExpr = toFind;
            pos = -1;

            context.ClearSelection();
            foreach (UIElement e in found) {
                ISelectable selectable = e as ISelectable;
                if (selectable != null) {
                    context.AddSelection(selectable);
                }
            };
            return found.Count;
        }

        #endregion
    }
}
