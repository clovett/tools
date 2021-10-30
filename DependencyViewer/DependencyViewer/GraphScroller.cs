using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Sample.Controls;

namespace DependencyViewer {
    public class GraphScroller : Canvas {
        ScrollBar hscroll = new ScrollBar();
        ScrollBar vscroll = new ScrollBar();
        Panel container;
        Panel target;
        Rectangle corner = new Rectangle();
        MapZoom zoom;

        public GraphScroller() {
            hscroll.Orientation = Orientation.Horizontal;
            vscroll.Orientation = Orientation.Vertical;
            this.Children.Add(hscroll);
            this.Children.Add(vscroll);
            corner.Fill = hscroll.Background;
            this.Children.Add(corner);
            hscroll.ValueChanged += new RoutedPropertyChangedEventHandler<double>(OnHScrollValueChanged);
            vscroll.ValueChanged += new RoutedPropertyChangedEventHandler<double>(OnVScrollValueChanged);
        }

        public Panel Target {
            get {
                return target;
            }
            set {
                target = value;
                target.RenderTransform.Changed += new EventHandler(OnContentRenderTransformChanged);
                target.SizeChanged += new SizeChangedEventHandler(OnTargetSizeChanged);
                    
                container = target.Parent as Panel;
                if (container == null) {
                    throw new NotSupportedException("Target panel must be wrapped in another container Panel");
                }
            }
        }

        void OnTargetSizeChanged(object sender, SizeChangedEventArgs e) {
            if (!ignore) {
                UpdateScrollbars();
            }
        }

        internal MapZoom Zoom {
            get { return zoom; }
            set { zoom = value; }
        }

        protected override Size ArrangeOverride(Size arrangeSize) {
            Size s = base.ArrangeOverride(arrangeSize);

            hscroll.Width = arrangeSize.Width - vscroll.Width;
            double hy = arrangeSize.Height - hscroll.Height;
            Canvas.SetTop(hscroll, hy);
            vscroll.Height = arrangeSize.Height - hscroll.Height;
            double vx = arrangeSize.Width - vscroll.Width;
            Canvas.SetLeft(vscroll, vx);

            corner.Width = vscroll.Width;
            corner.Height = hscroll.Height;
            Canvas.SetLeft(corner, vx);
            Canvas.SetTop(corner, hy);

            if (container != null) {
                container.Width = hscroll.Width;
                container.Height = vscroll.Height;
            }
            UpdateScrollbars();
            this.InvalidateVisual();
            return s;
        }

        bool updating;
        void UpdateScrollbars() {
            if (this.target != null) {
                updating = true;
                Rect bounds = new Rect(0, 0, this.target.ActualWidth, this.target.ActualHeight);
                Rect t = target.TransformToAncestor(this).TransformBounds(bounds);
                double w = hscroll.Width;
                double h = vscroll.Height;
                hscroll.Maximum = t.Width - w;
                hscroll.Value = -t.Left;
                hscroll.ViewportSize = w;
                hscroll.SmallChange = w / 10;
                hscroll.LargeChange = w;

                vscroll.Maximum = t.Height - h;
                vscroll.Value = -t.Top;
                vscroll.ViewportSize = h;
                vscroll.SmallChange = h / 10;
                vscroll.LargeChange = h;
                updating = false;
            }
        }

        bool ignore;
        void OnContentRenderTransformChanged(object sender, EventArgs e) {
            if (!ignore) {
                UpdateScrollbars();
            }
        }

        void OnVScrollValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (this.target != null && !updating) {
                ignore = true;
                double newOffsetY = -vscroll.Value;
                zoom.Offset = new Point(zoom.Offset.X, newOffsetY);
                ignore = false;
            }
        }

        void OnHScrollValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (this.target != null && !updating) {
                ignore = true;
                double newOffsetX = -hscroll.Value;
                zoom.Offset = new Point(newOffsetX, zoom.Offset.Y);
                ignore = false;
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e) {
            if (!e.Handled) {
                bool ctrl = IsControlKeyDown;

                switch (e.Key) {
                    case Key.PageUp:
                        if (ctrl) {
                            hscroll.Value = Math.Max(0, hscroll.Value - hscroll.LargeChange);
                        } else {
                            vscroll.Value = Math.Max(0, vscroll.Value - vscroll.LargeChange);
                        }
                        e.Handled = true;
                        break;
                    case Key.PageDown:
                        if (ctrl) {
                            hscroll.Value = Math.Min(hscroll.Maximum, hscroll.Value + hscroll.LargeChange);
                        } else {
                            vscroll.Value = Math.Min(vscroll.Maximum, vscroll.Value + vscroll.LargeChange);
                        }
                        e.Handled = true;
                        break;
                    case Key.Home:
                        vscroll.Value = 0;
                        e.Handled = true;
                        if (ctrl) {
                            hscroll.Value = 0;
                        }
                        break;
                    case Key.End:
                        vscroll.Value = vscroll.Maximum;
                        e.Handled = true;
                        if (ctrl) {
                            hscroll.Value = hscroll.Maximum;
                        }
                        break;
                    default:
                        base.OnKeyDown(e);
                        break;
                }
            } 
        }

        static bool IsControlKeyDown {
            get { return (Keyboard.Modifiers & ModifierKeys.Control) != 0; }
        }

    }
}
