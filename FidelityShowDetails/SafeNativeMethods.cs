using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace FidelityShowDetails
{
    /// <devdoc>
    /// See VsLauncher.cs
    /// </devdoc>
    internal static class SafeNativeMethods
    {

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(String sClassName, String sAppName);

        /// <summary>
        /// Get the foreground window
        /// </summary>
        /// <returns></returns>
        [DllImport("User32")]
        internal static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Get the foreground window
        /// </summary>
        /// <returns></returns>
        [DllImport("User32")]
        internal static extern bool SetForegroundWindow(IntPtr hwnd);

        /// <summary>
        /// Gets the top window
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        [DllImport("User32")]
        internal static extern IntPtr GetTopWindow(IntPtr hwnd);

        [DllImport("User32")]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Gets the window text length
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern int GetWindowTextLength(IntPtr hwnd);

        /// <summary>
        /// Gets the window text natively
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpString"></param>
        /// <param name="nMaxCount"></param>
        /// <returns></returns>
        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hWnd, IntPtr lpString, int nMaxCount);

        /// <summary>
        /// Gets the window text in a managed friendly way
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        internal static string GetWindowText(IntPtr hwnd)
        {
            int len = GetWindowTextLength(hwnd);
            if (len <= 0) return "";
            len++; // include space for the null terminator.
            IntPtr buffer = Marshal.AllocCoTaskMem(len * 2);
            GetWindowText(hwnd, buffer, len);
            string s = Marshal.PtrToStringUni(buffer, len - 1);
            Marshal.FreeCoTaskMem(buffer);
            return s;
        }

        /// <summary>
        /// Get the process and thread id of the given window.
        /// </summary>
        /// <param name="hwnd">The window handle to query</param>
        /// <param name="procId">The process that created the window</param>
        /// <returns>The thread id for the window</returns>
        [DllImport("User32", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowThreadProcessId(IntPtr hwnd, out int procId);

        /// <summary>
        /// The WindowFromPoint function retrieves a handle to the window that contains the specified point. 
        /// </summary>
        /// <param name="Point">Location of window</param>
        /// <returns>The found window handle or IntPtr.Zero if not found</returns>
        [DllImport("user32.dll")]
        internal static extern IntPtr WindowFromPoint(POINT Point);

        /// <summary>
        /// The POINT structure defines the x- and y- coordinates of a point. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        static extern int GetWindowRect(IntPtr hwnd, ref RECT bounds);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Return the bounds of the given window
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        internal static Rect GetWindowRect(IntPtr hwnd)
        {
            RECT bounds = new RECT();
            GetWindowRect(hwnd, ref bounds);
            return new Rect(bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
        }

        internal delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lParam);

        internal static IEnumerable<IntPtr> GetDesktopWindows()
        {
            IList<IntPtr> result = new List<IntPtr>();
            WindowEnumProc callback = new WindowEnumProc((hwnd, lparam) => { result.Add(hwnd); return true; });
            EnumWindows(callback, IntPtr.Zero);
            return result;
        }

        [DllImport("user32.dll")]
        static extern bool EnumWindows(WindowEnumProc lpEnumFunc, IntPtr lParam);


        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateDC(
        string lpszDriver,
        string lpszDevice,
        string lpszOutput,
        IntPtr lpInitData
        );

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(
        IntPtr hdcDest,
        int nXDest,
        int nYDest,
        int nWidth,
        int nHeight,
        IntPtr hdcSrc,
        int nXSrc,
        int nYSrc,
        int dwrop);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(
        IntPtr hdc // handle to DC 
        );

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(
        IntPtr hdc, // handle to DC 
        int nWidth, // width of bitmap, in pixels 
        int nHeight // height of bitmap, in pixels 
        );

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(
        IntPtr hdc, // handle to DC 
        IntPtr hgdiobj // handle to object 
        );

        [DllImport("gdi32.dll")]
        public static extern int DeleteDC(
        IntPtr hdc // handle to DC 
        );

        public static readonly int SRCCOPY = 13369376;
    }

}
