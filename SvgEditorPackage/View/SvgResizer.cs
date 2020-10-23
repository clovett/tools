using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.VisualStudio.XmlEditor;

namespace Microsoft.SvgEditorPackage.View
{
    public class SvgResizer : FrameworkElement
    {
        SvgElement adorned;
        Rect bounds;
        Rect initialBounds;

        public SvgResizer(SvgElement adorned)
        {
            this.adorned = adorned;

            this.bounds = adorned.Bounds;
            Transform t = adorned.RenderTransform;
            if (t != null)
            {
                bounds = t.TransformBounds(bounds);
            }

            SizeToBounds();
        }

        public SvgElement Adorned { get { return adorned; } }

        private void SizeToBounds()
        {
            Canvas.SetLeft(this, bounds.Left - ThumbSize);
            Canvas.SetTop(this, bounds.Top - ThumbSize);
            Size size = bounds.Size;
            this.Width = size.Width + (ThumbSize * 2);
            this.Height = size.Height + (ThumbSize * 2);
        }

        static Brush ThumbBrush = new SolidColorBrush(Color.FromRgb(0x7D, 0xA2, 0xCE));
        static Brush BorderBrush = new SolidColorBrush(Color.FromRgb(0xAE, 0xC5, 0xE0));
        const double ThumbSize = 8;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Pen pen = new Pen(BorderBrush, 1);

            var box = new Rect(0, 0, this.Width, this.Height);
            box.Inflate(-ThumbSize / 2, -ThumbSize / 2);
            drawingContext.DrawRectangle(Brushes.Transparent, pen, box);

            drawingContext.DrawRectangle(ThumbBrush, null, TopLeftThumb);
            drawingContext.DrawRectangle(ThumbBrush, null, TopMiddleThumb);
            drawingContext.DrawRectangle(ThumbBrush, null, TopRightThumb);

            drawingContext.DrawRectangle(ThumbBrush, null, MiddleLeftThumb);
            drawingContext.DrawRectangle(ThumbBrush, null, MiddleRightThumb);

            drawingContext.DrawRectangle(ThumbBrush, null, BottomLeftThumb);
            drawingContext.DrawRectangle(ThumbBrush, null, BottomMiddleThumb);
            drawingContext.DrawRectangle(ThumbBrush, null, BottomRightThumb);

        }

        enum Corner { None, Middle, TopLeft, TopMiddle, TopRight, MiddleLeft, MiddleRight, BottomLeft, BottomMiddle, BottomRight };

        Corner dragging;
        Point mouseDownPosition;

        protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            Cursor = System.Windows.Input.Cursors.Arrow;

            Point pos = e.GetPosition(this);
            if (TopLeftThumb.Contains(pos))
            {
                dragging = Corner.TopLeft;
            }
            else if (TopMiddleThumb.Contains(pos))
            {
                dragging = Corner.TopMiddle;
            }
            else if (TopRightThumb.Contains(pos))
            {
                dragging = Corner.TopRight;
            }
            else if (MiddleLeftThumb.Contains(pos))
            {
                dragging = Corner.MiddleLeft;
            }
            else if (MiddleRightThumb.Contains(pos))
            {
                dragging = Corner.MiddleRight;
            }
            else if (BottomLeftThumb.Contains(pos))
            {
                dragging = Corner.BottomLeft;
            }
            else if (BottomMiddleThumb.Contains(pos))
            {
                dragging = Corner.BottomMiddle;
            }
            else if (BottomRightThumb.Contains(pos))
            {
                dragging = Corner.BottomRight;
            }
            else
            {
                dragging = Corner.Middle;
            }

            initialBounds = bounds;
            
            e.Handled = true;
            mouseDownPosition = pos;
            System.Windows.Input.Mouse.Capture(this);            
        }

        protected override void OnPreviewMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            System.Windows.Input.Mouse.Capture(null);
            dragging = Corner.None;
        }

        protected override void OnLostMouseCapture(System.Windows.Input.MouseEventArgs e)
        {
            base.OnLostMouseCapture(e); 
            dragging = Corner.None;
            adorned.CommitEdits("Resize");
        }

        private void OnResize()
        {
            bounds = adorned.Resize(bounds);
            SizeToBounds();
        }

        protected override void OnPreviewMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            
            Point pos = e.GetPosition(this);
            double dx = pos.X - mouseDownPosition.X;
            double dy = pos.Y - mouseDownPosition.Y;

            if (dragging == Corner.Middle)
            {
                bounds.X += dx;
                bounds.Y += dy;
                OnResize();
            }
            else if (dragging != Corner.None)
            {
                // actually move the shape!
                switch (dragging)
                {
                    case Corner.TopLeft:
                        bounds.X += dx;
                        bounds.Y += dy;
                        bounds.Width = Math.Max(0, bounds.Width - dx);
                        bounds.Height = Math.Max(0, bounds.Height - dy);
                        break;
                    case Corner.TopMiddle:
                        bounds.Y += dy;
                        bounds.Height = Math.Max(0, bounds.Height - dy);
                        break;
                    case Corner.TopRight:
                        bounds.Width = Math.Max(0, initialBounds.Width + dx);
                        bounds.Height = Math.Max(0, bounds.Height - dy);
                        bounds.Y += dy;
                        break;
                    case Corner.MiddleLeft:
                        bounds.Width = Math.Max(0, bounds.Width - dx);
                        bounds.X += dx;
                        break;
                    case Corner.MiddleRight:
                        bounds.Width = Math.Max(0, initialBounds.Width + dx);
                        break;
                    case Corner.BottomLeft:
                        bounds.X += dx;
                        bounds.Height = Math.Max(0, initialBounds.Height + dy);
                        bounds.Width = Math.Max(0, bounds.Width - dx);
                        break;
                    case Corner.BottomMiddle:
                        bounds.Height = Math.Max(0, initialBounds.Height + dy);
                        break;
                    case Corner.BottomRight:
                        bounds.Width = Math.Max(0, initialBounds.Width + dx);
                        bounds.Height = Math.Max(0, initialBounds.Height + dy);
                        break;
                }
                OnResize();
            }
            else
            {
                if (TopLeftThumb.Contains(pos))
                {
                    Cursor = System.Windows.Input.Cursors.SizeNWSE;
                }
                else if (TopMiddleThumb.Contains(pos))
                {
                    Cursor = System.Windows.Input.Cursors.SizeNS;
                }
                else if (TopRightThumb.Contains(pos))
                {
                    Cursor = System.Windows.Input.Cursors.SizeNESW;
                }
                else if (MiddleLeftThumb.Contains(pos))
                {
                    Cursor = System.Windows.Input.Cursors.SizeWE;
                }
                else if (MiddleRightThumb.Contains(pos))
                {
                    Cursor = System.Windows.Input.Cursors.SizeWE;
                }
                else if (BottomLeftThumb.Contains(pos))
                {
                    Cursor = System.Windows.Input.Cursors.SizeNESW;
                }
                else if (BottomMiddleThumb.Contains(pos))
                {
                    Cursor = System.Windows.Input.Cursors.SizeNS;
                }
                else if (BottomRightThumb.Contains(pos))
                {
                    Cursor = System.Windows.Input.Cursors.SizeNWSE;
                }
                else
                {
                    Cursor = System.Windows.Input.Cursors.Arrow;
                }
            }
        }

        public Rect TopLeftThumb
        {
            get { return new Rect(0, 0, ThumbSize, ThumbSize); }
        }
        public Rect TopMiddleThumb
        {
            get { return new Rect(this.Width / 2 - ThumbSize / 2, 0, ThumbSize, ThumbSize); }
        }
        public Rect TopRightThumb
        {
            get { return new Rect(this.Width - ThumbSize, 0, ThumbSize, ThumbSize); }
        }
        public Rect MiddleLeftThumb
        {
            get { return new Rect(0, this.Height / 2 - ThumbSize / 2, ThumbSize, ThumbSize); }
        }

        public Rect MiddleRightThumb
        {
            get { return new Rect(this.Width - ThumbSize, this.Height / 2 - ThumbSize / 2, ThumbSize, ThumbSize); }
        }

        public Rect BottomLeftThumb
        {
            get { return new Rect(0, this.Height - ThumbSize, ThumbSize, ThumbSize); }
        }

        public Rect BottomMiddleThumb
        {
            get { return new Rect(this.Width / 2 - ThumbSize / 2, this.Height - ThumbSize, ThumbSize, ThumbSize); }
        }

        public Rect BottomRightThumb
        {
            get { return new Rect(this.Width - ThumbSize, this.Height - ThumbSize, ThumbSize, ThumbSize); }
        }
    }
}
