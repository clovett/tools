using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace FoscamExplorer
{
    class ImageManipulationGesture
    {
        FrameworkElement element;
        Point? downPos;

        public void Start(FrameworkElement e)
        {
            element = e;
            e.PointerCaptureLost += OnPointerCaptureLost;
            e.PointerPressed += OnPointerPressed;
            e.PointerReleased += OnPointerReleased;
            e.PointerMoved += OnPointerMoved;
            e.PointerWheelChanged += OnPointerWheelChanged;
        }

        void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(this.element);
            var wheel = point.Properties.MouseWheelDelta;
            if (wheel < 0)
            {
                OnZoomChanged(-1);
            }
            else
            {
                OnZoomChanged(1);
            }
        }

        void OnZoomChanged(double zoomDirection)
        {
            if (ZoomDirectionChanged != null)
            {
                ZoomDirectionChanged(this, zoomDirection);
            }
        }

        public event EventHandler<double> ZoomDirectionChanged;
        public event EventHandler<Vector> DragVectorChanged;

        void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // todo: add pinch gesture support.
            downPos = e.GetCurrentPoint(this.element).Position;
            element.CapturePointer(e.Pointer);
        }

        public Point MouseDownPosition { get { return this.downPos.HasValue ? this.downPos.Value : new Point(0, 0); } }

        void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (downPos.HasValue)
            {
                var movePos = e.GetCurrentPoint(this.element).Position;
                OnDragVectorChanged(new Vector(downPos.Value, movePos));
            }
        }

        private void OnDragVectorChanged(Vector v)
        {
            if (DragVectorChanged != null)
            {
                DragVectorChanged(this, v);
            }
        }

        void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            downPos = null;
            OnDragVectorChanged(new Vector(0, 0));
            element.ReleasePointerCapture(e.Pointer);
        }

        void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            downPos = null;
            OnDragVectorChanged(new Vector(0, 0));
        }
    }
}
