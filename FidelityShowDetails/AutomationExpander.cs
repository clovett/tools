using Interop.UIAutomationCore;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FidelityShowDetails
{
    class AutomationExpander
    {
        public static void ExpandAll()
        {
            IntPtr window = SafeNativeMethods.FindWindow("Windows.UI.Core.CoreWindow", "Microsoft Edge");
            if (window != IntPtr.Zero)
            {
                var cuia = new Interop.UIAutomationCore.CUIAutomation();
                var elementBrowser = cuia.ElementFromHandle(window);

                // Thanks to https://code.msdn.microsoft.com/Windows-7-UI-Automation-0625f55e/view/Reviews for this code.
                IUIAutomationCondition conditionControlView = cuia.ControlViewCondition;
                IUIAutomationCondition conditionHyperlink = cuia.CreatePropertyCondition(
                    (int)UIAutomationPropertyType.ControlType, (int)UIAutomationControlType.Hyperlink);
                IUIAutomationCondition condition = cuia.CreateAndCondition(conditionControlView, conditionHyperlink);
                IUIAutomationElementArray elementArray = elementBrowser.FindAll(Interop.UIAutomationCore.TreeScope.TreeScope_Descendants, condition);
                int n = elementArray.Length;
                for (int i = 0; i < n; i++)
                {
                    IUIAutomationElement elementLink = elementArray.GetElement(i);
                    string name = elementLink.CurrentName;
                    Debug.WriteLine(name);
                    if (name.Contains("Show Details"))
                    {
                        try
                        {
                            var pattern = (IUIAutomationInvokePattern)elementLink.GetCurrentPattern((int)UIAutomationPattern.Invoke);
                            pattern.Invoke();
                        }
                        catch
                        {
                            // hmmm
                        }
                    }
                }
            }
        }

        enum UIAutomationPropertyType
        {
            ControlType = 30003
        }

        enum UIAutomationControlType
        {
            Hyperlink = 50005
        }

        enum UIAutomationPattern
        {
            Invoke = 10000
        }

    }
}
