using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms;

namespace UnitTestDesktop
{
    public class DialogWrapper
    {
        /*
        SystemMenuBar
	        Restore: 61728
	        Move: 61456
	        Size: 61440
	        Minimize: 61472
	        Maximize: 61488
	        Close: 61536
        */
        protected AutomationElement window;

        public DialogWrapper(AutomationElement window)
        {
            this.window = window;
        }

        public AutomationElement Element { get { return window; } }

        public void Close()
        {
            GetButton("Close").Click();
        }

        public void Minimize()
        {
            GetButton("Minimize-Restore").Click();
        }

        public void Maximize()
        {
            GetButton("Maximize-Restore").Click();
        }

        public PushButtonWrapper GetButton(string name)
        {
            AutomationElement button = window.FindFirstWithRetries(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, name));
            if (button == null)
            {
                throw new Exception("Button '" + name + "' not found");
            }
            return new PushButtonWrapper(button);
        }

        internal static DialogWrapper FindMainWindow(int processId, string name, int retries = 10)
        {
            for (int i = 0; i < retries; i++)
            {
                AutomationElement e = Win32.FindWindow(processId, name);
                if (e != null)
                {
                    return new DialogWrapper(e);
                }

                Thread.Sleep(1000);
            }

            throw new Exception("MainWindow not found for process " + processId);
        }


        public AutomationElement SelectTab(string name)
        {
            AutomationElement tab = window.FindFirstWithRetries(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, name));
            if (tab == null)
            {
                throw new Exception("Tab '" + name + "' not found");
            }
            SelectionItemPattern selectionItem = (SelectionItemPattern)tab.GetCurrentPattern(SelectionItemPattern.Pattern);
            selectionItem.Select();
            return tab;
        }

        internal RichTextBoxWrapper GetRichTextBox(string name)
        {
            AutomationElement box = window.FindFirstWithRetries(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, name));
            if (box == null)
            {
                throw new Exception("RichTextBox '" + name + "' not found");
            }
            if (!box.Current.IsEnabled)
            {
                throw new Exception("RichTextBox '" + name + "' is not enabled");
            }
            return new RichTextBoxWrapper(box);
        }

        internal static void TileWindows(params DialogWrapper[] windows)
        {
            int len = windows.Length;
            if (len < 2)
            {
                return;
            }

            int rows = (int)Math.Ceiling(Math.Sqrt(windows.Length));
            int cols = rows;
            while ((cols * rows) > windows.Length)
            {
                if (((cols - 1) * rows) >= windows.Length)
                {
                    cols--;
                }
                else
                {
                    break;
                }
            }

            Screen screen = GetScreen(windows[0].Element);
            System.Drawing.Rectangle gdiBounds = screen.WorkingArea;

            double width = gdiBounds.Width / cols;
            double height = gdiBounds.Height / rows;

            int pos = 0;
            for (int i = 0; i < cols && pos < len; i++)
            {
                for (int j = 0; j < rows && pos < len; j++)
                {
                    DialogWrapper w = windows[pos++];
                    double x = i * width;
                    double y = j * height;
                    int hwnd = w.Element.Current.NativeWindowHandle;
                    Win32.MoveWindow(new IntPtr(hwnd), (int)x, (int)y, (int)width, (int)height, true);
                }
            }

        }

        private static Screen GetScreen(AutomationElement e)
        {
            Rect bounds = e.Current.BoundingRectangle;
            return Screen.FromPoint(Input.ConvertFromDeviceIndependentPoint(bounds.TopLeft));
        }

        static Point RealPixelsToWpf(Window w, Point p)
        {
            var t = PresentationSource.FromVisual(w).CompositionTarget.TransformFromDevice;
            return t.Transform(p);
        }

        private static void SetPosition(Window window, Point gdiPosition)
        {
            var corner = RealPixelsToWpf(window, gdiPosition);
            window.Left = corner.X;
            window.Top = corner.Y;
        }

        public TextBoxWrapper GetTextBox(string name)
        {
            AutomationElement box = window.FindFirstWithRetries(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, name));
            if (box == null)
            {
                throw new Exception("TextBox '" + name + "' not found");
            }
            if (!box.Current.IsEnabled)
            {
                throw new Exception("TextBox '" + name + "' is not enabled");
            }
            return new TextBoxWrapper(box);
        }

        public CheckBoxWrapper GetCheckBox(string name)
        {
            AutomationElement box = window.FindFirstWithRetries(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, name));
            if (box == null)
            {
                throw new Exception("CheckBox '" + name + "' not found");
            }
            return new CheckBoxWrapper(box);
        }

        public RadioButtonWrapper GetRadioButton(string automationId)
        {
            AutomationElement radio = window.FindFirstWithRetries(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
            if (radio == null)
            {
                throw new Exception("RadioButton '" + automationId + "' not found");
            }
            if (!radio.Current.IsEnabled)
            {
                throw new Exception("RadioButton '" + automationId + "' is not enabled");
            }

            return new RadioButtonWrapper(radio);
        }

        public AutomationElement Expand(string name)
        {
            AutomationElement expando = window.FindFirstWithRetries(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, name));
            if (expando == null)
            {
                throw new Exception("Expander '" + name + "' not found");
            }

            for (int retries = 10; retries > 0; retries--)
            {
                ExpandCollapsePattern p = (ExpandCollapsePattern)expando.GetCurrentPattern(ExpandCollapsePattern.Pattern);
                p.Expand();

                // either WPF or Automation is broken here because sometimes it does not expand.
                Thread.Sleep(250);
                if (p.Current.ExpandCollapseState == ExpandCollapseState.Expanded)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Expander '" + name + "' failed, trying again");
                }
            }            
            return expando;
        }

        public ComboBoxWrapper GetComboBox(string name)
        {
            AutomationElement box = window.FindFirstWithRetries(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, name));
            if (box == null)
            {
                throw new Exception("ComboBox '" + name + "' not found");
            }
            if (!box.Current.IsEnabled)
            {
                throw new Exception("ComboBox '" + name + "' is not enabled");
            }

            return new ComboBoxWrapper(box);
        }

        public AutomationElement FindChildWindow(string name, int retries)
        {
            for (int i = 0; i < retries; i++)
            {
                AutomationElement childWindow = window.FindFirst(TreeScope.Descendants,
                    new AndCondition(new PropertyCondition(AutomationElement.ClassNameProperty, "Window"),
                                     new PropertyCondition(AutomationElement.NameProperty, name)));

                if (childWindow != null)
                {
                    return childWindow;
                }

                Thread.Sleep(250);

                // this is needed to pump events so we actually get the new window we're looking for
                System.Windows.Forms.Application.DoEvents();
            }

            return null;
        }

        public bool HasModalChildWindow
        {
            get
            {
                AutomationElement childWindow = window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ClassNameProperty, "Window"));
                if (childWindow != null && !childWindow.Current.IsOffscreen)
                {
                    WindowPattern wp = (WindowPattern)childWindow.GetCurrentPattern(WindowPattern.Pattern);
                    return wp.Current.IsModal;
                }
                return false;
            }
        }

        public bool IsBlocked
        {
            get
            {
                return State == WindowInteractionState.BlockedByModalWindow || HasModalChildWindow;
            }
        }

        public bool IsInteractive
        {
            get
            {

                return State == WindowInteractionState.ReadyForUserInteraction || State == WindowInteractionState.Running && !HasModalChildWindow;
            }
        }

        public bool IsNotResponding
        {
            get
            {

                return State == WindowInteractionState.NotResponding;
            }
        }

        public WindowInteractionState State
        {
            get
            {
                WindowPattern wp = (WindowPattern)window.GetCurrentPattern(WindowPattern.Pattern);
                return wp.Current.WindowInteractionState;
            }
        }

        public void WaitForInputIdle(int milliseconds)
        {
            WindowPattern wp = (WindowPattern)window.GetCurrentPattern(WindowPattern.Pattern);
            wp.WaitForInputIdle(milliseconds);
        }
    }
}
