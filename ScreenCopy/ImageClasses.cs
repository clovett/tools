using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class User32dll
{
	[DllImport("User32.dll")]
	public static extern int GetDesktopWindow();
	[DllImport("User32.dll")]
	public static extern int GetWindowDC(int hWnd);
	[DllImport("User32.dll")]
	public static extern int ReleaseDC(int hWnd,int hDC);
}

class GDI32dll
{
	[DllImport("GDI32.dll")]
	public static extern bool BitBlt(int hdcDest,int nXDest,int nYDest, int nWidth,int nHeight,int hdcSrc, int nXSrc,int nYSrc,int dwRop);
	[DllImport("GDI32.dll")]
	public static extern int CreateCompatibleBitmap(int hdc,int width, int height);
	[DllImport("GDI32.dll")]
	public static extern int CreateCompatibleDC(int hdc);
	[DllImport("GDI32.dll")]
	public static extern bool DeleteDC(int hdc);
	[DllImport("GDI32.dll")]
	public static extern bool DeleteObject(int hObject);
	//	[DllImport("GDI32.dll")]
	//	public static extern int GetDeviceCaps(int hdc,int nIndex);
	[DllImport("GDI32.dll")]
	public static extern int SelectObject(int hdc,int hgdiobj);
}