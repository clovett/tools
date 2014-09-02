using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Microsoft.Journal.Controls
{
    public sealed partial class CalendarDayControl : UserControl
    {
        ObservableCollection<JournalEntry> entries = new ObservableCollection<JournalEntry>();
        Resizer resizer;
        double itemHeight = 60;
        bool layoutDirty;
        double availableWidth;

        public CalendarDayControl()
        {
            this.InitializeComponent();
            for (int i = 0; i < 24; i++)
            {
                this.HourStack.Children.Add(new CalendarHourControl() { DataContext = new CalendarHourModel() { Hour = i, ItemHeight = itemHeight } });
            }

            entries.CollectionChanged += OnEntriesCollectionChanged;
        }

        Ellipse thumb;

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            thumb = e.OriginalSource as Ellipse;
            if (thumb != null && resizer != null)
            {
                resizer.OnThumbPressed(thumb, e);
                this.CapturePointer(e.Pointer);
            }
            else
            {
                base.OnPointerPressed(e);
            }
        }

        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            if (thumb != null && resizer != null)
            {
                resizer.OnThumbMoved(thumb, e);
            }
            else
            {
                base.OnPointerMoved(e);
            }
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            if (thumb != null && resizer != null)
            {
                resizer.OnThumbReleased(thumb, e);
            }
            else
            {
                base.OnPointerPressed(e);
            }
            this.ReleasePointerCapture(e.Pointer);
        }

        protected override void OnPointerCanceled(PointerRoutedEventArgs e)
        {
            base.OnPointerCanceled(e);
        }

        private void OnCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            this.ReleasePointerCapture(e.Pointer);

            if (thumb != null && resizer != null)
            {
                resizer.OnThumbReleased(thumb, e);
            }
            else
            {
                base.OnPointerPressed(e);
            }
        }

        public void HighlightCurrentHour()
        {
            DateTime date = this.Date;
            if (date == DateTime.Today.Date)
            {
                int hour = DateTime.Now.Hour;
                foreach (CalendarHourControl c in this.HourStack.Children)
                {
                    CalendarHourModel model = c.DataContext as CalendarHourModel;
                    model.IsHighlighted = (model.Hour == hour);
                }
            }
        }

        void OnEntriesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            layoutDirty = true;
            InvalidateMeasure();
            InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (layoutDirty || availableWidth != availableSize.Width)
            {
                // add the journal entries to the CalendarEntryCanvas
                availableWidth = availableSize.Width;
                layoutDirty = false;
                StopResizing();
                CalendarEntryCanvas.Children.Clear();
                foreach (JournalEntry j in this.entries)
                {
                    j.PropertyChanged -= OnJournalEntryPropertyChanged;
                    j.PropertyChanged += OnJournalEntryPropertyChanged;
                    JournalEntryControl item = new JournalEntryControl();
                    item.MinHeight = 40;
                    item.PointerPressed += OnItemPointerPressed;
                    item.Selected += OnJournalItemSelected;
                    item.DataContext = j;
                    
                    CalendarEntryCanvas.Children.Add(item);
                }
                LayoutItems(availableSize.Width);
            }
            return base.MeasureOverride(availableSize);
        }
        private void LayoutItems(double availableWidth)
        {
            DateTime date = this.Date;
            foreach (UIElement child in CalendarEntryCanvas.Children)
            {
                JournalEntryControl item = child as JournalEntryControl;
                if (item != null)
                {
                    JournalEntry j = item.DataContext as JournalEntry;
                    DateTime start = j.StartTime;
                    TimeSpan duration = j.Duration;
                    if (start < date)
                    {
                        duration -= (date - start);
                        start = date;
                    }
                    item.Width = availableWidth - 60;
                    item.Height = item.FillHeight = duration.TotalHours * this.itemHeight;
                    double hours = (double)start.Hour + (double)start.Minute / 60;
                    Canvas.SetTop(item, hours * itemHeight);
                    Canvas.SetLeft(item, 61);
                }
            }
        }


        void OnJournalEntryPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LayoutItems(availableWidth);
        }

        void OnItemPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            JournalEntryControl item = sender as JournalEntryControl;
            item.IsSelected = true;
        }

        JournalEntryControl selected;

        public event EventHandler SelectionChanged;

        public JournalEntry SelectedItem
        {
            get { return selected != null ? selected.DataContext as JournalEntry : null; }
            set {
                JournalEntryControl found = null;
                foreach (UIElement child in CalendarEntryCanvas.Children)
                {
                    JournalEntryControl c = child as JournalEntryControl;
                    if (c != null && c.DataContext == value)
                    {
                        found = c;
                        break;
                    }
                }
                OnJournalItemSelected(found, EventArgs.Empty);
            }
        }

        void OnJournalItemSelected(object sender, EventArgs e)
        {
            if (selected != null && selected != sender)
            {
                selected.IsSelected = false;

                StopResizing();
            }
            selected = sender as JournalEntryControl;            
            if (selected != null)
            {
                StartResizing();
            }
            if (SelectionChanged != null)
            {
                SelectionChanged(this, EventArgs.Empty);
            }
        }

        private void StartResizing()
        {
            if (resizer == null && selected != null)
            {
                resizer = new Resizer();
                resizer.Resizing += resizer_Resizing;
                CalendarEntryCanvas.Children.Add(resizer);
            }
            if (selected != null)
            {
                selected.SizeChanged += OnItemSizeChanged;
                OnItemSizeChanged(this, null);
            }
        }

        void OnItemSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (selected != null && resizer != null)
            {
                double top = Canvas.GetTop(selected);
                double left = Canvas.GetLeft(selected);
                resizer.Width = selected.ActualWidth;
                resizer.Height = selected.ActualHeight;
                Canvas.SetTop(resizer, top);
                Canvas.SetLeft(resizer, left);
            }
        }

        public void StopResizing()
        {
            if (resizer != null)
            {
                selected.SizeChanged -= OnItemSizeChanged;
                CalendarEntryCanvas.Children.Remove(resizer);
                resizer = null;
            }
        }

        void resizer_Resizing(object sender, ResizerEventArgs e)
        {
            if (selected != null)
            {
                Rect newBounds = e.NewBounds;
                double top = newBounds.Top;
                if (top + newBounds.Height > CalendarEntryCanvas.ActualHeight)
                {
                    top = CalendarEntryCanvas.ActualHeight - newBounds.Height;
                }
                if (top < 0)
                {
                    top = 0;
                }
                Canvas.SetTop(selected, top);
                selected.Height = newBounds.Height;
                resizer.Height = newBounds.Height;
                Canvas.SetTop(resizer, top);

                JournalEntry j = selected.DataContext as JournalEntry;
                double hours = top / this.itemHeight;
                double ticks = j.StartTime.Date.Ticks + (hours * 60 * 60 * 10000); // convert to 100 nanosecond ticks
                j.StartTime = new DateTime((long)ticks);
                double seconds = (newBounds.Height / this.itemHeight) * 3600;
                j.Seconds = (int)seconds;
            }
        }

        public ObservableCollection<JournalEntry> Entries
        {
            get { return this.entries; }
        }

        public DateTime Date
        {
            get { return (DateTime)GetValue(DateProperty); }
            set { SetValue(DateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Date.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register("Date", typeof(DateTime), typeof(CalendarDayControl), new PropertyMetadata(DateTime.Today));

        
    }

    /// <summary>
    /// This is the model for items to display on the calendar.
    /// </summary>
    public class JournalEntry : INotifyPropertyChanged
    {
        private DateTime startTime;
        private int seconds;
        private string title;

        public JournalEntry() { }


        public DateTime StartTime
        {
            get { return startTime; }
            set
            {
                if (startTime != value)
                {
                    startTime = value;
                    OnPropertyChanged("StartTime");
                }
            }
        }

        public int Seconds
        {
            get { return seconds; }
            set
            {
                if (seconds != value)
                {
                    seconds = value;
                    OnPropertyChanged("Seconds");
                    OnPropertyChanged("Duration");
                }
            }
        }


        public TimeSpan Duration
        {
            get { return TimeSpan.FromSeconds(seconds); }
        }

        public string Title
        {
            get { return title; }
            set
            {
                if (title != value)
                {
                    this.title = value;
                    OnPropertyChanged("Title");
                }
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
