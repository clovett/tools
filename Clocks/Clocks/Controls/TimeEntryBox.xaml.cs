using Clocks.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Clocks.Controls
{
    /// <summary>
    /// Interaction logic for TimeEntryBox.xaml
    /// </summary>
    public partial class TimeEntryBox : UserControl
    {
        static Brush CorrectBrush = new SolidColorBrush(Color.FromRgb(0x80, 0xe0, 0x80));
        static Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xC0, 0xC0));

        public TimeEntryBox()
        {
            InitializeComponent();
        }


        private void OnHourChanged(object sender, TextChangedEventArgs e)
        {
            string hour = Hour.Text;
            int h;
            int.TryParse(hour, out h);
            if (hour.Length == 2 || h > 1)
            {
                Minute.Focus();
                Minute.SelectAll();
            }
        }

        private void OnMinuteChanged(object sender, TextChangedEventArgs e)
        {
            string minute = Minute.Text;
            if (minute.Length == 2)
            {
                Second.Focus();
                Second.SelectAll();
            }
        }

        private void OnSecondChanged(object sender, TextChangedEventArgs e)
        {
            string second = Second.Text;
            if (second.Length == 2)
            {
                if (Completed != null)
                {
                    Completed(this, EventArgs.Empty);
                }
            }
        }

        internal void Reset()
        {
            Hour.Text = Minute.Text = Second.Text = "";
            Hour.Focus();

            Hour.SetValue(TextBox.BackgroundProperty, DependencyProperty.UnsetValue);
            Minute.SetValue(TextBox.BackgroundProperty, DependencyProperty.UnsetValue);
            Second.SetValue(TextBox.BackgroundProperty, DependencyProperty.UnsetValue);

            HourPopup.IsOpen = false;
            MinutePopup.IsOpen = false;
            SecondPopup.IsOpen = false;
        }

        int GetHour()
        {
            int i = 0;
            int.TryParse(Hour.Text, out i);
            if (i == 0)
            {
                i = 12;
            }
            return i;
        }
        int GetMinute()
        {
            int i = 0;
            int.TryParse(Minute.Text, out i);
            return i;
        }

        int GetSecond()
        {
            int i = 0;
            int.TryParse(Second.Text, out i);
            return i;
        }

        internal void Correct()
        {
            Hour.Background = Minute.Background = Second.Background = CorrectBrush;
        }

        internal string TwoDigit(int i)
        {
            string s = i.ToString();
            if (s.Length == 1)
            {
                s = "0" + s;
            }
            return s;
        }

        internal void ShowError(DateTime answer)
        {
            if (answer.Hour != GetHour())
            {
                HourPopupText.Text = TwoDigit(answer.Hour);
                HourPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                HourPopup.PlacementTarget = Hour;
                HourPopup.IsOpen = true;
            }
            if (answer.Minute != GetMinute())
            {
                MinutePopupText.Text = TwoDigit(answer.Minute);
                MinutePopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                MinutePopup.PlacementTarget = Minute;
                MinutePopup.IsOpen = true;
            }
            if (answer.Second != GetSecond())
            {
                SecondPopupText.Text = TwoDigit(answer.Second);
                SecondPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                SecondPopup.PlacementTarget = Second;
                SecondPopup.IsOpen = true;
            }

        }

        public DateTime? Time
        {
            get
            {
                DateTime now = DateTime.Now;
                int h = GetHour();
                int m = GetMinute();
                int s = GetSecond();
                if (h <= 12 && h > 0 && m >= 0 && m < 60 && s >= 0 && s < 60)
                {
                    return new DateTime(now.Year, now.Month, now.Day, h, m, s);
                }
                return null;
            }
        }

        public event EventHandler Completed;

        internal void HidePopups()
        {
            HourPopup.IsOpen = false;
            MinutePopup.IsOpen = false;
            SecondPopup.IsOpen = false;
        }

        private void OnTextKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (e.Key == System.Windows.Input.Key.Back)
            {
                if (box.Text == "")
                {
                    delayedActions.StartDelayedAction("Back", () =>
                    {
                        OnPrevious(box);
                    }, TimeSpan.FromMilliseconds(20));
                }
            }
            else if (e.Key == System.Windows.Input.Key.Left)
            {
                if (box.SelectionStart == 0)
                {
                    delayedActions.StartDelayedAction("Previous", () =>
                    {
                        OnPrevious(box);
                    }, TimeSpan.FromMilliseconds(20));
                }
            }
            else if (e.Key == System.Windows.Input.Key.Right )
            {
                if (box.SelectionStart == box.Text.Length)
                {
                    delayedActions.StartDelayedAction("Next", () =>
                    {
                        OnNext(box);
                    }, TimeSpan.FromMilliseconds(20));
                }
            }
            else if (e.Key == System.Windows.Input.Key.Oem1 && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                e.Handled = true;
                if (box.SelectionStart == box.Text.Length)
                {
                    delayedActions.StartDelayedAction("Next", () =>
                    {
                        OnNext(box);
                    }, TimeSpan.FromMilliseconds(20));
                }
            }
            else if (e.Key == System.Windows.Input.Key.Add)
            {
                e.Handled = true;
                if (box.SelectionStart == box.Text.Length)
                {
                    delayedActions.StartDelayedAction("Next", () =>
                    {
                        OnNext(box);
                    }, TimeSpan.FromMilliseconds(20));
                }
            }
        }

        private void OnPrevious(TextBox box)
        {
            if (box == Second)
            {
                Minute.SelectAll();
                Minute.Focus();
            }
            else if (box == Minute)
            {
                Hour.SelectAll();
                Hour.Focus();
            }
        }

        private void OnNext(TextBox box)
        {
            if (box == Minute)
            {
                Second.SelectAll();
                Second.Focus();
            }
            else if (box == Hour)
            {
                Minute.SelectAll();
                Minute.Focus();
            }
        }

        DelayedActions delayedActions = new DelayedActions();
    }
}
