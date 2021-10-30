using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Sample.Controls
{
    /// <summary>
    /// This class encapsulates the MouseHover gesture.
    /// </summary>
    public class HoverGesture
    {
        /// <summary>
        /// The target UI element we are implementing the Hover Gesture for.
        /// </summary>
        FrameworkElement _target;

        /// <summary>
        /// The delay in milliseconds before Hover event is fired
        /// </summary>
        private const int hoverDelay = 200; 

        /// <summary>
        /// A timer for Hover delay.
        /// </summary>
        private DispatcherTimer _hoverTimer;

        /// <summary>
        /// The mouse position where hovering is occuring.
        /// </summary>
        private Point _hoverPosition;

        /// <summary>
        /// Whether mouse is outside the target
        /// </summary>
        private bool _mouseOutside;

        // <summary>
        // Remember when hover event is raised so we don't do duplicates
        // </summary>
        private bool _hoverJustRaised;

        /// <summary>
        /// The event that is raised when hovering is detected.
        /// </summary>
        public event EventHandler Hover;


        /// <summary>
        /// The event that is raised when hovering is cancelled by the user
        /// </summary>
        public event EventHandler HoverCancelled;

        /// <summary>
        /// Construct a new Hover gesture object
        /// </summary>
        /// <param name="target">The target object we are detecting hovering over</param>
        public HoverGesture(FrameworkElement target)
        {
            _target = target;
            target.MouseDown += new MouseButtonEventHandler(target_MouseDown);
            target.MouseMove += new MouseEventHandler(target_MouseMove);
            target.MouseEnter += new MouseEventHandler(target_MouseEnter);
            target.MouseLeave += new MouseEventHandler(target_MouseLeave);
            target.KeyDown += new KeyEventHandler(target_KeyDown);
        }

        /// <summary>
        /// Callback for key down event
        /// </summary>
        /// <param name="sender">Target</param>
        /// <param name="e">Key events</param>
        void target_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel();
            }
        }

        /// <summary>
        /// Callback for MouseLeave event
        /// </summary>
        /// <param name="sender">Target</param>
        /// <param name="e">Mouse events</param>
        void target_MouseLeave(object sender, MouseEventArgs e)
        {
            _mouseOutside = true;
            Cancel();
        }

        /// <summary>
        /// Callback for MouseEnter event
        /// </summary>
        /// <param name="sender">Target</param>
        /// <param name="e">Mouse events</param>
        void target_MouseEnter(object sender, MouseEventArgs e)
        {
            _mouseOutside = false;
        }

        /// <summary>
        /// Callback for MouseMove event
        /// </summary>
        /// <param name="sender">Target</param>
        /// <param name="e">Mouse events</param>
        void target_MouseMove(object sender, MouseEventArgs e)
        {
            CheckHover(e);
        }

        /// <summary>
        /// Callback for MouseDown event
        /// </summary>
        /// <param name="sender">Target</param>
        /// <param name="e">Mouse events</param>
        void target_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Cancel();
        }

        /// <summary>
        /// Handler for mouse move events for the graph canvas
        /// </summary>
        /// <param name="sender">Canvas control</param>
        /// <param name="args">Mouse move arguments</param>
        private void CheckHover(MouseEventArgs args)
        {
            if (_hoverJustRaised) // swallow the bogus mouse event that happens right after we open a popup.
            {
                _hoverJustRaised = false;
                return;
            }

            if (_mouseOutside)
            {
                return;
            }

            // Get mouse position
            _hoverPosition = args.GetPosition(_target);
            Point check = Mouse.GetPosition(_target);

            Cancel(); 
            if (_hoverTimer == null)
            {
                _hoverTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(hoverDelay),
                    DispatcherPriority.Background, OnMouseTick, _target.Dispatcher);
            }
            // Restart the timer on every mouse move so that we only get a hover event after the mouse stops moving.
            _hoverTimer.Stop();
            _hoverTimer.Start();
        }

        private void OnMouseTick(Object sender, EventArgs args)
        {
            _hoverTimer.Stop();
            if (Hover != null)
            {
                _hoverJustRaised = true;
                Hover(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raise the HoverCancelled event.
        /// </summary>
        void Cancel()
        {
            if (_hoverTimer != null)
            {
                _hoverTimer.Stop();
            }
            if (HoverCancelled != null)
            {
                HoverCancelled(this, EventArgs.Empty);
            }
        }
    }
}
