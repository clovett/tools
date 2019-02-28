using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace HookManager
{
    class Win32
    {
        [DllImport("Kernel32", CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("Kernel32", CharSet = CharSet.Unicode)]
        internal static extern bool FreeLibrary(IntPtr hmod);

        /// <summary>
        /// Get the process and thread id of the given window.
        /// </summary>
        /// <param name="hwnd">The window handle to query</param>
        /// <param name="procId">The process that created the window</param>
        /// <returns>The thread id for the window</returns>
        [DllImport("User32", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowThreadProcessId(IntPtr hwnd, out int procId);

        /// <summary>
        /// An application-defined or library-defined callback function used with the SetWindowsHookEx function. 
        /// The system calls this function every time a new keyboard input event is about to be posted into a thread input queue.
        /// </summary>
        /// <param name="code">A code the hook procedure uses to determine how to process the message. If nCode is less than zero, 
        /// the hook procedure must pass the message to the CallNextHookEx function without further processing and should return the 
        /// value returned by CallNextHookEx. If the code is HC_ACTION=0, then The wParam and lParam parameters contain information about a keyboard message.
        /// If the code is HC_NOREMOVE then the application called PeekMessage.
        /// </param>
        /// <param name="wParam">The identifier of the keyboard message. This parameter can be one of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.</param>
        /// <param name="lParam">A pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns>If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx.
        /// If nCode is greater than or equal to zero, and the hook procedure did not process the message, it is highly recommended that you call CallNextHookEx and return the value it returns</returns>
        public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        public const int WH_KEYBOARD = 2;
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_CBT = 5;

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hmod, int dwThreadId);

        [DllImport("user32.dll")]
        public static extern IntPtr UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        IntPtr dwExtraInfo;
    };

    enum KeyboardHookAction
    {
        HC_ACTION = 0,
        HC_NOREMOVE = 1
    };

    enum CBTAction
    {
        HCBT_MOVESIZE = 0,
        HCBT_MINMAX = 1,
        HCBT_QS = 2,
        HCBT_CREATEWND = 3,
        HCBT_DESTROYWND = 4,
        HCBT_ACTIVATE = 5,
        HCBT_CLICKSKIPPED = 6,
        HCBT_KEYSKIPPED = 7,
        HCBT_SYSCOMMAND = 8,
        HCBT_SETFOCUS = 9
    };

    public enum VirtualKeyCode
    {

        /*
         * Virtual Keys, Standard Set
         */
        VK_LBUTTON        = 0x01,
        VK_RBUTTON        = 0x02,
        VK_CANCEL         = 0x03,
        VK_MBUTTON        = 0x04,    /* NOT contiguous with L & RBUTTON */


        VK_XBUTTON1       = 0x05,    /* NOT contiguous with L & RBUTTON */
        VK_XBUTTON2       = 0x06,    /* NOT contiguous with L & RBUTTON */

        VK_BACK           = 0x08,
        VK_TAB            = 0x09,

        VK_CLEAR          = 0x0C,
        VK_RETURN         = 0x0D,

        VK_SHIFT          = 0x10,
        VK_CONTROL        = 0x11,
        VK_MENU           = 0x12,
        VK_PAUSE          = 0x13,
        VK_CAPITAL        = 0x14,

        VK_KANA           = 0x15,
        VK_HANGEUL        = 0x15,  /* old name - should be here for compatibility */
        VK_HANGUL         = 0x15,

        VK_JUNJA          = 0x17,
        VK_FINAL          = 0x18,
        VK_HANJA          = 0x19,
        VK_KANJI          = 0x19,

        VK_ESCAPE         = 0x1B,

        VK_CONVERT        = 0x1C,
        VK_NONCONVERT     = 0x1D,
        VK_ACCEPT         = 0x1E,
        VK_MODECHANGE     = 0x1F,

        VK_SPACE          = 0x20,
        VK_PRIOR          = 0x21,
        VK_NEXT           = 0x22,
        VK_END            = 0x23,
        VK_HOME           = 0x24,
        VK_LEFT           = 0x25,
        VK_UP             = 0x26,
        VK_RIGHT          = 0x27,
        VK_DOWN           = 0x28,
        VK_SELECT         = 0x29,
        VK_PRINT          = 0x2A,
        VK_EXECUTE        = 0x2B,
        VK_SNAPSHOT       = 0x2C,
        VK_INSERT         = 0x2D,
        VK_DELETE         = 0x2E,
        VK_HELP           = 0x2F,

                /*
                 * VK_0 - VK_9 are the same as ASCII '0' - '9' (0x30 - 0x39) */
        VK_0 = 0x30,
        VK_1 = 0x31,
        VK_2 = 0x32,
        VK_3 = 0x33,
        VK_4 = 0x34,
        VK_5 = 0x35,
        VK_6 = 0x36,
        VK_7 = 0x37,
        VK_8 = 0x38,
        VK_9 = 0x39,
                 /* 0x3A - 0x40 : unassigned
                 * VK_A - VK_Z are the same as ASCII 'A' - 'Z' (0x41 - 0x5A)
                 */
        VK_A = 0x41,
        VK_B = 0x42,
        VK_C = 0x43,
        VK_D = 0x44,
        VK_E = 0x45,
        VK_F = 0x46,
        VK_G = 0x47,
        VK_H = 0x48,
        VK_I = 0x49,
        VK_J = 0x4A,
        VK_K = 0x4B,
        VK_L = 0x4C,
        VK_M = 0x4D,
        VK_N = 0x4E,
        VK_O = 0x4F,
        VK_P = 0x50,
        VK_Q = 0x51,
        VK_R = 0x52,
        VK_S = 0x53,
        VK_T = 0x54,
        VK_U = 0x55,
        VK_V = 0x56,
        VK_W = 0x57,
        VK_X = 0x58,
        VK_Y = 0x59,
        VK_Z = 0x5A,

        VK_LWIN           = 0x5B,
        VK_RWIN           = 0x5C,
        VK_APPS           = 0x5D,

                /*
                 * 0x5E : reserved
                 */

        VK_SLEEP          = 0x5F,

        VK_NUMPAD0        = 0x60,
        VK_NUMPAD1        = 0x61,
        VK_NUMPAD2        = 0x62,
        VK_NUMPAD3        = 0x63,
        VK_NUMPAD4        = 0x64,
        VK_NUMPAD5        = 0x65,
        VK_NUMPAD6        = 0x66,
        VK_NUMPAD7        = 0x67,
        VK_NUMPAD8        = 0x68,
        VK_NUMPAD9        = 0x69,
        VK_MULTIPLY       = 0x6A,
        VK_ADD            = 0x6B,
        VK_SEPARATOR      = 0x6C,
        VK_SUBTRACT       = 0x6D,
        VK_DECIMAL        = 0x6E,
        VK_DIVIDE         = 0x6F,
        VK_F1             = 0x70,
        VK_F2             = 0x71,
        VK_F3             = 0x72,
        VK_F4             = 0x73,
        VK_F5             = 0x74,
        VK_F6             = 0x75,
        VK_F7             = 0x76,
        VK_F8             = 0x77,
        VK_F9             = 0x78,
        VK_F10            = 0x79,
        VK_F11            = 0x7A,
        VK_F12            = 0x7B,
        VK_F13            = 0x7C,
        VK_F14            = 0x7D,
        VK_F15            = 0x7E,
        VK_F16            = 0x7F,
        VK_F17            = 0x80,
        VK_F18            = 0x81,
        VK_F19            = 0x82,
        VK_F20            = 0x83,
        VK_F21            = 0x84,
        VK_F22            = 0x85,
        VK_F23            = 0x86,
        VK_F24            = 0x87,

        /*
         * 0x88 - 0x8F : UI navigation
         */

        VK_NAVIGATION_VIEW     = 0x88, // reserved
        VK_NAVIGATION_MENU     = 0x89, // reserved
        VK_NAVIGATION_UP       = 0x8A, // reserved
        VK_NAVIGATION_DOWN     = 0x8B, // reserved
        VK_NAVIGATION_LEFT     = 0x8C, // reserved
        VK_NAVIGATION_RIGHT    = 0x8D, // reserved
        VK_NAVIGATION_ACCEPT   = 0x8E, // reserved
        VK_NAVIGATION_CANCEL   = 0x8F, // reserved


        VK_NUMLOCK        = 0x90,
        VK_SCROLL         = 0x91,

                /*
                 * NEC PC-9800 kbd definitions
                 */
        VK_OEM_NEC_EQUAL  = 0x92,   // '=' key on numpad

                /*
                 * Fujitsu/OASYS kbd definitions
                 */
        VK_OEM_FJ_JISHO   = 0x92,   // 'Dictionary' key
        VK_OEM_FJ_MASSHOU = 0x93,   // 'Unregister word' key
        VK_OEM_FJ_TOUROKU = 0x94,   // 'Register word' key
        VK_OEM_FJ_LOYA    = 0x95,   // 'Left OYAYUBI' key
        VK_OEM_FJ_ROYA    = 0x96,   // 'Right OYAYUBI' key

                /*
                 * 0x97 - 0x9F : unassigned
                 */

                /*
                 * VK_L* & VK_R* - left and right Alt, Ctrl and Shift virtual keys.
                 * Used only as parameters to GetAsyncKeyState() and GetKeyState().
                 * No other API or message will distinguish left and right keys in this way.
                 */
        VK_LSHIFT         = 0xA0,
        VK_RSHIFT         = 0xA1,
        VK_LCONTROL       = 0xA2,
        VK_RCONTROL       = 0xA3,
        VK_LMENU          = 0xA4,
        VK_RMENU          = 0xA5,

        VK_BROWSER_BACK        = 0xA6,
        VK_BROWSER_FORWARD     = 0xA7,
        VK_BROWSER_REFRESH     = 0xA8,
        VK_BROWSER_STOP        = 0xA9,
        VK_BROWSER_SEARCH      = 0xAA,
        VK_BROWSER_FAVORITES   = 0xAB,
        VK_BROWSER_HOME        = 0xAC,

        VK_VOLUME_MUTE         = 0xAD,
        VK_VOLUME_DOWN         = 0xAE,
        VK_VOLUME_UP           = 0xAF,
        VK_MEDIA_NEXT_TRACK    = 0xB0,
        VK_MEDIA_PREV_TRACK    = 0xB1,
        VK_MEDIA_STOP          = 0xB2,
        VK_MEDIA_PLAY_PAUSE    = 0xB3,
        VK_LAUNCH_MAIL         = 0xB4,
        VK_LAUNCH_MEDIA_SELECT = 0xB5,
        VK_LAUNCH_APP1         = 0xB6,
        VK_LAUNCH_APP2         = 0xB7,


                /*
                 * 0xB8 - 0xB9 : reserved
                 */

        VK_OEM_1          = 0xBA,   // ';:' for US
        VK_OEM_PLUS       = 0xBB,   // '+' any country
        VK_OEM_COMMA      = 0xBC,   // ',' any country
        VK_OEM_MINUS      = 0xBD,   // '-' any country
        VK_OEM_PERIOD     = 0xBE,   // '.' any country
        VK_OEM_2          = 0xBF,   // '/?' for US
        VK_OEM_3          = 0xC0,   // '`~' for US

    };
}
