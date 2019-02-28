using HookManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Walkabout.Utilities;

namespace KeyboardMonkey
{
    class Monkey
    {
        DelayedActions actions = new DelayedActions();
        List<KeyboardInput> script;
        int scriptPosition;
        int startTime;
        bool stopped;
        IntPtr active;
        int delay;

        public event EventHandler Progress;

        public int Position {  get { return scriptPosition; } }

        public int Maximum { get { return script.Count; } }

        public Monkey(List<KeyboardInput> script, int delay = 30)
        {
            this.delay = delay;
            this.script = script;
        }

        internal void Start()
        {
            this.startTime = SafeNativeMethods.GetTickCount();
            this.scriptPosition = 0;
            this.stopped = false;
            SendNextKeystroke();
        }

        private void SendNextKeystroke()
        {
            if (scriptPosition <= script.Count)
            {
                KeyboardInput input = script[scriptPosition++];
                IntPtr hwnd = SafeNativeMethods.GetForegroundWindow();
                if (hwnd != input.hwnd && hwnd != active)
                {
                    Debug.WriteLine("Current Window {0}, Activating window {1}", hwnd.ToString("x"), input.hwnd.ToString("x"));
                    SafeNativeMethods.SetForegroundWindow(input.hwnd);
                    System.Threading.Thread.Sleep(1000);
                    active = SafeNativeMethods.GetForegroundWindow();
                }
                Input.SendKeyboardInput(KeyInterop.KeyFromVirtualKey(input.vkCode), input.pressed);

                if (scriptPosition < script.Count)
                {
                    KeyboardInput next = script[scriptPosition];
                    int delta = next.time - script[0].time;
                    int realDelta = SafeNativeMethods.GetTickCount() - this.startTime;
                    if (!stopped)
                    {
                        this.actions.StartDelayedAction("KeyMonkey", SendNextKeystroke, TimeSpan.FromMilliseconds(this.delay));
                    }
                }

                if (Progress != null)
                {
                    Progress(this, EventArgs.Empty);
                }
            }
        }

        public void Stop()
        {
            stopped = true;
            this.actions.CancelDelayedAction("KeyMonkey");
        }

    }
}
