using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.SvgEditorPackage
{
    internal static class Win32
    {
        #region Win32

        /// <summary>
        /// http://msdn2.microsoft.com/en-us/library/ms536119(VS.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// http://msdn2.microsoft.com/en-us/library/ms648390.aspx
        /// </summary>
        /// <param name="lpPoint"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        internal static System.Windows.Point GetMousePosition()
        {
            POINT p;
            if (!GetCursorPos(out p))
            {
                return new System.Windows.Point(0, 0);
            }

            // Convert pixels to device independent WPF coordinates
            return new System.Windows.Point(ConvertPixelsToDeviceIndependentPixels(p.X), ConvertPixelsToDeviceIndependentPixels(p.Y));
        }

        [DllImport("User32.dll")]
        private static extern IntPtr GetDC(HandleRef hWnd);

        [DllImport("User32.dll")]
        private static extern int ReleaseDC(HandleRef hWnd, HandleRef hDC);

        [DllImport("GDI32.dll")]
        private static extern int GetDeviceCaps(HandleRef hDC, int nIndex);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806")]
        private static int DPI
        {
            get
            {
                HandleRef desktopHwnd = new HandleRef(null, IntPtr.Zero);
                HandleRef desktopDC = new HandleRef(null, GetDC(desktopHwnd));
                try
                {
                    return GetDeviceCaps(desktopDC, 88 /*LOGPIXELSX*/);
                }
                finally
                {
                    ReleaseDC(desktopHwnd, desktopDC);
                }
            }
        }

        private static double ConvertPixelsToDeviceIndependentPixels(int pixels)
        {
            return (double)pixels * 96 / DPI;
        }

        #endregion

    }
}
