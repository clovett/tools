using System;
using System.Threading;
using System.Windows.Automation;

namespace UnitTestDesktop
{
    internal class RichTextBoxWrapper
    {
        private AutomationElement box;

        public RichTextBoxWrapper(AutomationElement box)
        {
            this.box = box;
        }

        public string GetValue()
        {
            TextPattern tp = (TextPattern)box.GetCurrentPattern(TextPattern.Pattern);
            if (tp == null)
            {
                throw new Exception(string.Format("Element '{0}' does not support the TextPattern", box.Current.Name));
            }
            return tp.DocumentRange.GetText(-1);
        }

        public void WaitForText(string expecting)
        {
            int retries = 10;
            string output = this.GetValue();
            while (!output.Contains(expecting) && retries-- > 0)
            {
                Thread.Sleep(1000);
                output = this.GetValue();
            }

            if (!output.Contains(expecting))
            {
                throw new Exception(string.Format("RichTextBox does not contain the expected text '{0}', instead it contains '{1}'", expecting, output));
            }
        }

        internal void Clear()
        {
            TextPattern tp = (TextPattern)box.GetCurrentPattern(TextPattern.Pattern);
            if (tp == null)
            {
                throw new Exception(string.Format("Element '{0}' does not support the TextPattern", box.Current.Name));
            }
            try
            {
                tp.DocumentRange.Select();
                box.SetFocus();
                Input.TapKey(System.Windows.Input.Key.Delete);
                return;
            }
            catch
            {

            }
            return;
        }
    }
}