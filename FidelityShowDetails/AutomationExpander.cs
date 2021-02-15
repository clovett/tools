using Interop.UIAutomationCore;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace FidelityShowDetails
{
    class AutomationExpander
    {
        public static void ExpandAll(FlowDocument document)
        {
            document.Blocks.Clear();
            Paragraph p = new Paragraph();
            document.Blocks.Add(p);

            IntPtr window = IntPtr.Zero;
            foreach (var w in SafeNativeMethods.GetDesktopWindows())
            {
                var text = SafeNativeMethods.GetWindowText(w);
                if (text.Contains("Fidelity Netbenefits"))
                {
                    window = w;
                    p.Inlines.Add(new Run("Found window 'Fidelity Netbenefits'"));
                    break;
                }
            }
            // IntPtr window = SafeNativeMethods.FindWindow("Chrome_WidgetWin_1", "Fidelity Netbenefits - Transaction History - Personal - Microsoft​ Edge");
            if (window != IntPtr.Zero)
            {
                var cuia = new Interop.UIAutomationCore.CUIAutomation();
                var elementBrowser = cuia.ElementFromHandle(window);

                // Thanks to https://code.msdn.microsoft.com/Windows-7-UI-Automation-0625f55e/view/Reviews for this code.
                IUIAutomationCondition conditionControlView = cuia.ControlViewCondition;
                IUIAutomationCondition conditionButton = cuia.CreatePropertyCondition(
                    (int)UIAutomationPropertyType.ControlType, (int)UIAutomationControlType.Button);
                IUIAutomationCondition condition = cuia.CreateAndCondition(conditionControlView, conditionButton);
                IUIAutomationElementArray elementArray = elementBrowser.FindAll(Interop.UIAutomationCore.TreeScope.TreeScope_Descendants, condition);
                int n = elementArray.Length;
                int count = 0;
                for (int i = 0; i < n; i++)
                {
                    IUIAutomationElement elementLink = elementArray.GetElement(i);
                    string name = elementLink.CurrentName;
                    p.Inlines.Add(new LineBreak());
                    p.Inlines.Add("found link " + name);
                    if (name.Contains("Show Details"))
                    {
                        try
                        {
                            var pattern = (IUIAutomationInvokePattern)elementLink.GetCurrentPattern((int)UIAutomationPattern.UIA_InvokePatternId);
                            if (pattern != null)
                            {
                                pattern.Invoke();
                                count++;
                            }
                            else
                            {
                                var expand = (IUIAutomationExpandCollapsePattern)elementLink.GetCurrentPattern((int)UIAutomationPattern.UIA_ExpandCollapsePatternId);
                                expand.Expand();
                                count++;
                            }
                        }
                        catch
                        {
                            // hmmm
                        }
                    }
                }
                if (count == 0)
                {
                    p.Inlines.Add(new LineBreak());
                    p.Inlines.Add("found no links like 'Show Details'");
                } 
                else
                {
                    p.Inlines.Add(new LineBreak());
                    p.Inlines.Add(string.Format("found {0} links like 'Show Details'", count));
                }
            }
        }

        enum UIAutomationPropertyType
        {
            ControlType = 30003
        }

        enum UIAutomationControlType
        {
            Button = 0xc350,
            Hyperlink = 0xc355
        }

        // Error CS1752  Interop type 'UIA_PatternIds' cannot be embedded.Use the applicable interface instead.	        
        enum UIAutomationPattern
        {
            UIA_InvokePatternId = 10000,
            UIA_ExpandCollapsePatternId = 10005
        }

    }
}
