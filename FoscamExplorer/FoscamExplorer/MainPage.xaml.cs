using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FoscamExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DataStore store;

        public MainPage()
        {
            App.BaseUri = this.BaseUri;

            this.InitializeComponent();

            this.store = DataStore.Instance;
            CameraGrid.ItemsSource = store.Cameras;

            store.Cameras.CollectionChanged += Cameras_CollectionChanged;
        }

        void Cameras_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var quiet = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                {                    
                    Prompt.Text = store.Cameras.Count == 0  ? "Searching..." : "";
                }));
        }


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (store.Cameras.Count == 0)
            {
                Prompt.Text = "Searching...";
            }
            FoscamDevice.DeviceAvailable += OnDeviceAvailable;
            FoscamDevice.FindDevices();
        }

        async void OnDeviceAvailable(object sender, FoscamDevice e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                store.MergeNewCamera(e.CameraInfo);
            }));

            DelaySave(TimeSpan.FromMilliseconds(500));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        void OnDeviceError(object sender, ErrorEventArgs e)
        {
            Debug.WriteLine(e.Message);
        }

        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            var info = e.ClickedItem as CameraInfo;
            if (info.Unauthorized)
            {
                LogonPage login = new LogonPage() { UserName = info.UserName, Password = info.Password };
                login.Flyout(new Action(() =>
                {
                    info.UserName = login.UserName;
                    info.Password = login.Password;
                    DelaySave(TimeSpan.FromMilliseconds(500));
                }));
            }
            else
            {
                this.Frame.Navigate(typeof(FoscamDetailsPage), info);
            }
        }


        DispatcherTimer delaySaveTimer;

        void DelaySave(TimeSpan delay)
        {
            if (delaySaveTimer == null)
            {
                var quiet = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                {
                    delaySaveTimer = new DispatcherTimer();
                    delaySaveTimer.Interval = delay;
                    delaySaveTimer.Tick += new EventHandler<object>((s, e) =>
                    {
                        delaySaveTimer.Stop();
                        Save();
                        delaySaveTimer = null;
                    });
                    delaySaveTimer.Start();

                }));
            }
            else
            {                
                delaySaveTimer.Start();        
            }
        }


        void Save()
        {
            var result = DataStore.Instance.SaveAsync(((FoscamExplorer.App)App.Current).CacheFolder); ;
        }

    }
}
