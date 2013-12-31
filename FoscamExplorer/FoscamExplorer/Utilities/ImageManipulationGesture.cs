using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#if WINDOWS_PHONE
using System.Windows;
using System.Windows.Input;
#else
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
#endif

namespace FoscamExplorer
{
    class ImageManipulationGesture
    {
        FrameworkElement element;
        Point? downPos;

        public event EventHandler<double> ZoomDirectionChanged;
        public event EventHandler<Vector> DragVectorChanged;

        public Point MouseDownPosition { get { return this.downPos.HasValue ? this.downPos.Value : new Point(0, 0); } }

        public void Start(FrameworkElement e)
        {
            element = e;

#if WINDOWS_PHONE
            e.LostMouseCapture += OnLostMouseCapture;
            e.MouseWheel += OnMouseWheel;
            e.MouseLeftButtonDown += OnMouseLeftButtonDown;
            e.MouseMove += OnMouseMove;
            e.MouseLeftButtonUp += OnMouseLeftButtonUp;
#else
            e.PointerCaptureLost += OnPointerCaptureLost;
            e.PointerPressed += OnPointerPressed;
            e.PointerReleased += OnPointerReleased;
            e.PointerMoved += OnPointerMoved;
            e.PointerWheelChanged += OnPointerWheelChanged;
#endif
        }

#if WINDOWS_PHONE
        void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            downPos = null;
            OnDragVectorChanged(new Vector(0, 0));
        }

        void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int clicks = e.Delta;
            if (clicks < 0)
            {
                OnZoomChanged(-1);
            }
            else
            {
                OnZoomChanged(1);
            }
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // todo: add pinch gesture support.
            downPos = e.GetPosition(this.element);
            element.CaptureMouse();
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (downPos.HasValue)
            {
                var movePos = e.GetPosition(this.element);
                OnDragVectorChanged(new Vector(downPos.Value, movePos));
            }
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            downPos = null;
            OnDragVectorChanged(new Vector(0, 0));
            element.ReleaseMouseCapture();
        }

#else
        void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            downPos = null;
            OnDragVectorChanged(new Vector(0, 0));
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
        
        void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // todo: add pinch gesture support.
            downPos = e.GetCurrentPoint(this.element).Position;
            element.CapturePointer(e.Pointer);
        }
        
        void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (downPos.HasValue)
            {
                var movePos = e.GetCurrentPoint(this.element).Position;
                OnDragVectorChanged(new Vector(downPos.Value, movePos));
            }
        }
        void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            downPos = null;
            OnDragVectorChanged(new Vector(0, 0));
            element.ReleasePointerCapture(e.Pointer);
        }

#endif

        void OnZoomChanged(double zoomDirection)
        {
            if (ZoomDirectionChanged != null)
            {
                ZoomDirectionChanged(this, zoomDirection);
            }
        }


        private void OnDragVectorChanged(Vector v)
        {
            if (DragVectorChanged != null)
            {
                DragVectorChanged(this, v);
            }
        }

    }
}
