using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DependencyViewer {

    // Provides slightly better keyboard navigation over the graph than the default behavior provided by WPF,
    // for example, you can hold down the shift key and navigate to nodes connected by edges.
    public class GraphKeyboadNavigator {
        GraphCanvas diagram;

        public GraphKeyboadNavigator(GraphCanvas diagram) {
            this.diagram = diagram;
            diagram.LostKeyboardFocus += new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus);
            diagram.KeyDown += new KeyEventHandler(OnKeyDown);
        }

        void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            e.Handled = true;
        }

        Rect GetNodeBounds(NodeShape n) {
            Rect bounds = new Rect(0, 0, n.ActualWidth, n.ActualHeight);
            bounds = n.TransformToAncestor(this.diagram).TransformBounds(bounds);
            return bounds;
        }

        Point Center(Rect r) {
            return new Point(r.Left + (r.Width / 2), r.Top + (r.Height / 2));
        }

        private bool InDirection(NodeShape selected, Key key, ref Point u, out double dist, out double offset) {
            // Step to the direction has to be significant: more than half of height or width 
            // of the node correspondingly. 
            // For example if we are going up than we discard all nodes with y only slightly 
            // bigger the y of the current selected node: such nodes can be reached by
            // left or right steps

            offset = 0;
            dist = Math.Sqrt((u.X * u.X) + (u.Y * u.Y));
            Rect bounds = GetNodeBounds(selected);
            switch (key) {
                case Key.Down:
                    if (u.Y > bounds.Height / 2) {
                        offset = Math.Abs(u.X);
                        if (offset < bounds.Width / 2) offset = 0;
                        return true;
                    }
                    break;
                case Key.Up:
                    if (u.Y < -bounds.Height / 2) {
                        offset = Math.Abs(u.X);
                        if (offset < bounds.Width / 2) offset = 0;
                        return true;
                    }

                    break;
                case Key.Left:
                    if (u.X < -bounds.Width / 2) {
                        offset = Math.Abs(u.Y);
                        if (offset < bounds.Height / 2) offset = 0;
                        return true;
                    }
                    break;
                case Key.Right:
                    if (u.X > bounds.Width / 2) {
                        offset = Math.Abs(u.Y);
                        if (offset < bounds.Height / 2) offset = 0;
                        return true;
                    }
                    break;
            }
            return false;

        }

        void OnKeyDown(object sender, KeyEventArgs e) {
             // There is a default navigation provided by System.Windows.Input.KeyboardNavigation, but it
            // only navigates visible elements - whereas we want to also navgate elements offscreen because
            // when we select them they will automatically scroll into view, and besides, we can tweak the
            // algorithm a bit so it works a bit better.

            // The algorithm finds two nodes to navigate to - one is considered the "best" if it is exactly in 
            // the direction of the keystroke.  If none is found exactly in the right direction then it falls
            // back on the other one.

            // If the shift key is down then it only searches nodes that have connected incoming or outgoing
            // edges, with a preference for outgoing.

            bool shift = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0;
            Key key = e.Key;
            if (key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down) {
                NodeShape focus = Keyboard.FocusedElement as NodeShape;
                if (focus != null) {
                    Point center = Center(GetNodeBounds(focus));

                    double bestMinDist = Double.MaxValue;
                    double minDist = Double.MaxValue;

                    double bestOutgoingMinDist = Double.MaxValue;
                    double minOutgoingDist = Double.MaxValue;

                    NodeShape bestNode = null;
                    NodeShape okNode = null;

                    NodeShape bestOutgoingNode = null;
                    NodeShape okOutgoingNode = null;
                    List<EdgeShape> edges = GetEdgesTo(focus);

                    foreach (object o in diagram.GraphElements) {
                        NodeShape nodeShape = o as NodeShape;
                        if (nodeShape != null && nodeShape != focus) {
                            if (nodeShape.GetType() == focus.GetType()) {
                                bool outgoing = false;
                                if (!shift || IsConnected(edges, nodeShape, out outgoing)) {
                                    Point c = Center(GetNodeBounds(nodeShape));
                                    Point u = new Point(c.X - center.X, c.Y - center.Y);
                                    double d;
                                    double offset;
                                    if (InDirection(focus, key, ref u, out d, out offset)) {
                                        if (offset == 0) {
                                            if (d < bestMinDist) {
                                                bestMinDist = d;
                                                bestNode = nodeShape;
                                            }
                                            if (outgoing && d < bestOutgoingMinDist) {
                                                bestOutgoingMinDist = d;
                                                bestOutgoingNode = nodeShape;
                                            }
                                        }
                                        double f = d + offset;
                                        if (f < minDist) {
                                            minDist = d + offset;
                                            okNode = nodeShape;
                                        }
                                        if (outgoing && f < minOutgoingDist) {
                                            minOutgoingDist = d + offset;
                                            okOutgoingNode = nodeShape;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (shift) {
                        // Then favor the outgoing edges if we have any.
                        if (bestOutgoingNode == null) {
                            bestOutgoingNode = okOutgoingNode;
                        }
                        if (bestOutgoingNode != null) {
                            bestNode = bestOutgoingNode;
                        }
                    }

                    if (bestNode == null)
                        bestNode = okNode;

                    if (bestNode != null) {
                        bestNode.Focus();
                        e.Handled = true;
                    }
                }
            }

        }

        List<EdgeShape> GetEdgesTo(NodeShape selected) {
            List<EdgeShape> result = new List<EdgeShape>();
            INode id = selected.Node;
            foreach (System.Windows.UIElement c in this.diagram.GraphElements) {
                EdgeShape e = (c as EdgeShape);
                if (e != null) {
                    if (e.Edge.Source == id || e.Edge.Target == id) {
                        result.Add(e);
                    }
                }
            }
            return result;
        }

        static bool IsConnected(List<EdgeShape> edges, NodeShape b, out bool outgoing) {
            bool result = false;
            INode bid = b.Node;
            outgoing = false;
            foreach (EdgeShape e in edges) {
                if (e.Edge.Target == bid) {
                    outgoing = true;
                    return true;
                } else if (e.Edge.Source == bid) {
                    result = true; // but keep looking for an outgoing edge.
                }
            }
            return result;
        }
    }
}
