using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Walkabout.Utilities;
using System.Reflection;

namespace KeyboardMonkey
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DelayedActions actions = new DelayedActions();
        bool findingWindows;
        IntPtr hwnd;
        Monkey monkey;

        public MainWindow()
        {
            UiDispatcher.Initialize(this.Dispatcher);
            InitializeComponent();

            Message.Text = "";

            foreach (System.Reflection.FieldInfo fi in typeof(Key).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                Key value = (Key)fi.GetValue(null);
                ComboBoxKey.Items.Add(value);
            }
            ComboBoxKey.SelectedIndex = 0;
        }

        private void OnTextBoxWindowGotFocus(object sender, RoutedEventArgs e)
        {
            findingWindows = true;
            BeginFindWindow();
        }

        void BeginFindWindow()
        { 
            actions.StartDelayedAction("FindWindow", new Action(FindWindow), TimeSpan.FromMilliseconds(30));
        }

        void FindWindow()
        {
            Point pos = Input.GetMousePosition();
            SafeNativeMethods.POINT p;
            p.X = (int)pos.X;
            p.Y = (int)pos.Y;
            IntPtr hwnd = SafeNativeMethods.WindowFromPoint(p);
            if (hwnd != IntPtr.Zero)
            {
                string text = SafeNativeMethods.GetWindowText(hwnd);
                TextBoxWindow.Text = text + " (0x" + hwnd.ToInt64().ToString("x").Trim('0') + ")";
                this.hwnd = hwnd;
            }

            if (findingWindows)
            {
                BeginFindWindow();
            }
        }

        private void OnTextBoxWindowLostFocus(object sender, RoutedEventArgs e)
        {
            actions.CancelDelayedAction("FindWindow");
            findingWindows = false;
        }

        private void OnGoClick(object sender, RoutedEventArgs e)
        {
            ShowError("");

            if (monkey != null)
            {
                monkey.Stop();
            }

            if (ButtonStart.Tag != null)
            {
                Stop();
                ShowError("Stopped");
                return;
            }

            int ms = 0;
            if (!int.TryParse(TextBoxDelay.Text, out ms) || ms <= 0)
            {
                ShowError("Please enter valid number of milliseonds (greater than zero)");
                TextBoxDelay.SelectAll();
                TextBoxDelay.Focus();
                return;
            }


            int repeat = 0;
            if (!int.TryParse(TextBoxRepeat.Text, out repeat) || repeat <= 0)
            {
                ShowError("Please enter valid number of times to type the key  (greater than zero)");
                TextBoxRepeat.SelectAll();
                TextBoxRepeat.Focus();
                return;
            }

            Key key = (Key)ComboBoxKey.SelectedItem;
            if (key == Key.None)
            {
                ShowError("Please enter valid key to type (not none)");
                ComboBoxKey.Focus();
                return;
            }

            if (hwnd == IntPtr.Zero)
            {
                ShowError("Please move mouse over the window you want to type in");
                TextBoxWindow.Focus();
                return;
            }

            ButtonStart.Tag = "started";
            ButtonStart.Content = "Stop";

            this.monkey = new Monkey(this.actions, this.hwnd, key, repeat, ms);
            this.monkey.Progress += OnMonkeyProgress; 
        }

        private void OnMonkeyProgress(object sender, EventArgs e)
        {
            if (this.monkey != null)
            {
                if (this.monkey.Remanining == 0)
                {
                    Stop();
                }
                else
                {
                    ShowError(this.monkey.Remanining.ToString());
                }
            }
        }

        void Stop()
        { 
            if (ButtonStart.Tag != null)
            {
                ButtonStart.Tag = null;
                ButtonStart.Content = "Start";
                ShowError("Finished");
            }
            this.monkey.Stop();
        }

        private void ShowError(string msg)
        {
            Message.Text = msg;
        }

        private void OnComboKeyDown(object sender, KeyEventArgs e)
        {
            Key k = e.Key;
            ComboBoxKey.SelectedItem = k;
        }
        
    }
}
