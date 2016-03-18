using System;
using System.Windows.Automation;

namespace UnitTestDesktop
{
    public class TextBoxWrapper
    {
        private AutomationElement box;

        public TextBoxWrapper(AutomationElement box)
        {
            this.box = box;
        }

        public string GetValue()
        {
            ValuePattern p = (ValuePattern)box.GetCurrentPattern(ValuePattern.Pattern);
            if (p == null)
            {
                throw new Exception("Element does not support ValuePattern");
            }
            return p.Current.Value;
        }

        public void SetValue(string value)
        {
            ValuePattern p = (ValuePattern)box.GetCurrentPattern(ValuePattern.Pattern);
            if (p == null)
            {
                throw new Exception("Element does not support ValuePattern");
            }
            if (p.Current.IsReadOnly)
            {
                throw new Exception("Element is read only right now");
            }
            p.SetValue(value);
        }

        internal void Focus()
        {
            box.SetFocus();

            int retries = 10;
            while (AutomationElement.FocusedElement != box && retries-- > 0)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (AutomationElement.FocusedElement != box)
            {
                throw new Exception(string.Format("Failing to set focus on TextBox '{0}'", box.Current.Name));
            }
            return;
        }
    }
}