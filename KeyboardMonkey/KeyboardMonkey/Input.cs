namespace Walkabout.Utilities
{
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Input;
    using System.Security.Permissions;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    
    using System;
    using HookManager;
    using System.Diagnostics;

    /// <summary>
    /// Flags for Input.SendMouseInput, indicate whether movement took place,
    /// or whether buttons were pressed or released.
    /// </summary>
    [Flags]
    public enum SendMouseInputFlags {
        /// <summary>Specifies that the pointer moved.</summary>
        Move       = 0x0001,
        /// <summary>Specifies that the left button was pressed.</summary>
        LeftDown   = 0x0002,
        /// <summary>Specifies that the left button was released.</summary>
        LeftUp     = 0x0004,
        /// <summary>Specifies that the right button was pressed.</summary>
        RightDown  = 0x0008,
        /// <summary>Specifies that the right button was released.</summary>
        RightUp    = 0x0010,
        /// <summary>Specifies that the middle button was pressed.</summary>
        MiddleDown = 0x0020,
        /// <summary>Specifies that the middle button was released.</summary>
        MiddleUp   = 0x0040,
        /// <summary>Specifies that the x button was pressed.</summary>
        XDown      = 0x0080,
        /// <summary>Specifies that the x button was released. </summary>
        XUp        = 0x0100,
        /// <summary>Specifies that the wheel was moved</summary>
        Wheel      = 0x0800,
        /// <summary>Specifies that x, y are absolute, not relative</summary>
        Absolute   = 0x8000,
    };


    /// <summary>
    /// Flags for Input.SendMouseInput, for indicating the intention of the mouse wheel rotation
    /// </summary>
    [Flags]
    public enum MouseWheel
    {
        /// <summary>Specifies that the mouse wheel is rotated forward, away from the user</summary>
        Forward_ZoomIn = 1,

        /// <summary>Specifies that the mouse wheel is rotated backward, towards the user</summary>
        Backward_ZoomOut = -1
    }

    /// <summary>
    /// Provides methods for sending mouse and keyboard input
    /// </summary>
    public static class Input
    {
        /// <summary>The first X mouse button</summary>
        public const int XButton1 = 0x01;

        /// <summary>The second X mouse button</summary>
        public const int XButton2 = 0x02;

        public static void SendMouseInput(int x, int y, int data, SendMouseInputFlags flags) {
            SendMouseInput((double)x, (double)y, data, flags);
        }

        /// <summary>
        /// Call this function between mouse clicks to ensure that it is not interpretted as a double click.
        /// </summary>
        public static void WaitDoubleClickTime()
        {
            int time = Win32.GetDoubleClickTime();
            System.Threading.Thread.Sleep(time * 2);
        }

        /// <summary>
        /// Inject pointer input into the system
        /// </summary>
        /// <param name="x">x coordinate of pointer, if Move flag specified</param>
        /// <param name="y">y coordinate of pointer, if Move flag specified</param>
        /// <param name="data">wheel movement, or mouse X button, depending on flags</param>
        /// <param name="flags">flags to indicate which type of input occurred - move, button press/release, wheel move, etc.</param>
        /// <remarks>x, y are in pixels. If Absolute flag used, are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void SendMouseInput(double x, double y, int data, SendMouseInputFlags flags)
        {
            SendMouseInput(x, y, data, flags, 0);
        }

        public static void SendMouseInput(double x, double y, int data, SendMouseInputFlags flags, int mouseData)
        {
            int intflags = (int) flags;

            if((intflags & (int)SendMouseInputFlags.Absolute) != 0) {
                int vscreenWidth = Win32.GetSystemMetrics(Win32.SM_CXVIRTUALSCREEN);
                int vscreenHeight = Win32.GetSystemMetrics(Win32.SM_CYVIRTUALSCREEN);
                int vscreenLeft = Win32.GetSystemMetrics(Win32.SM_XVIRTUALSCREEN);
                int vscreenTop = Win32.GetSystemMetrics(Win32.SM_YVIRTUALSCREEN);

                // Absolute input requires that input is in 'normalized' coords - with the entire
                // desktop being (0,0)...(65535,65536). Need to convert input x,y coords to this
                // first.
                //
                // In this normalized world, any pixel on the screen corresponds to a block of values
                // of normalized coords - eg. on a 1024x768 screen,
                // y pixel 0 corresponds to range 0 to 85.333,
                // y pixel 1 corresponds to range 85.333 to 170.666,
                // y pixel 2 correpsonds to range 170.666 to 256 - and so on.
                // Doing basic scaling math - (x-top)*65536/Width - gets us the start of the range.
                // However, because int math is used, this can end up being rounded into the wrong
                // pixel. For example, if we wanted pixel 1, we'd get 85.333, but that comes out as
                // 85 as an int, which falls into pixel 0's range - and that's where the pointer goes.
                // To avoid this, we add on half-a-"screen pixel"'s worth of normalized coords - to
                // push us into the middle of any given pixel's range - that's the 65536/(Width*2)
                // part of the formula. So now pixel 1 maps to 85+42 = 127 - which is comfortably
                // in the middle of that pixel's block.
                // The key ting here is that unlike points in coordinate geometry, pixels take up
                // space, so are often better treated like rectangles - and if you want to target
                // a particular pixel, target its rectangle's midpoint, not its edge.
                x = ((x - vscreenLeft) * 65536) / vscreenWidth + 65536 / (vscreenWidth * 2);
                y = ((y - vscreenTop) * 65536) / vscreenHeight + 65536 / (vscreenHeight * 2);

                intflags |= Win32.MOUSEEVENTF_VIRTUALDESK;
            }

            Win32.INPUT mi = new Win32.INPUT();
            mi.type = Win32.INPUT_MOUSE;
            mi.union.mouseInput.dx = (int) x;
            mi.union.mouseInput.dy = (int)y;
            mi.union.mouseInput.mouseData = data;
            mi.union.mouseInput.dwFlags = intflags;
            mi.union.mouseInput.time = 0;
            mi.union.mouseInput.dwExtraInfo = new IntPtr(0);
            mi.union.mouseInput.mouseData = mouseData;

            if(Win32.SendInput(1, ref mi, Marshal.SizeOf(mi)) == 0) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }


        /// <summary>
        /// Move the mouse to a point and click.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        /// </summary>
        /// <param name="pt">The point to click at</param>
        /// <remarks>pt are in pixels that are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void SendMouseScrollWheel(Point pt, MouseWheel mwRotation, System.Windows.Input.Key key)
        {
            Input.SendKeyboardInput(key, true);
            int mouseData = 0;

            if (Walkabout.Utilities.MouseWheel.Forward_ZoomIn == mwRotation)
            {
                mouseData = 120;
            }

            if (Walkabout.Utilities.MouseWheel.Backward_ZoomOut == mwRotation)
            {
                mouseData = -120;
            }
            
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.Wheel | SendMouseInputFlags.Absolute, mouseData);

            Input.SendKeyboardInput(key, false);
        }

        /// <summary>
        /// Taps the specified keyboard key
        /// </summary>
        /// <param name="key">key to tap</param>
        public static void TapKey(Key key)
        {
            SendKeyboardInput(key, true);
            SendKeyboardInput(key, false);
        }

        /// <summary>
        /// Taps the specified keyboard key
        /// </summary>
        /// <param name="key">key to tap</param>
        public static void TapKey(Key key, ModifierKeys mods)
        {
            if ((mods & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                SendKeyboardInput(Key.LeftAlt, true);
            }
            if ((mods & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SendKeyboardInput(Key.LeftCtrl, true);
            }
            if ((mods & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                SendKeyboardInput(Key.LeftShift, true);
            }
            if ((mods & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                SendKeyboardInput(Key.LWin, true);
            }
            SendKeyboardInput(key, true);
            SendKeyboardInput(key, false);

            if ((mods & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                SendKeyboardInput(Key.LeftAlt, false);
            }
            if ((mods & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SendKeyboardInput(Key.LeftCtrl, false);
            }
            if ((mods & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                SendKeyboardInput(Key.LeftShift, false);
            }
            if ((mods & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                SendKeyboardInput(Key.LWin, false);
            }
        }

        
        
        /// <summary>
        /// Taps the specified keyboard key
        /// </summary>
        /// <param name="key">key to tap</param>
        public static void TapKeySlow(Key key)
        {
            TapKey(key);
            System.Threading.Thread.Sleep(100);
        }


         /// <summary>
        /// Taps the specified keyboard key
        /// </summary>
        /// <param name="key">key to tap</param>
        public static void TapTabKeyBackward()
        {
            Input.SendKeyboardInput(Key.LeftShift, true);
            TapKeySlow(Key.Tab);
            Input.SendKeyboardInput(Key.LeftShift, false);
        }

        private static long MakeLong(ushort lo, ushort hi)
        {
            return (long)lo | (long)hi << 16;
        }


        /// <summary>
        /// Send WM_KEYDOWN or WM_KEYUP messages to the given window.
        /// </summary>
        /// <param name="hwnd">The window to send to</param>
        /// <param name="key">indicates the key pressed or released. Can be one of the constants defined in the Key enum</param>
        /// <param name="press">true to inject a key press, false to inject a key release</param>
        internal static void SendKeyboardMessage(IntPtr hwnd, Key key, bool pressed)
        {
            Debug.WriteLine("Sending key {0} with pressed={1}", key, pressed);
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            int scanCode = Win32.MapVirtualKey(virtualKey, 0);
            ushort flags = pressed ? (ushort)0 : (ushort)(Win32.KF_UP | Win32.KF_REPEAT);
            if (IsExtendedKey(key))
            {
                flags |= Win32.KF_EXTENDED;
            }
            ushort repeat = (ushort)1;
            long l = MakeLong(repeat, (ushort)(scanCode | flags));
            UIntPtr lparam = (UIntPtr)l;
            // todo: bit 30: The previous key state. The value is always 1 for a WM_KEYUP message.
            UIntPtr wparam = (UIntPtr)virtualKey;
            if (pressed)
            {
                Win32.SendMessage(Win32.HWND.Cast(hwnd), Win32.WM_KEYDOWN, wparam, lparam);
                // since the window likely doesn't have the focus, we also need to send WM_CHAR.
                Win32.MSG msg = new Win32.MSG()
                {
                    hwnd = hwnd,
                    lParam = (ulong)lparam,
                    wParam = (ulong)wparam,
                    time = (uint)Environment.TickCount,
                    message = Win32.WM_KEYDOWN
                };

                if (!Win32.TranslateMessage(ref msg))
                {
                    Debug.WriteLine("no translate");
                }
            }
            else
            {
                Win32.SendMessage(Win32.HWND.Cast(hwnd), Win32.WM_KEYUP, wparam, lparam);
            }
        }

        /// <summary>
        /// Inject keyboard input into the system
        /// </summary>
        /// <param name="key">indicates the key pressed or released. Can be one of the constants defined in the Key enum</param>
        /// <param name="press">true to inject a key press, false to inject a key release</param>
        public static void SendKeyboardInput(Key key, bool press) {
            Win32.INPUT ki = new Win32.INPUT();
            ki.type = Win32.INPUT_KEYBOARD;
            ki.union.keyboardInput.wVk = (short) KeyInterop.VirtualKeyFromKey(key);
            ki.union.keyboardInput.wScan = (short) Win32.MapVirtualKey(ki.union.keyboardInput.wVk, 0);
            
            int dwFlags = 0;
            if(ki.union.keyboardInput.wScan > 0) {
                dwFlags |= Win32.KEYEVENTF_SCANCODE;
            }
            if(false == press) {
                dwFlags |= Win32.KEYEVENTF_KEYUP;
            }
            
            ki.union.keyboardInput.dwFlags = dwFlags;
            if(IsExtendedKey(key)) {
                ki.union.keyboardInput.dwFlags |= Win32.KEYEVENTF_EXTENDEDKEY;
            }

            ki.union.keyboardInput.time = Environment.TickCount;
            ki.union.keyboardInput.dwExtraInfo = new IntPtr(0);
            if(0 == Win32.SendInput(1, ref ki, Marshal.SizeOf(ki))) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Injects a unicode character as keyboard input into the system
        /// </summary>
        /// <param name="key">indicates the key to be pressed or released. Can be any unicode character</param>
        /// <param name="press">true to inject a key press, false to inject a key release</param>
        public static void SendUnicodeKeyboardInput(char key, bool press) {
            Win32.INPUT ki = new Win32.INPUT();

            ki.type = Win32.INPUT_KEYBOARD;
            ki.union.keyboardInput.wVk = (short)0;
            ki.union.keyboardInput.wScan = (short)key;
            ki.union.keyboardInput.dwFlags = Win32.KEYEVENTF_UNICODE | (press ? 0 : Win32.KEYEVENTF_KEYUP);
            ki.union.keyboardInput.time = Environment.TickCount;
            ki.union.keyboardInput.dwExtraInfo = new IntPtr(0);
            if (0 == Win32.SendInput(1, ref ki, Marshal.SizeOf(ki))) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Injects a string of Unicode characters using simulated keyboard input
        /// It should be noted that this overload just sends the whole string
        /// with no pauses, depending on the recieving applications input processing
        /// it may not be able to keep up with the speed, resulting in corruption or
        /// loss of the input data.
        /// </summary>
        /// <param name="data">The unicode string to be sent</param>
        public static void SendUnicodeString(string data) {
            InternalSendUnicodeString(data, 50);
        }

        /// <summary>
        /// Injects a string of Unicode characters using simulated keyboard input
        /// with user defined timing.
        /// </summary>
        /// <param name="data">The unicode string to be sent</param>
        /// <param name="sleepLength">How long, in milliseconds, to sleep between each character</param>
        public static void SendUnicodeString(string data, int sleepLength) {
            if(sleepLength < 0) {
                throw new ArgumentOutOfRangeException("sleepLength");
            }
            
            InternalSendUnicodeString(data, sleepLength);
        }

        /// <summary>
        /// Checks whether the specified key is currently up or down
        /// </summary>
        /// <param name="key">The Key to check</param>
        /// <returns>true if the specified key is currently down (being pressed), false if it is up</returns>
        public static bool GetAsyncKeyState(Key key) {
            int vKey = KeyInterop.VirtualKeyFromKey(key);
            int resp = Win32.GetAsyncKeyState(vKey);

            if(resp == 0) {
                throw new InvalidOperationException("GetAsyncKeyStateFailed");
            }

            return resp < 0;
        }

        /// <summary>
        /// Move the mouse with a the left button down.
        /// </summary>
        /// <param name="p"></param>
        public static void DragTo(Point p1, Point p2)
        {
            Input.SendMouseInput(p1.X, p1.Y, 0, SendMouseInputFlags.LeftDown | SendMouseInputFlags.Absolute);

            System.Threading.Thread.Sleep(200);

            Input.SlideTo(p2);
            Input.SendMouseInput(p2.X, p2.Y, 0, SendMouseInputFlags.LeftUp | SendMouseInputFlags.Absolute);            
        }

        /// <summary>
        /// Move the mouse with a the right button down.
        /// </summary>
        /// <param name="p"></param>
        public static void DragRightTo(Point p1, Point p2)
        {
            Input.SendMouseInput(p1.X, p1.Y, 0, SendMouseInputFlags.RightDown | SendMouseInputFlags.Absolute);

            System.Threading.Thread.Sleep(200);

            Input.SlideTo(p2);
            Input.SendMouseInput(p2.X, p2.Y, 0, SendMouseInputFlags.RightUp | SendMouseInputFlags.Absolute);
        }

        /// <summary>
        /// Move the mouse to an element. 
        ///
        /// IMPORTANT!
        /// 
        /// Do not call MoveToAndClick (actually, do not make any calls to UIAutomationClient) 
        /// from the UI thread if your test is in the same process as the UI being tested.  
        /// UIAutomation calls back into Avalon core for UI information (e.g. ClickablePoint) 
        /// and must be on the UI thread to get this information.  If your test is making calls 
        /// from the UI thread you are going to deadlock...
        /// 
        /// </summary>
        /// <param name="el">The element that the mouse will move to</param>
        /// <exception cref="NoClickablePointException">If there is not clickable point for the element</exception>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveTo(AutomationElement el) {
            if (el == null) {
                throw new ArgumentNullException("el");
            }
            MoveTo(GetClickablePoint(el));
        }

        /// <summary>
        /// Slide the mouse to an element. 
        /// </summary>
        /// <param name="el">The element that the mouse will move to</param>
        /// <exception cref="NoClickablePointException">If there is not clickable point for the element</exception>
        public static void SlideTo(AutomationElement el)
        {
            if (el == null) {
                throw new ArgumentNullException("el");
            }
            SlideTo(GetClickablePoint(el));
        }

        /// <summary>
        /// Move the mouse to a point. 
        /// </summary>
        /// <param name="pt">The point that the mouse will move to.</param>
        /// <remarks>pt are in pixels that are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveTo(Point pt) {
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);
        }

        /// <summary>
        /// Get the current mouse position.
        /// </summary>
        /// <returns></returns>
        public static Point GetMousePosition()
        {
            Win32.POINT cursorPosition = new Win32.POINT();
            if (Win32.GetCursorPos(out cursorPosition))
            {
                // chart a smooth course from cursor pos to the specified position.
                return new Point(cursorPosition.X, cursorPosition.Y);
            }
            throw new Exception("Win32.GetCursorPos failed");
        }

        /// <summary>
        /// Slide the mouse to a point, simulating a real mouse move with all the points inbetween.
        /// </summary>
        /// <param name="pt">The point that the mouse will move to.</param>
        /// <remarks>pt are in pixels that are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void SlideTo(Point pt)
        {
            Win32.POINT cursorPosition = new Win32.POINT();
            if (Win32.GetCursorPos(out cursorPosition))
            {
                // chart a smooth course from cursor pos to the specified position.
                Point start = new Point(cursorPosition.X, cursorPosition.Y);
                Vector v = pt - start;
                int increment = 5;
                int steps = (int)(v.Length / increment);
                v.Normalize();
                for (int i = 0; i < steps; i++)
                {
                    Vector v2 = Vector.Multiply(i * increment, v);
                    Point p = Point.Add(start, v2);
                    Input.SendMouseInput(p.X, p.Y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);
                    System.Threading.Thread.Sleep(10);
                }
            }
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);
        }

        /// <summary>
        /// Move the mouse to an element and click on it.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        ///
        /// IMPORTANT!
        /// 
        /// Do not call MoveToAndClick (actually, do not make any calls to UIAutomationClient) 
        /// from the UI thread if your test is in the same process as the UI being tested.  
        /// UIAutomation calls back into Avalon core for UI information (e.g. ClickablePoint) 
        /// and must be on the UI thread to get this information.  If your test is making calls 
        /// from the UI thread you are going to deadlock...
        /// 
        /// </summary>
        /// <param name="el">The element to click on</param>
        /// <exception cref="NoClickablePointException">If there is not clickable point for the element</exception>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveToAndClick(AutomationElement el) {
            if (el == null) {
                throw new ArgumentNullException("el");
            }
            MoveToAndLeftClick(GetClickablePoint(el));
        }


        /// <summary>
        /// Move to and click in the left side of the element
        /// </summary>
        /// <param name="el">The element to click on</param>
        internal static void MoveToAndClickLeftSide(AutomationElement el)
        {
            if (el == null)
            {
                throw new ArgumentNullException("el");
            }
            Point p = GetClickablePoint(el);
            Rect bounds = el.Current.BoundingRectangle;
            p.X = bounds.Left + (bounds.Width / 10);
            MoveToAndLeftClick(p);
        }

        /// <summary>
        /// Move the mouse to a point and click.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        /// </summary>
        /// <param name="pt">The point to click at</param>
        /// <remarks>pt are in pixels that are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveToAndLeftClick(Point pt) {
            MoveTo(pt);

            // send SendMouseInput works in term of the phisical mouse buttons, therefore we need
            // to check to see if the mouse buttons are swapped because this method need to use the primary
            // mouse button.
            if (0 == Win32.GetSystemMetrics(Win32.SM_SWAPBUTTON)) {
                // the mouse buttons are not swaped the primary is the left
                LeftClick(pt);
            }
            else {
                // the mouse buttons are swaped so click the right button which as actually the primary
                RightClick(pt);
            }
        }

        /// <summary>
        /// Click middle button.
        /// </summary>
        /// <param name="pt">Place to click</param>
        public static void MiddleClick(Point pt)
        {
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.MiddleDown | SendMouseInputFlags.Absolute);
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.MiddleUp | SendMouseInputFlags.Absolute);
        }

        /// <summary>
        /// Click right button
        /// </summary>
        /// <param name="pt">Place to click</param>
        public static void RightClick(Point pt)
        {
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.RightDown | SendMouseInputFlags.Absolute);
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.RightUp | SendMouseInputFlags.Absolute);
        }

        /// <summary>
        /// Click left button.
        /// </summary>
        /// <param name="pt">Place to click</param>
        public static void LeftClick(Point pt)
        {
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.LeftDown | SendMouseInputFlags.Absolute);
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.LeftUp | SendMouseInputFlags.Absolute);
        }

       
        internal static Point GetClickablePoint(AutomationElement el) {
            int retries = 10;
            while (retries-- > 0)
            {
                try
                {
                    Point pt = el.GetClickablePoint();
                    return pt;
                }
                catch (Exception)
                {
                    // UI is blocked doing something and can't respond right now.
                    System.Threading.Thread.Sleep(1000);
                }
            }
            throw new Exception("Not finding a clickable point!");
        }

        /// <summary>
        /// Move the mouse to a point and right clicks.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        /// </summary>
        /// <param name="pt">The point to click at</param>
        /// <remarks>pt are in pixels that are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveToAndRightClick(Point pt)
        {
            MoveTo(pt);

            // send SendMouseInput works in term of the phisical mouse buttons, therefore we need
            // to check to see if the mouse buttons are swapped because this method need to use the primary
            // mouse button.
            if (0 == Win32.GetSystemMetrics(Win32.SM_SWAPBUTTON))
            {
                RightClick(pt);
            }
            else
            {
                // the mouse buttons are swapped so the right click is actually the left button.
                LeftClick(pt);
            }
        }

        /// <summary>
        /// Move the mouse to automation element and click.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        /// </summary>
        /// <param name="pt">The automation element to double click</param>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveToAndDoubleClick(AutomationElement el)
        {
            if (el == null)
            {
                throw new ArgumentNullException("el");
            }
            MoveToAndDoubleClick(GetClickablePoint(el));
        }

        /// <summary>
        /// Move the mouse to a point and click.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        /// </summary>
        /// <param name="pt">The point to click at</param>
        /// <remarks>pt are in pixels that are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveToAndDoubleClick(Point pt)
        {
            MoveTo(pt);
            DoubleLeftClick(pt);
        }

        public static void DoubleLeftClick(Point pt)
        {
            // send SendMouseInput works in term of the phisical mouse buttons, therefore we need
            // to check to see if the mouse buttons are swapped because this method need to use the primary
            // mouse button.
            if (0 == Win32.GetSystemMetrics(Win32.SM_SWAPBUTTON))
            {
                // the mouse buttons are not swaped the primary is the left
                LeftClick(pt);
                LeftClick(pt);
            }
            else
            {
                // the mouse buttons are swaped so click the right button which as actually the primary
                RightClick(pt);
                RightClick(pt);
            }
        }

        // Used internally by the HWND SetFocus code - it sends a hotkey to
        // itself - because it uses a VK that's not on the keyboard, it needs
        // to send the VK directly, not the scan code, which regular
        // SendKeyboardInput does.
        // Note that this method is public, but this class is private, so
        // this is not externally visible.
        internal static void SendKeyboardInputVK(byte vk, bool press) {
            Win32.INPUT ki = new Win32.INPUT();
            ki.type = Win32.INPUT_KEYBOARD;
            ki.union.keyboardInput.wVk = vk;
            ki.union.keyboardInput.wScan = 0;
            ki.union.keyboardInput.dwFlags = press ? 0 : Win32.KEYEVENTF_KEYUP;
            ki.union.keyboardInput.time = 0;
            ki.union.keyboardInput.dwExtraInfo = new IntPtr(0);
            if(0 == Win32.SendInput(1, ref ki, Marshal.SizeOf(ki))) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        internal static bool IsExtendedKey(Key key)
        {
            // From the SDK:
            // The extended-key flag indicates whether the keystroke message originated from one of
            // the additional keys on the enhanced keyboard. The extended keys consist of the ALT and
            // CTRL keys on the right-hand side of the keyboard; the INS, DEL, HOME, END, PAGE UP,
            // PAGE DOWN, and arrow keys in the clusters to the left of the numeric keypad; the NUM LOCK
            // key; the BREAK (CTRL+PAUSE) key; the PRINT SCRN key; and the divide (/) and ENTER keys in
            // the numeric keypad. The extended-key flag is set if the key is an extended key. 
            //
            // - docs appear to be incorrect. Use of Spy++ indicates that break is not an extended key.
            // Also, menu key and windows keys also appear to be extended.
            return key == Key.RightAlt
                || key == Key.RightCtrl
                || key == Key.NumLock
                || key == Key.Insert
                || key == Key.Delete
                || key == Key.Home
                || key == Key.End
                || key == Key.Prior
                || key == Key.Next
                || key == Key.Up
                || key == Key.Down
                || key == Key.Left
                || key == Key.Right
                || key == Key.Apps
                || key == Key.RWin
                || key == Key.LWin;

            // Note that there are no distinct values for the following keys:
            // numpad divide
            // numpad enter
        }

        // Injects a string of Unicode characters using simulated keyboard input
        // with user defined timing
        // <param name="data">The unicode string to be sent</param>
        // <param name="sleepLength">How long, in milliseconds, to sleep between each character</param>
        private static void InternalSendUnicodeString(string data, int sleepLength) {
            char[] chardata = data.ToCharArray();

            foreach (char c in chardata) {
                SendUnicodeKeyboardInput(c, true);
                System.Threading.Thread.Sleep(sleepLength);
                SendUnicodeKeyboardInput(c, false);
                System.Threading.Thread.Sleep(sleepLength);
            }
        }

        internal static string GetVirtualKeyString(VirtualKeyCode vkCode)
        {
            switch (vkCode)
            {
                case VirtualKeyCode.VK_LBUTTON:
                    return "{LBUTTON}";
                case VirtualKeyCode.VK_RBUTTON:
                    return "{RBUTTON}";
                case VirtualKeyCode.VK_CANCEL:
                    return "{CANCEL}";
                case VirtualKeyCode.VK_MBUTTON:
                    return "{MBUTTON}";
                case VirtualKeyCode.VK_XBUTTON1:
                    return "{XBUTTON1}";
                case VirtualKeyCode.VK_XBUTTON2:
                    return "{XBUTTON2}";
                case VirtualKeyCode.VK_BACK:
                    return "{BACK}";
                case VirtualKeyCode.VK_TAB:
                    return "{TAB}";
                case VirtualKeyCode.VK_CLEAR:
                    return "{CLEAR}";
                case VirtualKeyCode.VK_RETURN:
                    return "{RETURN}";
                case VirtualKeyCode.VK_SHIFT:
                    return "{SHIFT}";
                case VirtualKeyCode.VK_CONTROL:
                    return "{CONTROL}";
                case VirtualKeyCode.VK_MENU:
                    return "{MENU}";
                case VirtualKeyCode.VK_PAUSE:
                    return "{PAUSE}";
                case VirtualKeyCode.VK_CAPITAL:
                    return "{CAPITAL}";
                case VirtualKeyCode.VK_KANA:
                    return "{KANA}";
                case VirtualKeyCode.VK_JUNJA:
                    return "{JUNJA}";
                case VirtualKeyCode.VK_FINAL:
                    return "{FINAL}";
                case VirtualKeyCode.VK_HANJA:
                    return "{HANJA}";
                case VirtualKeyCode.VK_ESCAPE:
                    return "{ESCAPE}";
                case VirtualKeyCode.VK_CONVERT:
                    return "{CONVERT}";
                case VirtualKeyCode.VK_NONCONVERT:
                    return "{NONCONVERT}";
                case VirtualKeyCode.VK_ACCEPT:
                    return "{ACCEPT}";
                case VirtualKeyCode.VK_MODECHANGE:
                    return "{MODECHANGE}";
                case VirtualKeyCode.VK_SPACE:
                    return "{SPACE}";
                case VirtualKeyCode.VK_PRIOR:
                    return "{PRIOR}";
                case VirtualKeyCode.VK_NEXT:
                    return "{NEXT}";
                case VirtualKeyCode.VK_END:
                    return "{END}";
                case VirtualKeyCode.VK_HOME:
                    return "{HOME}";
                case VirtualKeyCode.VK_LEFT:
                    return "{LEFT}";
                case VirtualKeyCode.VK_UP:
                    return "{UP}";
                case VirtualKeyCode.VK_RIGHT:
                    return "{RIGHT}";
                case VirtualKeyCode.VK_DOWN:
                    return "{DOWN}";
                case VirtualKeyCode.VK_SELECT:
                    return "{SELECT}";
                case VirtualKeyCode.VK_PRINT:
                    return "{PRINT}";
                case VirtualKeyCode.VK_EXECUTE:
                    return "{EXECUTE}";
                case VirtualKeyCode.VK_SNAPSHOT:
                    return "{SNAPSHOT}";
                case VirtualKeyCode.VK_INSERT:
                    return "{INSERT}";
                case VirtualKeyCode.VK_DELETE:
                    return "{DELETE}";
                case VirtualKeyCode.VK_HELP:
                    return "{HELP}";
                case VirtualKeyCode.VK_0:
                    return "0";
                case VirtualKeyCode.VK_1:
                    return "1";
                case VirtualKeyCode.VK_2:
                    return "2";
                case VirtualKeyCode.VK_3:
                    return "3";
                case VirtualKeyCode.VK_4:
                    return "4";
                case VirtualKeyCode.VK_5:
                    return "5";
                case VirtualKeyCode.VK_6:
                    return "6";
                case VirtualKeyCode.VK_7:
                    return "7";
                case VirtualKeyCode.VK_8:
                    return "8";
                case VirtualKeyCode.VK_9:
                    return "9";
                case VirtualKeyCode.VK_A:
                    return "A";
                case VirtualKeyCode.VK_B:
                    return "B";
                case VirtualKeyCode.VK_C:
                    return "C";
                case VirtualKeyCode.VK_D:
                    return "D";
                case VirtualKeyCode.VK_E:
                    return "E";
                case VirtualKeyCode.VK_F:
                    return "F";
                case VirtualKeyCode.VK_G:
                    return "G";
                case VirtualKeyCode.VK_H:
                    return "H";
                case VirtualKeyCode.VK_I:
                    return "I";
                case VirtualKeyCode.VK_J:
                    return "J";
                case VirtualKeyCode.VK_K:
                    return "K";
                case VirtualKeyCode.VK_L:
                    return "L";
                case VirtualKeyCode.VK_M:
                    return "M";
                case VirtualKeyCode.VK_N:
                    return "N";
                case VirtualKeyCode.VK_O:
                    return "O";
                case VirtualKeyCode.VK_P:
                    return "P";
                case VirtualKeyCode.VK_Q:
                    return "Q";
                case VirtualKeyCode.VK_R:
                    return "R";
                case VirtualKeyCode.VK_S:
                    return "S";
                case VirtualKeyCode.VK_T:
                    return "T";
                case VirtualKeyCode.VK_U:
                    return "U";
                case VirtualKeyCode.VK_V:
                    return "V";
                case VirtualKeyCode.VK_W:
                    return "W";
                case VirtualKeyCode.VK_X:
                    return "X";
                case VirtualKeyCode.VK_Y:
                    return "Y";
                case VirtualKeyCode.VK_Z:
                    return "Z";
                case VirtualKeyCode.VK_LWIN:
                    return "{LWIN}";
                case VirtualKeyCode.VK_RWIN:
                    return "{RWIN}";
                case VirtualKeyCode.VK_APPS:
                    return "{APPS}";
                case VirtualKeyCode.VK_SLEEP:
                    return "{SLEEP}";
                case VirtualKeyCode.VK_NUMPAD0:
                    return "{NUMPAD0}";
                case VirtualKeyCode.VK_NUMPAD1:
                    return "{NUMPAD1}";
                case VirtualKeyCode.VK_NUMPAD2:
                    return "{NUMPAD2}";
                case VirtualKeyCode.VK_NUMPAD3:
                    return "{NUMPAD3}";
                case VirtualKeyCode.VK_NUMPAD4:
                    return "{NUMPAD4}";
                case VirtualKeyCode.VK_NUMPAD5:
                    return "{NUMPAD5}";
                case VirtualKeyCode.VK_NUMPAD6:
                    return "{NUMPAD6}";
                case VirtualKeyCode.VK_NUMPAD7:
                    return "{NUMPAD7}";
                case VirtualKeyCode.VK_NUMPAD8:
                    return "{NUMPAD8}";
                case VirtualKeyCode.VK_NUMPAD9:
                    return "{NUMPAD9}";
                case VirtualKeyCode.VK_MULTIPLY:
                    return "*";
                case VirtualKeyCode.VK_ADD:
                    return "+";
                case VirtualKeyCode.VK_SEPARATOR:
                    return "{SEPARATOR}";
                case VirtualKeyCode.VK_SUBTRACT:
                    return "-";
                case VirtualKeyCode.VK_DECIMAL:
                    return ".";
                case VirtualKeyCode.VK_DIVIDE:
                    return "/";
                case VirtualKeyCode.VK_F1:
                    return "{F1}";
                case VirtualKeyCode.VK_F2:
                    return "{F2}";
                case VirtualKeyCode.VK_F3:
                    return "{F3}";
                case VirtualKeyCode.VK_F4:
                    return "{F4}";
                case VirtualKeyCode.VK_F5:
                    return "{F5}";
                case VirtualKeyCode.VK_F6:
                    return "{F6}";
                case VirtualKeyCode.VK_F7:
                    return "{F7}";
                case VirtualKeyCode.VK_F8:
                    return "{F8}";
                case VirtualKeyCode.VK_F9:
                    return "{F9}";
                case VirtualKeyCode.VK_F10:
                    return "{F10}";
                case VirtualKeyCode.VK_F11:
                    return "{F11}";
                case VirtualKeyCode.VK_F12:
                    return "{F12}";
                case VirtualKeyCode.VK_F13:
                    return "{F13}";
                case VirtualKeyCode.VK_F14:
                    return "{F14}";
                case VirtualKeyCode.VK_F15:
                    return "{F15}";
                case VirtualKeyCode.VK_F16:
                    return "{F16}";
                case VirtualKeyCode.VK_F17:
                    return "{F17}";
                case VirtualKeyCode.VK_F18:
                    return "{F18}";
                case VirtualKeyCode.VK_F19:
                    return "{F19}";
                case VirtualKeyCode.VK_F20:
                    return "{F20}";
                case VirtualKeyCode.VK_F21:
                    return "{F21}";
                case VirtualKeyCode.VK_F22:
                    return "{F22}";
                case VirtualKeyCode.VK_F23:
                    return "{F23}";
                case VirtualKeyCode.VK_F24:
                    return "{F24}";
                case VirtualKeyCode.VK_NAVIGATION_VIEW:
                    return "{NAVIGATION_VIEW}";
                case VirtualKeyCode.VK_NAVIGATION_MENU:
                    return "{NAVIGATION_MENU}";
                case VirtualKeyCode.VK_NAVIGATION_UP:
                    return "{NAVIGATION_UP}";
                case VirtualKeyCode.VK_NAVIGATION_DOWN:
                    return "{NAVIGATION_DOWN}";
                case VirtualKeyCode.VK_NAVIGATION_LEFT:
                    return "{NAVIGATION_LEFT}";
                case VirtualKeyCode.VK_NAVIGATION_RIGHT:
                    return "{NAVIGATION_RIGHT}";
                case VirtualKeyCode.VK_NAVIGATION_ACCEPT:
                    return "{NAVIGATION_ACCEPT}";
                case VirtualKeyCode.VK_NAVIGATION_CANCEL:
                    return "{NAVIGATION_CANCEL}";
                case VirtualKeyCode.VK_NUMLOCK:
                    return "{NUMLOCK}";
                case VirtualKeyCode.VK_SCROLL:
                    return "{SCROLL}";
                case VirtualKeyCode.VK_OEM_NEC_EQUAL:
                    return "{EQUAL}";
                case VirtualKeyCode.VK_OEM_FJ_MASSHOU:
                    return "{MASSHOU}";
                case VirtualKeyCode.VK_OEM_FJ_TOUROKU:
                    return "{TOUROKU}";
                case VirtualKeyCode.VK_OEM_FJ_LOYA:
                    return "{LOYA}";
                case VirtualKeyCode.VK_OEM_FJ_ROYA:
                    return "{ROYA}";
                case VirtualKeyCode.VK_LSHIFT:
                    return "{LSHIFT}";
                case VirtualKeyCode.VK_RSHIFT:
                    return "{RSHIFT}";
                case VirtualKeyCode.VK_LCONTROL:
                    return "{LCONTROL}";
                case VirtualKeyCode.VK_RCONTROL:
                    return "{RCONTROL}";
                case VirtualKeyCode.VK_LMENU:
                    return "{LMENU}";
                case VirtualKeyCode.VK_RMENU:
                    return "{RMENU}";
                case VirtualKeyCode.VK_BROWSER_BACK:
                    return "{BACK}";
                case VirtualKeyCode.VK_BROWSER_FORWARD:
                    return "{FORWARD}";
                case VirtualKeyCode.VK_BROWSER_REFRESH:
                    return "{REFRESH}";
                case VirtualKeyCode.VK_BROWSER_STOP:
                    return "{STOP}";
                case VirtualKeyCode.VK_BROWSER_SEARCH:
                    return "{SEARCH}";
                case VirtualKeyCode.VK_BROWSER_FAVORITES:
                    return "{FAVORITES}";
                case VirtualKeyCode.VK_BROWSER_HOME:
                    return "{HOME}";
                case VirtualKeyCode.VK_VOLUME_MUTE:
                    return "{VOLUME_MUTE}";
                case VirtualKeyCode.VK_VOLUME_DOWN:
                    return "{VOLUME_DOWN}";
                case VirtualKeyCode.VK_VOLUME_UP:
                    return "{VOLUME_UP}";
                case VirtualKeyCode.VK_MEDIA_NEXT_TRACK:
                    return "{MEDIA_NEXT_TRACK}";
                case VirtualKeyCode.VK_MEDIA_PREV_TRACK:
                    return "{MEDIA_PREV_TRACK}";
                case VirtualKeyCode.VK_MEDIA_STOP:
                    return "{MEDIA_STOP}";
                case VirtualKeyCode.VK_MEDIA_PLAY_PAUSE:
                    return "{MEDIA_PLAY_PAUSE}";
                case VirtualKeyCode.VK_LAUNCH_MAIL:
                    return "{LAUNCH_MAIL}";
                case VirtualKeyCode.VK_LAUNCH_MEDIA_SELECT:
                    return "{LAUNCH_MEDIA_SELECT}";
                case VirtualKeyCode.VK_LAUNCH_APP1:
                    return "{LAUNCH_APP1}";
                case VirtualKeyCode.VK_LAUNCH_APP2:
                    return "{LAUNCH_APP2}";
                case VirtualKeyCode.VK_OEM_1:
                    return "{OEM_1}";
                case VirtualKeyCode.VK_OEM_PLUS:
                    return "{OEM_PLUS}";
                case VirtualKeyCode.VK_OEM_COMMA:
                    return "{OEM_COMMA}";
                case VirtualKeyCode.VK_OEM_MINUS:
                    return "{OEM_MINUS}";
                case VirtualKeyCode.VK_OEM_PERIOD:
                    return "{OEM_PERIOD}";
                case VirtualKeyCode.VK_OEM_2:
                    return "{OEM_2}";
                case VirtualKeyCode.VK_OEM_3:
                    return "{VK_OEM_3}";
            }
            return "";
        }
    }
}
