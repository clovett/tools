using Microsoft.Journal.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Microsoft.Journal.Controls
{
    public class ResizerEventArgs : EventArgs
    {
        public Rect NewBounds { get; set; }
    }

    public enum Corner { None, TopLeft, TopRight, BottomLeft, BottomRight };

    public sealed partial class Resizer : UserControl
    {
        Rect initialBounds;


        public Resizer()
        {
            this.InitializeComponent();
            this.SizeChanged += Resizer_SizeChanged;

            SetCorner(TopLeftThumbGhost, Corner.TopLeft);
            SetCorner(TopRightThumbGhost, Corner.TopRight);
            SetCorner(BottomLeftThumbGhost, Corner.BottomLeft);
            SetCorner(BottomRightThumbGhost, Corner.BottomRight);

            SetCorner(TopLeftThumb, Corner.TopLeft);
            SetCorner(TopRightThumb, Corner.TopRight);
            SetCorner(BottomLeftThumb, Corner.BottomLeft);
            SetCorner(BottomRightThumb, Corner.BottomRight);
        }

        public event EventHandler<ResizerEventArgs> Resizing;

        public static Corner GetCorner(DependencyObject obj)
        {
            return (Corner)obj.GetValue(CornerProperty);
        }

        public static void SetCorner(DependencyObject obj, Corner value)
        {
            obj.SetValue(CornerProperty, value);
        }

        // Using a DependencyProperty as the backing store for Corner.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CornerProperty =
            DependencyProperty.RegisterAttached("Corner", typeof(Corner), typeof(Resizer), new PropertyMetadata(Corner.None));

        void Resizer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size s = e.NewSize;

            Canvas.SetLeft(TopLeftThumb, -TopLeftThumb.Width / 2);
            Canvas.SetTop(TopLeftThumb, -TopLeftThumb.Height / 2);
            Canvas.SetLeft(TopLeftThumbGhost, -TopLeftThumbGhost.Width / 2);
            Canvas.SetTop(TopLeftThumbGhost, -TopLeftThumbGhost.Height / 2);

            Canvas.SetLeft(TopRightThumb, s.Width - TopRightThumb.Width / 2);
            Canvas.SetTop(TopRightThumb, -TopRightThumb.Height / 2);
            Canvas.SetLeft(TopRightThumbGhost, s.Width - TopRightThumbGhost.Width / 2);
            Canvas.SetTop(TopRightThumbGhost, -TopRightThumbGhost.Height / 2);


            Canvas.SetLeft(BottomLeftThumb, -BottomLeftThumb.Width / 2);
            Canvas.SetTop(BottomLeftThumb, s.Height - BottomLeftThumb.Height / 2);
            Canvas.SetLeft(BottomLeftThumbGhost, -BottomLeftThumbGhost.Width / 2);
            Canvas.SetTop(BottomLeftThumbGhost, s.Height - BottomLeftThumbGhost.Height / 2);

            Canvas.SetLeft(BottomRightThumb, s.Width - BottomRightThumb.Width / 2);
            Canvas.SetTop(BottomRightThumb, s.Height - BottomRightThumb.Height / 2);
            Canvas.SetLeft(BottomRightThumbGhost, s.Width - BottomRightThumbGhost.Width / 2);
            Canvas.SetTop(BottomRightThumbGhost, s.Height - BottomRightThumbGhost.Height / 2);


        }

        Corner dragging;
        Point mouseDownPosition;
        Ellipse selectedThumb;

        public void OnThumbPressed(object sender, PointerRoutedEventArgs e)
        {
            if (selectedThumb == null)
            {
                selectedThumb = sender as Ellipse;
                if (!selectedThumb.Name.EndsWith("Ghost"))
                {
                    selectedThumb.Fill = this.FindResource<Brush>("SelectedThumbBrush");
                }
                // start dragging on first pointer press.
                DependencyObject thumb = sender as DependencyObject;
                dragging = GetCorner(thumb);
                Canvas parent = this.Parent as Canvas;
                if (parent == null)
                {
                    throw new Exception("Resizer requires parent to be a Canvas");
                }
                mouseDownPosition = e.GetCurrentPoint(parent).Position;
                this.initialBounds = new Rect(Canvas.GetLeft(this), Canvas.GetTop(this), this.ActualWidth, this.ActualHeight);

                e.Handled = true;
            }
        }

        public void OnThumbMoved(object sender, PointerRoutedEventArgs e)
        {
            if (selectedThumb != null)
            {
                e.Handled = true;

                UIElement parent = this.Parent as UIElement;
                Point pos = e.GetCurrentPoint(parent).Position;
                double dx = pos.X - mouseDownPosition.X;
                double dy = pos.Y - mouseDownPosition.Y;

                Rect newBounds = this.initialBounds;
                switch (dragging)
                {
                    case Corner.TopLeft:
                        newBounds.X = this.initialBounds.Left + dx;
                        newBounds.Y = this.initialBounds.Top + dy;
                        newBounds.Width = Math.Max(0, newBounds.Width - dx);
                        newBounds.Height = Math.Max(0, newBounds.Height - dy);                        
                        break;
                    case Corner.TopRight:
                        newBounds.Width = Math.Max(0, newBounds.Width + dx);
                        newBounds.Height = Math.Max(0, newBounds.Height - dy);
                        newBounds.Y = this.initialBounds.Top + dy;
                        break;
                    case Corner.BottomLeft:
                        newBounds.X = this.initialBounds.Left + dx;
                        newBounds.Height = Math.Max(0, newBounds.Height + dy);
                        newBounds.Width = Math.Max(0, newBounds.Width - dx);
                        break;
                    case Corner.BottomRight:
                        newBounds.Width = Math.Max(0, newBounds.Width + dx);
                        newBounds.Height = Math.Max(0, newBounds.Height + dy);
                        break;                
                }

                if (Resizing != null)
                {
                    Resizing(this, new ResizerEventArgs() { NewBounds = newBounds });
                }
            }
        }

        public void OnThumbReleased(object sender, PointerRoutedEventArgs e)
        {
            if (selectedThumb != null)
            {
                if (!selectedThumb.Name.EndsWith("Ghost"))
                {
                    selectedThumb.Fill = this.FindResource<Brush>("NormalThumbBrush");
                }
                selectedThumb = null;
                e.Handled = true;
            }
        }

    }
}
