using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

namespace Microsoft.Sample.Controls
{

    /// <summary>
    /// This class provides the ability to pan the target object when dragging the mouse 
    /// </summary>
    public class Pan
    {
        public const double PanThreshold = 5; // amount of drag before panning really kicks in (this allows for sloppy mouse clicks).
        bool _down;
        bool _dragging;
        FrameworkElement _target;
        MapZoom _zoom;
        bool _captured;
        Panel _container;
        Point _mouseDownPoint;
        Point _startTranslate;
        ModifierKeys _mods;

        /// <summary>
        /// Construct new Pan gesture object.
        /// </summary>
        /// <param name="target">The target to be panned, must live inside a container Panel</param>
        /// <param name="zoom">The zoom object that does the actual panning</param>
        /// <param name="modifiers">The modifier key to use</param>
        public Pan(FrameworkElement target, MapZoom zoom, ModifierKeys modifiers)
        {
            this._mods = modifiers;
            this._target = target;
            this._container = target.Parent as Panel;
            if (this._container == null)
            {
                // todo: localization
                throw new Exception("Target object must live in a Panel");
            }
            this._zoom = zoom;
            _container.MouseLeftButtonDown += new MouseButtonEventHandler(OnMouseLeftButtonDown);
            _container.MouseLeftButtonUp += new MouseButtonEventHandler(OnMouseLeftButtonUp);
            _container.MouseMove += new MouseEventHandler(OnMouseMove);
            _container.LostMouseCapture += new MouseEventHandler(OnLostMouseCapture);
        }

        /// <summary>
        /// Handle mouse left button event on container by recording that position and setting
        /// a flag that we've received mouse left down.
        /// </summary>
        /// <param name="sender">Container</param>
        /// <param name="e">Mouse information</param>
        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (!e.Handled && (Keyboard.Modifiers & _mods) == _mods &&
                (_mods != ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.None))
            {
                _down = true;
                _mouseDownPoint = e.GetPosition(this._container);
                Point offset = _zoom.Offset;
                _startTranslate = new Point(offset.X, offset.Y);
                // Have to capture the mouse on mouse down to ensure we get the mouse up!
                _captured = true;
                Mouse.Capture(this._container, CaptureMode.SubTree);
            }
        }

        /// <summary>
        /// Handle the mouse move event and this is where we capture the mouse.  We don't want
        /// to actually start panning on mouse down.  We want to be sure the user starts dragging
        /// first.
        /// </summary>
        /// <param name="sender">Mouse</param>
        /// <param name="e">Move information</param>
        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (this._down && !e.Handled && _captured)
            {
                Vector v = e.GetPosition(this._container) - _mouseDownPoint;
                if (v.Length > PanThreshold) 
                {                
                    _dragging = true; 
                    _target.Cursor = Cursors.Hand;
                }
            }
            if (_dragging)
            {
                this.MoveBy(_mouseDownPoint - e.GetPosition(this._container));
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle the mouse left button up event and stop any panning.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragging)
            {
                e.Handled = true;
            }
            StopPanning();
        }

        /// <summary>
        /// Stop the panning behavior.
        /// </summary>
        void StopPanning()
        {
            if (_captured)
            {
                Mouse.Capture(this._container, CaptureMode.None);
                _target.Cursor = Cursors.Arrow;
                _captured = false;
            }
            _dragging = false;
            _down = false;
        }

        /// <summary>
        /// Move the target object by the given delta delative to the start scroll position we recorded in mouse down event.
        /// </summary>
        /// <param name="vector">A vector containing the delta from recorded mouse down position and current mouse position</param>
        public void MoveBy(Vector vector)
        {
            _zoom.Offset = new Point(_startTranslate.X - vector.X, _startTranslate.Y - vector.Y);
            _target.InvalidateVisual();
        }

        /// <summary>
        /// Called if we lose the mouse capture.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            StopPanning();
        }
    }
}