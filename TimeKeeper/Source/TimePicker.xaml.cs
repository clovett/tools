//-----------------------------------------------------------------------
// <copyright file="TimePicker.cs" company="Lovett Software">
//   (c) Lovett Software.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TimeKeeper
{
    /// <summary>
    /// Interaction logic for TimePicker.xaml, this control can edit a time of day or a time span.
    /// The difference is the time span doesn't have "am/pm".
    /// </summary>
    public partial class TimePicker : UserControl
    {
        bool editTimeSpan;

        public TimePicker()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(TimePicker_Loaded);
        }

        static string LastFocus = "hourText";

        void TimePicker_Loaded(object sender, RoutedEventArgs e)
        {
            Point pos = Mouse.GetPosition(this);
            HitTestResult r = VisualTreeHelper.HitTest(this, pos);
            if (r != null)
            {
                DependencyObject d = r.VisualHit;
                while (d != null)
                {
                    TextBox t = d as TextBox;
                    if (t != null)
                    {

                        t.Focus();
                        t.SelectAll();
                        break;
                    }
                    d = VisualTreeHelper.GetParent(d);
                }
            }
            else
            {
                TextBox t = this.FindName(LastFocus) as TextBox;
                if (t != null)
                {

                    t.Focus();
                    t.SelectAll();
                }
            }
        }

        public bool EditTimeSpan
        {
            get { return editTimeSpan; }
            set { 
                editTimeSpan = value;
                am.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        

        public DateTime TimeOfDay
        {
            get { return (DateTime)GetValue(TimeOfDayProperty); }
            set { SetValue(TimeOfDayProperty, value); } 
        }

        public static readonly DependencyProperty TimeOfDayProperty = DependencyProperty.Register("TimeOfDay", typeof(DateTime), typeof(TimePicker), new UIPropertyMetadata(DateTime.Now, new PropertyChangedCallback(OnTimeOfDayChanged)));

        private static void OnTimeOfDayChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            DateTime time = (DateTime)e.NewValue;
            TimePicker control = obj as TimePicker;
            control.ConvertToAmPm(time.Hour);
            control.Minutes = time.Minute;
            control.Seconds = time.Second;
        }

        public TimeSpan TimeSpan
        {
            get { return (TimeSpan)GetValue(TimeSpanProperty); }
            set { SetValue(TimeSpanProperty, value); }
        }

        public static readonly DependencyProperty TimeSpanProperty = DependencyProperty.Register("TimeSpan", typeof(TimeSpan), typeof(TimePicker), new UIPropertyMetadata(TimeSpan.MinValue, new PropertyChangedCallback(OnTimeSpanChanged)));

        private static void OnTimeSpanChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TimeSpan time = (TimeSpan)e.NewValue;
            TimePicker control = obj as TimePicker;
            control.Hours = time.Hours;
            control.Minutes = time.Minutes;
            control.Seconds = time.Seconds;
        }

        void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            LastFocus = box.Name;
            box.SelectAll();
        }

        void ConvertToAmPm(int hours)
        {
            if (hours == 0 || hours == 24)
            {
                this.Am = true;
                this.Hours = 12; // midnight.
            } 
            else if (hours < 12)
            {
                this.Am = true;
                this.Hours = hours;
            }
            else if (hours > 12)
            {
                this.Am = false;
                this.Hours = hours - 12;
            }
            else
            {
                this.Am = false; // 12 PM
                this.Hours = hours;
            }
        }
        int Get24Hour()
        {
            if (this.Am)
            {
                if (this.Hours == 12) return 0;
                return this.Hours;
            }
            else
            {
                if (this.Hours == 12) return 12;
                return this.Hours + 12;
            }
        }
        
        public int Hours { 
            get { return (int)GetValue(HoursProperty); } 
            set { SetValue(HoursProperty, value); } 
        }        
        
        public static readonly DependencyProperty HoursProperty = DependencyProperty.Register("Hours", typeof(int), typeof(TimePicker), new UIPropertyMetadata(0, new PropertyChangedCallback(OnTimeChanged))); 
        
        public int Minutes {
            get { return (int)GetValue(MinutesProperty); } 
            set { SetValue(MinutesProperty, value); } 
        }      
        
        public static readonly DependencyProperty MinutesProperty = DependencyProperty.Register("Minutes", typeof(int), typeof(TimePicker), new UIPropertyMetadata(0, new PropertyChangedCallback(OnTimeChanged))); 
        
        public int Seconds { 
            get { return (int)GetValue(SecondsProperty); } 
            set { SetValue(SecondsProperty, value); } 
        }       
        
        public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register("Seconds", typeof(int), typeof(TimePicker), new UIPropertyMetadata(0, new PropertyChangedCallback(OnTimeChanged)));

        public bool Am
        {
            get { return (bool)GetValue(AmProperty); }
            set { SetValue(AmProperty, value); }
        }

        public static readonly DependencyProperty AmProperty = DependencyProperty.Register("Am", typeof(bool), typeof(TimePicker), new UIPropertyMetadata(false, new PropertyChangedCallback(OnTimeChanged))); 
        
        private static void OnTimeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TimePicker control = obj as TimePicker;
            if (control.editTimeSpan)
            {
                control.TimeSpan = new TimeSpan(control.Hours, control.Minutes, control.Seconds);
            }
            else
            {
                DateTime today = DateTime.Today;
                int hour = control.Get24Hour();
                int min = control.Minutes;
                int sec = control.Seconds;
                control.TimeOfDay = new DateTime(today.Year, today.Month, today.Day, hour, min, sec);
            }
        }
        
        private void Down(object sender, KeyEventArgs args) 
        {
            switch (((Grid)sender).Name) { 
                case "sec":
                    if (args.Key == Key.Up)
                    {
                        if (this.Seconds < 59)
                        {
                            this.Seconds++;
                        }
                        else
                        {
                            this.Seconds = 0;
                            goto case "min";
                        }
                        args.Handled = true;
                    }
                    if (args.Key == Key.Down)
                    {
                        if (this.Seconds > 0)
                        {
                            this.Seconds--;
                        }
                        else
                        {
                            this.Seconds = 59;
                            goto case "min";
                        }
                        args.Handled = true;
                    }
                    break; 

                case "min":
                    if (args.Key == Key.Up)
                    {
                        if (this.Minutes < 59)
                        {
                            this.Minutes++;
                        }
                        else
                        {
                            this.Minutes = 0;
                            goto case "hour";
                        }
                        args.Handled = true;
                    }
                    if (args.Key == Key.Down) 
                    {
                        if (this.Minutes > 0)
                        {
                            this.Minutes--;
                        }
                        else
                        {
                            this.Minutes = 59;
                            goto case "hour";
                        }
                        args.Handled = true;
                    }
                    break; 
                case "hour":      
                    if (args.Key == Key.Up)     {
                        if (this.Hours < 12 || this.EditTimeSpan)
                        {
                            this.Hours++;
                        }
                        else
                        {
                            this.Hours = 1;
                        }
                        if (this.Hours == 12 && ! this.EditTimeSpan)
                        {
                            this.Am = !this.Am;
                        }
                        args.Handled = true;
                    }
                    if (args.Key == Key.Down) 
                    {
                        if (this.Hours == 1 && !this.EditTimeSpan)
                        {
                            this.Hours = 12;
                        }
                        else if (this.Hours > 0)
                        {
                            this.Hours--;
                        }
                        if (this.Hours == 12 && this.EditTimeSpan)
                        {
                            this.Am = !this.Am;
                        }
                        args.Handled = true;
                    }
                    break;
                case "am":
                    if (args.Key == Key.Up || args.Key == Key.Down)
                    {
                        this.Am = !this.Am;
                        args.Handled = true;
                    }
                    break;
            } 
        }
    }

    public class AmConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? "AM" : "PM";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString().Trim() == "AM" ? true : false;
        }
    }


    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan t = (TimeSpan)value;
            return string.Format("{0}:{1}:{2}", TwoDigits(t.Hours), TwoDigits(t.Minutes), TwoDigits(t.Seconds));
        }

        string TwoDigits(int i)
        {
            string s = i.ToString();
            if (s.Length < 2) s = "0" + s;
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString();
        }
    }


    public class TwoDigitsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int i = 0;

            switch (parameter as string)
            {
                case "h":
                    i = ((TimeSpan)value).Hours;
                    break;
                case "m":
                    i = ((TimeSpan)value).Minutes;
                    break;
                case "s":
                    i = ((TimeSpan)value).Seconds;
                    break;
                default:
                    i = (int)value;
                    break;
            }
            string s = i.ToString();
            if (s.Length < 2) s = "0" + s;
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return int.Parse(value.ToString());
            }
            catch
            {
                return 0;
            }
        }
    }
}
