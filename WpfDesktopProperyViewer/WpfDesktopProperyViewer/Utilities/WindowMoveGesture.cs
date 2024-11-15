using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfDesktopProperyViewer.Utilities
{
    internal class WindowMoveGesture
    {
        public WindowMoveGesture(Window owner)
        {
            this.owner = owner;
            owner.MouseLeftButtonDown += OnMouseLeftButtonDown;
            owner.MouseLeftButtonUp += OnMouseUp;
            owner.MouseMove += OnMouseMove;
            owner.LostMouseCapture += OnLostMouseCapture;
        }

        Window owner;
        Point mouseDownPos;
        bool mouseDown;
        bool captured;
        Point startLocation;

        protected void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            startLocation = new Point(this.owner.Left, this.owner.Top);
            mouseDownPos = this.owner.PointToScreen(e.GetPosition(this.owner));
            captured = this.owner.CaptureMouse();
        }

        protected void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (this.mouseDown)
            {
                MoveWindowTo(this.owner.PointToScreen(e.GetPosition(this.owner)));
            }
        }

        private void MoveWindowTo(Point pt)
        {
            var x = pt.X - mouseDownPos.X + startLocation.X;
            var y = pt.Y - mouseDownPos.Y + startLocation.Y;
            this.owner.Left = x;
            this.owner.Top = y;
        }

        protected void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (captured)
            {
                this.owner.ReleaseMouseCapture();
                captured = false;
            }
            if (this.mouseDown)
            {
                this.mouseDown = false;
                MoveWindowTo(this.owner.PointToScreen(e.GetPosition(this.owner)));
            }
        }

        protected void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            captured = false;
            mouseDown = false; // ?
        }

    }
}
