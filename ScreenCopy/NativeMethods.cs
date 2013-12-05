using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

public static class NativeMethods
{ 
	[DllImport("User32.dll")]
    public static extern IntPtr GetDesktopWindow();

	[DllImport("User32.dll")]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("User32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("User32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

	[DllImport("GDI32.dll")]
    public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

	[DllImport("GDI32.dll")]
    public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);
	
    [DllImport("GDI32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

	[DllImport("GDI32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);

	[DllImport("GDI32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);
	
    [DllImport("GDI32.dll")]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("GDI32.dll")]
    public static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

    private static int _dpi = 0;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806")]
    private static int DPI
    {
        get
        {
            if (_dpi == 0)
            {
                IntPtr hwndDesktop = IntPtr.Zero;
                IntPtr desktopDC = GetDC(hwndDesktop);
                _dpi = GetDeviceCaps(desktopDC, 88 /*LOGPIXELSX*/);
                ReleaseDC(hwndDesktop, desktopDC);
            }
            return _dpi;
        }
    }

    internal static double ConvertToDeviceIndependentPixels(int pixels)
    {
        return (double)pixels * 96 / (double)DPI;
    }


    internal static int ConvertFromDeviceIndependentPixels(double pixels)
    {
        return (int)(pixels * (double)DPI / 96);
    }

    [DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
        SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall)]
    public static extern int ShellExecute(IntPtr handle, string verb, string file, string args, string dir, int show);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "rc")]
    public static void OpenUrl(IntPtr owner, Uri url)
    {
        // todo: support showing embedded pack:// resources in a popup page (could be useful for help content).
        const int SW_SHOWNORMAL = 1;
        int rc = ShellExecute(owner, "open", url.AbsoluteUri, null, System.IO.Directory.GetCurrentDirectory(), SW_SHOWNORMAL);
    }

}