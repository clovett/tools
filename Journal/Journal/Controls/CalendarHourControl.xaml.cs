using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Microsoft.Journal.Controls
{
    public sealed partial class CalendarHourControl : UserControl
    {

        public CalendarHourControl()
        {
            this.InitializeComponent();
        }

        public int Hour
        {
            get { return (int)GetValue(HourProperty); }
            set { SetValue(HourProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Hour.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HourProperty =
            DependencyProperty.Register("Hour", typeof(int), typeof(CalendarHourControl), new PropertyMetadata(0));

    }

    /// <summary>
    /// This is the model for the CalendarHourControl, it should be set as DataContext.
    /// </summary>
    public class CalendarHourModel : INotifyPropertyChanged
    {
        private double height;
        private int hour;
        private bool isHighlighted;

        public int Hour
        {
            get { return hour; }
            set
            {
                if (hour != value)
                {
                    hour = value;
                    OnPropertyChanged("Hour");
                }
            }
        }

        public double ItemHeight
        {
            get { return height; }
            set
            {
                if (height != value)
                {
                    height = value;
                    OnPropertyChanged("ItemHeight");
                };
            }

        }
        public bool IsHighlighted 
        {
            get { return isHighlighted; }
            set
            {
                if (isHighlighted != value)
                {
                    isHighlighted = value;
                    OnPropertyChanged("IsHighlighted");
                };
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

    }

}
