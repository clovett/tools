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

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            UpdateReorderBackgroundBrush();

            Window.Current.Activated += OnWindowActivated;
            this.Unloaded += MainPage_Unloaded;
        }

        void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            StopTimer();
        }

        private void OnWindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            UpdateReorderBackgroundBrush();
        }        

        private void UpdateReorderBackgroundBrush()
        {
            // todo: figure out how to get the same brush used by the Speed Dial page so we don't have to do this hack.
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                JournalList.ReorderItemBackground = this.FindResource<Brush>("ListViewReorderItemBackgroundDarkTheme");
            }
            else
            {
                JournalList.ReorderItemBackground = this.FindResource<Brush>("ListViewReorderItemBackgroundLightTheme");
            }
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
                    BrowseButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
            }
            else
            {
                MessageText.Text = "Please select a location to save your journal:";
                BrowseButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private void StartTimer()
        {
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
                JournalEntry last = journal.Entries.Last();
                TimeSpan span = DateTime.Now - last.StartTime;
                last.Seconds = (int)span.TotalSeconds;
            }
            ticks++;
            if (ticks == 60)
            {
                ticks = 0;
                OnSaveFile();
            }
        }

        private void OnSaveFile()
        {
            var nowait = journal.SaveAsync();
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

            BrowseButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
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

            MessageText.Text = "";

            JournalList.ItemsSource = this.journal.Entries;

        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            this.journal.Entries.Add(new JournalEntry() { Title = "new", StartTime = DateTime.Now });

            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, new Windows.UI.Core.DispatchedHandler(() =>
            {
                JournalList.SelectedIndex = this.journal.Entries.Count - 1;
            }));
        }


        private void OnButtonDownClick(object sender, RoutedEventArgs e)
        {
            int index = JournalList.SelectedIndex;
            if (index >= 0 && index < journal.Entries.Count - 1)
            {
                FlipItems(index, index + 1);
            }
        }

        private void OnButtonUpClick(object sender, RoutedEventArgs e)
        {
            int index = JournalList.SelectedIndex;
            if (index > 0 && index < journal.Entries.Count)
            {
                FlipItems(index - 1, index);
            }
        }

        private void FlipItems(int item1Index, int item2Index)
        {
            JournalList.FlipItems(item1Index, item2Index, () => { journal.Entries.Move(item1Index, item2Index); });
        }

        private void OnItemClicked(object sender, ItemClickEventArgs e)
        {
            JournalEntry entry = e.ClickedItem as JournalEntry;
            if (entry != null)
            {
                JournalList.SelectedItem = entry;
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ButtonDelete.Visibility = (JournalList.SelectedItem == null) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnButtonDeleteClick(object sender, RoutedEventArgs e)
        {
            JournalEntry entry = JournalList.SelectedItem as JournalEntry;
            if (entry != null)
            {
                this.journal.Entries.Remove(entry);
            }
        }


    }
}
