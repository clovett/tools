using System;
using System.Windows.Automation;

namespace UnitTestDesktop
{
    public class RadioButtonWrapper
    {
        private AutomationElement radio;

        public RadioButtonWrapper(AutomationElement radio)
        {
            this.radio = radio;
        }


        bool IsSelected
        {
            get
            {
                SelectionItemPattern p = (SelectionItemPattern)radio.GetCurrentPattern(SelectionItemPattern.Pattern);
                if (p == null)
                {
                    throw new System.Exception(string.Format("Radio button '{0}' doesn't support SelectionItemPatern", radio.Current.Name));
                }
                return p.Current.IsSelected;
            }
            set
            {

                if (!value)
                {
                    throw new ArgumentException("Cannot clear a radio button", "value");
                }

                SelectionItemPattern p = (SelectionItemPattern)radio.GetCurrentPattern(SelectionItemPattern.Pattern);
                if (p == null)
                {
                    throw new System.Exception(string.Format("Radio button '{0}' doesn't support SelectionItemPatern", radio.Current.Name));
                }
                p.Select();
            }
        }
        
    }
}