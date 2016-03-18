using System;
using System.Windows.Automation;

namespace UnitTestDesktop
{
    public class ComboBoxWrapper
    {
        private AutomationElement box;

        public ComboBoxWrapper(AutomationElement box)
        {
            this.box = box;
        }

        public string GetComboText()
        {
            // editable combo boxes expose a ValuePattern.
            return GetValue(box);
        }

        public void SetComboText(string value)
        {
            // editable combo boxes expose a ValuePattern.
            SetValue(box, value);
        }

        public string GetSelectedItem()
        {
            SelectionPattern si = (SelectionPattern)box.GetCurrentPattern(SelectionPattern.Pattern);
            AutomationElement[] array = si.Current.GetSelection();
            if (array == null || array.Length == 0)
            {
                return "";
            }

            AutomationElement item = array[0];
            return GetValue(item);
        }

        public AutomationElement SelectItem(string value)
        {
            ExpandCollapsePattern p = (ExpandCollapsePattern)box.GetCurrentPattern(ExpandCollapsePattern.Pattern);
            p.Expand();

            AutomationElement item = box.FindFirstWithRetries(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.NameProperty, value),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem)));
            if (item == null)
            {
                throw new Exception("ComboBoxItem '" + value + "' not found in combo box '" + box.Current.Name + "'");
            }

            SelectionItemPattern si = (SelectionItemPattern)item.GetCurrentPattern(SelectionItemPattern.Pattern);
            si.Select();

            p.Collapse();
            return box;
        }


        private string GetValue(AutomationElement item)
        {
            ValuePattern p = (ValuePattern)item.GetCurrentPattern(ValuePattern.Pattern);
            if (p == null)
            {
                throw new Exception("Item does not support ValuePattern");
            }
            return p.Current.Value;
        }

        private void SetValue(AutomationElement item, string value)
        {
            ValuePattern p = (ValuePattern)item.GetCurrentPattern(ValuePattern.Pattern);
            if (p == null)
            {
                throw new Exception("Item does not support ValuePattern");
            }
            if (p.Current.IsReadOnly)
            {
                throw new Exception("Element is read only right now");
            }
            p.SetValue(value);
        }

    }
}