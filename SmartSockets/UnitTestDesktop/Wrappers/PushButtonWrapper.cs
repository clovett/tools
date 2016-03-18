using System;
using System.Threading;
using System.Windows.Automation;

namespace UnitTestDesktop
{
    public class PushButtonWrapper
    {
        private AutomationElement button;

        public PushButtonWrapper(AutomationElement button)
        {
            this.button = button;
        }

        public void Click()
        {
            for (int retries = 5; retries > 0; retries--)
            {
                if (button.Current.IsEnabled)
                {
                    break;
                }
                button.SetFocus();
                Thread.Sleep(250);
            }
            if (!button.Current.IsEnabled)
            {
                throw new Exception("Cannot invoke disabled button: " + button.Current.Name);
            }

            InvokePattern invoke = (InvokePattern)button.GetCurrentPattern(InvokePattern.Pattern);
            invoke.Invoke();
        }

        public bool IsEnabled
        {
            get
            {
                return button.Current.IsEnabled;
            }
        }

    }
}