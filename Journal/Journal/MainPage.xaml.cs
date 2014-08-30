using Microsoft.Phone.BatteryStretcher.Common;
using System;
using System.Collections.Generic;
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

namespace Journal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IFilePickerContinuable
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            App app = (App)App.Current;
            Settings settings = app.Settings;

            if (settings.LogFileName != null)
            {
                DebugText.Text = Path.GetFileName(settings.LogFileName);
                StorageFile file = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(settings.LogToken);
                // load it...
            }
        }

        private void OnPressed(object sender, PointerRoutedEventArgs e)
        {

            LoadJournal();
        }
        
        private async void LoadJournal()
        {
            App app = (App)App.Current;
            Settings settings = app.Settings;

            if (settings.LogFileName == null)
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedFileName = "data";
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });

                savePicker.PickSaveFileAndContinue();               
            }
            else
            {
                
            }

            // j.Entries.Add(new JournalEntry() { StartTime = DateTime.Now, Title = "Go bowling" });
            // await store.SaveToFileAsync("Data\\Test.xml", j);
            
        }

        public void ContinueFileOpenPicker(Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs args)
        {
            
        }


        public async void ContinueFileSavePicker(Windows.ApplicationModel.Activation.FileSavePickerContinuationEventArgs args)
        {
            App app = (App)App.Current;
            Settings settings = app.Settings;

            StorageFile file = args.File;

            // Add to FutureAccessList
            string faToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);

            settings.LogFileName = file.Path;
            settings.LogToken = faToken;
            await settings.SaveAsync();

            // j.Entries.Add(new JournalEntry() { StartTime = DateTime.Now, Title = "Go bowling" });
            // await store.SaveToFileAsync("Data\\Test.xml", j);
        }

    }
}
