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
using System.Diagnostics;
using HookManager;
using System.ComponentModel;

namespace KeyboardMonkey
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DelayedActions actions = new DelayedActions();
        bool findingWindows;
        Monkey monkey;
        SystemHooks hooks = new SystemHooks();
        List<KeyboardInput> script = new List<KeyboardInput>();
        int displayPosition;
        bool recording;
        
        public MainWindow()
        {
            UiDispatcher.Initialize(this.Dispatcher);
            InitializeComponent();

            Message.Text = "";

            findingWindows = true;
            BeginFindWindow();

            hooks.HookKeyboard(KeyboardHandler);
            //hooks.HookCbt();
        }

        void KeyboardHandler(object sender, KeyboardInput input)
        {
            if (recording)
            {
                input.hwnd = this.lastWindow;
                script.Add(input);
                actions.StartDelayedAction("ShowScript", new Action(ShowScript), TimeSpan.FromMilliseconds(30));
            }
        }

        void ShowScript()
        {
            while (displayPosition < script.Count)
            {
                KeyboardInput input = script[displayPosition++];
                if (input.pressed)
                {
                    TextBoxScript.Text += Input.GetVirtualKeyString((VirtualKeyCode)input.vkCode);
                }
            }
        }

        void BeginFindWindow()
        { 
            actions.StartDelayedAction("FindWindow", new Action(FindWindow), TimeSpan.FromMilliseconds(30));
        }

        IntPtr lastWindow;

        void FindWindow()
        {
            Point pos = Input.GetMousePosition();
            SafeNativeMethods.POINT p;
            p.X = (int)pos.X;
            p.Y = (int)pos.Y;
            IntPtr hwnd = SafeNativeMethods.WindowFromPoint(p);
            if (hwnd != IntPtr.Zero)
            {
                if (hwnd != lastWindow)
                {
                    string text = SafeNativeMethods.GetWindowText(hwnd);
                    Debug.WriteLine("Mouse is over window: " + text);
                    lastWindow = hwnd;
                }
            }

            if (findingWindows)
            {
                BeginFindWindow();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            actions.CancelDelayedAction("FindWindow");
            findingWindows = false;
        }

        private void OnGoClick(object sender, RoutedEventArgs e)
        {
            ShowError("");

            StopRecording();
            if (monkey != null)
            {
                monkey.Stop();
            }

            if (ButtonPlay.Tag != null)
            {
                StopPlaying();
                ShowError("Stopped");
                return;
            }

            // todo: play the script
            string script = TextBoxScript.Text;

            ButtonPlay.Tag = "started";
            ButtonPlay.Content = "Stop";

            int delay = 30;
            int.TryParse(TextBoxSpeed.Text, out delay);
            this.monkey = new Monkey(this.script, delay);
            this.monkey.Progress += OnMonkeyProgress;
            this.monkey.Start();
        }

        private void OnMonkeyProgress(object sender, EventArgs e)
        {
            if (this.monkey != null)
            {
                if (this.monkey.Position == this.monkey.Maximum)
                {
                    Progress.Visibility = Visibility.Collapsed;
                    StopPlaying();
                }
                else
                {
                    Progress.Visibility = Visibility.Visible;
                    Progress.Maximum = this.monkey.Maximum;
                    Progress.Value = this.monkey.Position;
                }
            }
        }

        void StopPlaying()
        { 
            ShowError("Finished");
            this.monkey.Stop();
            ButtonPlay.Tag = null;
            ButtonPlay.Content = "Start";
            Progress.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string msg)
        {
            Message.Text = msg;
        }

        private void OnRecordClick(object sender, RoutedEventArgs e)
        {
            if (ButtonRecord.Tag != null)
            {
                StopRecording();
            }
            else
            {
                ButtonRecord.Tag = "recording";
                ButtonRecord.Content = "Stop";
                StartRecording();
            }
        }

        private void StopRecording()
        {
            ButtonRecord.Tag = null;
            ButtonRecord.Content = "Record";
            recording = false;
        }

        private void StartRecording()
        {
            this.script = new List<KeyboardInput>();
            this.displayPosition = 0;
            this.TextBoxScript.Text = "";
            recording = true;
        }
    }
}
