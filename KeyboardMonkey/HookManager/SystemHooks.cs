using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HookManager
{
    public class SystemHooks
    {
        KeyboardHook llhook;
        CbtHook cbthook;

        public void HookCbt()
        {
            cbthook = new CbtHook();
        }

        public void HookKeyboard(EventHandler<KeyboardInput> handler)
        {
            llhook = new KeyboardHook(handler);
        }
    }

    public class KeyboardInput
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public bool pressed; // true for key down, false for key up
        public IntPtr hwnd;
    }

    class CbtHook
    {
        IntPtr cbtHook;
        Win32.HookProc cbtDelegate;
        IntPtr hmod;

        public CbtHook()
        {
            hmod = Win32.LoadLibrary(this.GetType().Assembly.Location);
            this.cbtDelegate = new Win32.HookProc(CBTHookProc);
            this.cbtHook = Win32.SetWindowsHookEx(Win32.WH_CBT, cbtDelegate, hmod, 0);
            if (this.cbtHook == IntPtr.Zero)
            {
                int rc = Win32.GetLastError(); // 1428=ERROR_HOOK_NEEDS_HMOD
                Debug.WriteLine("Error from SetWindowsHookEx for WH_CBT " + rc.ToString());
            }
            else
            {
                Debug.WriteLine("SetWindowsHookEx for WH_CBT succeeded");
            }
        }

        ~CbtHook()
        {
            Win32.FreeLibrary(hmod);
            if (this.cbtHook != IntPtr.Zero)
            {
                Debug.WriteLine("Unhooked cbt");
                Win32.UnhookWindowsHookEx(this.cbtHook);
            }
        }

        private IntPtr CBTHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            CBTAction action = (CBTAction)code;
            Debug.WriteLine("Received cbt event: " + action.ToString());
            return Win32.CallNextHookEx(this.cbtHook, code, wParam, lParam);
        }
    }

    class KeyboardHook
    {
        IntPtr keyboardHook;
        Win32.HookProc keyboardDelegate;
        IntPtr hmod;
        EventHandler<KeyboardInput> handler;

        public KeyboardHook(EventHandler<KeyboardInput> handler)
        {
            this.handler = handler;
            this.keyboardDelegate = new Win32.HookProc(KeyboardHookProc);
            hmod = Win32.LoadLibrary(this.GetType().Assembly.Location);
            int nativeThreadId = Win32.GetCurrentThreadId();
            this.keyboardHook = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, this.keyboardDelegate, hmod, 0);
            if (this.keyboardHook == IntPtr.Zero)
            {
                int rc = Win32.GetLastError(); // 1428=ERROR_HOOK_NEEDS_HMOD, 1429=ERROR_GLOBAL_ONLY_HOOK
                Debug.WriteLine("Error from SetWindowsHookEx for WH_KEYBOARD_LL: " + rc.ToString());
            }
            else
            {
                Debug.WriteLine("SetWindowsHookEx for WH_KEYBOARD_LL succeeded");
            }
        }

        ~KeyboardHook()
        {
            if (this.keyboardHook != IntPtr.Zero)
            {
                Debug.WriteLine("Unhooked low level keyboard");
                Win32.UnhookWindowsHookEx(this.keyboardHook);
            }
            Win32.FreeLibrary(hmod);
        }

        private IntPtr KeyboardHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            long flags = (long)lParam;
            KBDLLHOOKSTRUCT data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);            
            handler(this, new KeyboardInput()
            {
                flags = data.flags,
                scanCode = data.scanCode,
                vkCode = data.vkCode,
                time = data.time,
                // bit 7: The transition state. The value is 0 if the key is pressed and 1 if it is being released.
                pressed = (data.flags & 0x80) == 0
            });
            return Win32.CallNextHookEx(this.keyboardHook, code, wParam, lParam);
        }

        private IntPtr CBTHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            CBTAction action = (CBTAction)code;
            Debug.WriteLine("Received cbt event: " + action.ToString());
            return Win32.CallNextHookEx(this.keyboardHook, code, wParam, lParam);
        }

    }
}
