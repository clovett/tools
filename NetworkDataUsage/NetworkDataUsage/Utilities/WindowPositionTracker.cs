using NetgearDataUsage.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Walkabout.Utilities;

namespace NetworkDataUsage.Utilities
{
    class WindowPositionTracker
    {
        Window window;
        DelayedActions delayedActions = new DelayedActions();

        public WindowPositionTracker()
        {
        }

        public async void Open(Window w)
        {
            Close();
            window = w;
            RestorePosition();
            window.SizeChanged += OnWindowSizeChanged;
            window.LocationChanged += OnWindowLocationChanged;
        }

        public void Close()
        {
            if (this.window != null)
            {
                window.SizeChanged -= OnWindowSizeChanged;
                window.LocationChanged -= OnWindowLocationChanged;
                this.window = null;
            }
        }
        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            delayedActions.StartDelayedAction("SaveWindowLocation", SavePosition, TimeSpan.FromMilliseconds(1000));
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            delayedActions.StartDelayedAction("SaveWindowLocation", SavePosition, TimeSpan.FromMilliseconds(1000));
        }

        async void RestorePosition()
        {
            Settings settings = await ((App)App.Current).LoadSettings();
            if (settings.WindowSize.Width != 0 && settings.WindowSize.Height != 0)
            {
                Window w = this.window;
                w.Visibility = Visibility.Hidden;
                try
                {
                    var presentationSource = PresentationSource.FromVisual(this.window);
                    var transform = presentationSource.CompositionTarget.TransformToDevice;
                    Point topLeft = new Point(settings.WindowLocation.X, settings.WindowLocation.Y);
                    Point bottomRight = new Point(settings.WindowLocation.X + settings.WindowSize.Width, settings.WindowLocation.Y + settings.WindowSize.Height);
                    transform.Transform(new Point[] { topLeft, bottomRight });

                    // make sure it is visible on the user's current screen configuration.
                    var bounds = new System.Drawing.Rectangle((int)topLeft.X, (int)topLeft.Y,
                        (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));

                    var screen = System.Windows.Forms.Screen.FromRectangle(bounds);
                    bounds.Intersect(screen.WorkingArea);
                    if (bounds.Width == 0 || bounds.Height == 0)
                    {
                        // cannot restore this position, let the OS choose a new one.
                        return;
                    }

                    // transform back to device independent coordinates.
                    topLeft.X = bounds.X;
                    topLeft.Y = bounds.Y;
                    bottomRight.X = bounds.Right;
                    bottomRight.Y = bounds.Bottom;

                    transform = presentationSource.CompositionTarget.TransformFromDevice;
                    transform.Transform(new Point[] { topLeft, bottomRight });

                    this.window.Left = topLeft.X;
                    this.window.Top = topLeft.Y;
                    this.window.Width = bottomRight.X - topLeft.X;
                    this.window.Height = bottomRight.Y - topLeft.Y;
                }
                finally
                {
                }
                w.Visibility = Visibility.Visible;
            }
        }

        async void SavePosition()
        {
            if (this.window == null)
            {
                return;
            }
            var bounds = this.window.RestoreBounds;
            Settings settings = await ((App)App.Current).LoadSettings();
            bool changed = false;
            if (settings.WindowLocation != bounds.TopLeft)
            {
                settings.WindowLocation = bounds.TopLeft;
                changed = true;
            }
            if (settings.WindowSize != bounds.Size)
            {
                settings.WindowSize = bounds.Size;
                changed = true;
            }
            if (changed)
            {
                await settings.SaveAsync();
            }
        }

    }
}
