using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clocks.Controls
{
    /// <summary>
    /// Interaction logic for TimeEntryBox.xaml
    /// </summary>
    public partial class TimeEntryBox : UserControl
    {
        static Brush CorrectBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xF0, 0xC0));
        static Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xC0, 0xC0));

        public TimeEntryBox()
        {
            InitializeComponent();
            
        }


        private void OnHourChanged(object sender, TextChangedEventArgs e)
        {
            string hour = Hour.Text;
            if (hour.Length == 2)
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
            return i;
        }
        int GetMinute()
        {
            int i = 0;
            int.TryParse(Minute.Text, out i);
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

        int GetSecond()
        {
            int i = 0;
            int.TryParse(Second.Text, out i);
            return i;
        }

        public DateTime Time
        {
            get
            {
                DateTime now = DateTime.Now;
                return new DateTime(now.Year, now.Month, now.Day, GetHour(), GetMinute(), GetSecond());
            }
        }

        public event EventHandler Completed;
    }
}
