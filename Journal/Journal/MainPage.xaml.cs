using Microsoft.Journal.Common;
using Microsoft.Journal.Controls;
using Microsoft.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Microsoft.Journal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IFileSavePickerContinuable
    {
        Journal journal;
        DispatcherTimer timer;
        int ticks;
        bool loaded;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            Window.Current.Activated += OnWindowActivated;
            Window.Current.VisibilityChanged += OnWindowVisibilityChanged;
            this.Unloaded += MainPage_Unloaded;

            this.Loaded += MainPage_Loaded;

            OnDayChanged();
        }

        private void OnWindowVisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            if (!e.Visible)
            {
                OnSaveFile();
            }
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.loaded = true;
        }

        void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.Activated -= OnWindowActivated;
            Window.Current.VisibilityChanged -= OnWindowVisibilityChanged;
            StopTimer();
        }

        private void OnWindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            StartTimer();
            App app = (App)App.Current;
            Settings settings = app.Settings;

            if (!string.IsNullOrEmpty(settings.LogToken))
            {
                try
                {
                    StorageFile file = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(settings.LogToken);

                    LoadJournal(file);
                } 
                catch (Exception ex)
                {
                    Debug.WriteLine("token is stale? " + ex.Message);

                    MessageText.Text = "Journal file is gone, please select a new location to save your journal:";
                    SetupPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    DayPivot.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    DateText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            else
            {
                MessageText.Text = "Please select a location to save your journal:";
                SetupPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                DayPivot.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                DateText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        DateTime previousTime;

        private void StartTimer()
        {
            previousTime = DateTime.Now;
            StopTimer();
            timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTimerTick;
            timer.Start();
        }

        private void StopTimer()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= OnTimerTick;
                timer = null;
            }
        }

        void OnTimerTick(object sender, object e)
        {
            if (journal != null && journal.Entries.Count > 0)
            {
                if (previousTime.Date != DateTime.Now.Date)
                {
                    previousTime = DateTime.Now;
                    OnDayChanged();
                }

                JournalEntry last = journal.Entries.LastOrDefault();
                if (last != null)
                {
                    TimeSpan span = DateTime.Now - last.StartTime;
                    last.Seconds = (int)span.TotalSeconds;
                }
                CalendarToday.HighlightCurrentHour();
            }
            ticks++;
            if (ticks == 60)
            {
                ticks = 0;
                OnSaveFile();
            }
        }

        private void OnDayChanged()
        {
            CalendarToday.Date = DateTime.Today;
            CalendarYesterday.Date = DateTime.Today.AddDays(-1);
            CalendarTomorrow.Date = DateTime.Today.AddDays(1);

            DateTime date = CalendarYesterday.Date;
            int first = DayPivot.Items.IndexOf(PivotYesterday);
            while (first > 0)
            {
                first--;
                PivotItem item = (PivotItem)DayPivot.Items[first];
                CalendarDayControl c = item.Content as CalendarDayControl;
                date = c.Date = date.AddDays(-1);               
            }

            date = CalendarTomorrow.Date;
            int last = DayPivot.Items.IndexOf(PivotTomorrow);
            if (last >= DayPivot.Items.Count - 1)
            {
                AddNewItem(DayPivot.Items.Count, date.AddDays(1));
            }
            else
            {
                for (int i = last + 1, n = DayPivot.Items.Count; i < n; i++)
                {
                    PivotItem item = (PivotItem)DayPivot.Items[i];
                    CalendarDayControl c = item.Content as CalendarDayControl;
                    date = c.Date = date.AddDays(1);
                }
            }
            if (journal != null)
            {
                BindJournal();
            }
        }

        private void OnSaveFile()
        {
            if (journal != null)
            {
                var nowait = journal.SaveAsync();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StopTimer();
            Window.Current.Activated -= OnWindowActivated;
            base.OnNavigatedFrom(e);
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedFileName = "journal";
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("XML Files", new List<string>() { ".xml" });
            savePicker.PickSaveFileAndContinue();     // we get called back on ContinueFileSavePicker below.          
        }

        public async void ContinueFileSavePicker(Windows.ApplicationModel.Activation.FileSavePickerContinuationEventArgs args)
        {
            App app = (App)App.Current;
            Settings settings = app.Settings;

            StorageFile file = args.File;

            // Add to FutureAccessList
            string faToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);

            // save token so we can use it again sometime.
            settings.LogFileName = file.Path;
            settings.LogToken = faToken;
            await settings.SaveAsync();

            LoadJournal(file);

            SetupPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            DayPivot.Visibility = Windows.UI.Xaml.Visibility.Visible;
            DateText.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private async void LoadJournal(StorageFile file)
        {
            MessageText.Text = "Loading " + file.Path;

            this.journal = await Journal.LoadAsync(file);
            if (this.journal.Entries.Count == 0)
            {
                OnAddClick(this, null);
                OnSaveFile();
            }

            BindJournal();

            MessageText.Text = "";
        }

        private void BindJournal()
        {

            foreach (PivotItem item in this.DayPivot.Items)
            {
                CalendarDayControl content = item.Content as CalendarDayControl;
                PopulateJournalEntries(content);
            }
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            PivotItem selectedPivot = this.DayPivot.SelectedItem as PivotItem;
            CalendarDayControl content = selectedPivot.Content as CalendarDayControl;
            DateTime date = content.Date;
            if (date == DateTime.Today)
            {
                this.journal.Entries.Add(new JournalEntry() { Title = "new", StartTime = DateTime.Now });
            }
            else
            {
                // user is back-filling, find late time on this day
                JournalEntry last = content.Entries.LastOrDefault();
                if (last == null)
                {
                    this.journal.Entries.Add(new JournalEntry() { Title = "new", StartTime = date });
                }
                else if (last.StartTime + TimeSpan.FromSeconds(last.Seconds) < date.AddMinutes((24 * 60) - 1)) 
                {
                    this.journal.Entries.Add(new JournalEntry() { Title = "new", StartTime = last.StartTime + TimeSpan.FromSeconds(last.Seconds) });
                }
            }
            PopulateJournalEntries(content);
        }

        private void OnItemClicked(object sender, ItemClickEventArgs e)
        {
            //JournalEntry entry = e.ClickedItem as JournalEntry;
            //if (entry != null)
            //{
            //    JournalList.SelectedItem = entry;
            //}
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            CalendarDayControl calendar = sender as CalendarDayControl;
            Visibility visibleWhenHaveSelection = (calendar.SelectedItem == null) ? Visibility.Collapsed : Visibility.Visible;
            ButtonDelete.Visibility = visibleWhenHaveSelection;
        }

        private void OnButtonDeleteClick(object sender, RoutedEventArgs e)
        {
            var pivotItem = DayPivot.SelectedItem as PivotItem;
            if (pivotItem != null)
            {
                CalendarDayControl calendar = pivotItem.Content as CalendarDayControl;
                JournalEntry entry = calendar.SelectedItem as JournalEntry;
                if (entry != null)
                {
                    this.journal.Entries.Remove(entry);
                    calendar.Entries.Remove(entry);
                }
            }
        }

        private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PivotItem selected = DayPivot.SelectedItem as PivotItem;
            CalendarDayControl selectedDay = selected.Content as CalendarDayControl;
            DateText.Text = selectedDay.Date.ToString("D").ToUpper();

            if (this.loaded)
            {
                if (DayPivot.SelectedIndex == 0)
                {
                    // need a new first pivot
                    var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                    {
                        DateTime previous = selectedDay.Date.AddDays(-1);
                        AddNewItem(0, previous);
                    }));
                }
                else if (DayPivot.SelectedIndex == DayPivot.Items.Count - 1)
                {
                    // need a new last pivot
                    var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                    {
                        AddNewItem(DayPivot.Items.Count, selectedDay.Date.AddDays(1));
                    }));
                }
            }
        }

        private void AddNewItem(int index, DateTime date)
        {
            PivotItem newItem = new PivotItem() { Header = date.ToString("dddd").ToLower() };
            var control = new CalendarDayControl() { Date = date };
            control.SelectionChanged += OnSelectionChanged;
            newItem.Content = control;
            DayPivot.Items.Insert(index, newItem);
            if (index == 0)
            {
                DayPivot.SelectedIndex = 1;
            }
            PopulateJournalEntries(control);
        }

        private void PopulateJournalEntries(CalendarDayControl content)
        {
            if (this.journal != null)
            {
                content.Entries.Clear();
                DateTime date = content.Date.Date;
                for (int i = 0; i < this.journal.Entries.Count; i++)
                {
                    JournalEntry e = this.journal.Entries[i];
                    DateTime endTime = e.StartTime + e.Duration;
                    if (e.StartTime.Date == date)
                    {
                        if (endTime.Date > date)
                        {
                            // wraps around, let's split it into two entries.
                            DateTime splitStart = endTime.Date;
                            JournalEntry split = new JournalEntry() { StartTime = splitStart, Title = e.Title, Seconds = (int)(endTime - splitStart).TotalSeconds };
                            this.journal.Entries.Insert(i + 1, split);
                            e.Seconds = (int)(splitStart - e.StartTime).TotalSeconds;
                        }
                        content.Entries.Add(e);
                    }
                }
            }
        }

    }
}
