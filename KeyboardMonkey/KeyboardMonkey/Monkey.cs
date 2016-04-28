using System;
using System.Collections.Generic;
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
        DelayedActions actions;
        IntPtr hwnd;
        Key key;
        int repeat;
        int delay;
        bool stopped;

        public event EventHandler Progress;

        public int Remanining {  get { return repeat; } }

        public Monkey(DelayedActions actions, IntPtr hwnd, Key key, int repeat, int delay)
        {
            this.actions = actions;
            this.hwnd = hwnd;
            this.key = key;
            this.repeat = repeat - 1;
            this.delay = delay;
            this.actions.StartDelayedAction("KeyMonkey", SendNextKeystroke, TimeSpan.FromMilliseconds(delay));
        }

        private void SendNextKeystroke()
        {
            SafeNativeMethods.SetForegroundWindow(hwnd);
            Input.TapKey(this.key);

            if (!stopped && repeat > 0)
            {
                repeat--;
                this.actions.StartDelayedAction("KeyMonkey", SendNextKeystroke, TimeSpan.FromMilliseconds(delay));
            }

            if (Progress != null)
            {
                Progress(this, EventArgs.Empty);
            }
        }

        public void Stop()
        {
            stopped = true;
            this.actions.CancelDelayedAction("KeyMonkey");
        }
    }
}
