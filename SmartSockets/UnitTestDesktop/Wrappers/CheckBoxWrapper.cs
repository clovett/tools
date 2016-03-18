using System.Windows.Automation;
using System;

namespace UnitTestDesktop
{
    public class CheckBoxWrapper
    {
        private AutomationElement box;

        public CheckBoxWrapper(AutomationElement box)
        {
            this.box = box;
        }

        public bool IsChecked
        {
            get
            {
                TogglePattern p = (TogglePattern)box.GetCurrentPattern(TogglePattern.Pattern);
                if (p == null)
                {
                    throw new Exception("CheckBox '" + box.Current.Name + "' does not support the TogglePattern");
                }
                return p.Current.ToggleState == ToggleState.On;
            }
            set
            {
                TogglePattern p = (TogglePattern)box.GetCurrentPattern(TogglePattern.Pattern);
                if (p == null)
                {
                    throw new Exception("CheckBox '" + box.Current.Name + "' does not support the TogglePattern");
                }
                if (value)
                {
                    if (p.Current.ToggleState != ToggleState.On)
                    {
                        p.Toggle();
                    }
                }
                else
                {
                    if (p.Current.ToggleState == ToggleState.On)
                    {
                        p.Toggle();
                    }
                }
            }
        }
    }
}