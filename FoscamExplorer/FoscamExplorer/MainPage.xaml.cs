using FoscamExplorer.Foscam;
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
    public sealed partial class MainPage : Page, ISuspendable
    {
        DataStore store;

        public MainPage()
        {
            App.BaseUri = this.BaseUri;

            this.InitializeComponent();

            this.store = DataStore.Instance;
            CameraGrid.ItemsSource = store.Cameras;

            DeleteButton.Visibility = Visibility.Collapsed;

            this.SizeChanged += OnSizeChanged;
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.WriteLine("Size changed: " + e.NewSize);
        }

        private void OnCameraPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "LastPingTime" && e.PropertyName != "LastFrameTime")
            {
                Save();
            }
        }

        void Cameras_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var quiet = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                {                    
                    Prompt.Text = store.Cameras.Count == 0  ? "Searching..." : "";
                    Save();
                }));
        }

        DispatcherTimer searchTimer;

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.WriteLine("MainPage OnNavigatedTo");

            StartSearch();
        }

        void StartSearch()
        {
            if (store.Cameras.Count == 0)
            {
                Prompt.Text = StringResources.SearchingPrompt;
                searchTimer = new DispatcherTimer();
                searchTimer.Interval = TimeSpan.FromSeconds(10);
                searchTimer.Tick += OnSearchTick;
                searchTimer.Start();
            }
            else
            {
                foreach (var cam in store.Cameras)
                {
                    cam.PropertyChanged += OnCameraPropertyChanged;
                }

                store.Cameras.CollectionChanged += Cameras_CollectionChanged;
            }

            try
            {
                FoscamDevice.StartFindDevices();
                FoscamDevice.DeviceAvailable -= OnDeviceAvailable;
                FoscamDevice.DeviceAvailable += OnDeviceAvailable;
            }
            catch (Exception ex)
            {
                Prompt.Text = "Error: " + ex.Message;
            }


        }

        private void OnSearchTick(object sender, object e)
        {
            if (searchTimer != null)
            {
                searchTimer.Tick -= OnSearchTick;
                searchTimer = null;
            }

            // show a UI for buying foscam cameras :-) 
            if (store.Cameras.Count == 0)
            {
                Prompt.Text = "";
                CameraInfo ad = new CameraInfo();
                ad.Name = StringResources.NoCameraName;
                ad.StaticImageUrl = "ms-appx:/Assets/fi9821w_0_black.png";
                ad.StaticError = StringResources.NoCameraMessage;
                store.MergeNewCamera(ad);
            }
        }

        async void OnDeviceAvailable(object sender, FoscamDevice e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                SetupNewCamera(e);
            }));
        }

        private async void SetupNewCamera(FoscamDevice device)
        {
            var newCam = store.MergeNewCamera(device.CameraInfo);
            
            if (newCam.IsNew || newCam.LastPingTime + 10000 < Environment.TickCount)
            {
                // get the device properties
                device = new FoscamDevice() { CameraInfo = newCam };
                var properties = await device.GetStatus();

                var realName = properties.GetValue<string>("alias");
                if (!string.IsNullOrEmpty(realName))
                {
                    newCam.Name = realName;
                }

                var sysver = properties.GetValue<string>("sys_ver");
                if (!string.IsNullOrEmpty(sysver))
                {
                    newCam.SystemVersion = sysver;
                }

                var webver = properties.GetValue<string>("app_ver");
                if (!string.IsNullOrEmpty(webver))
                {
                    newCam.WebUiVersion = webver;
                }

                CheckFirmwareVersion(device);
            }
            
            Prompt.Text = "";
            newCam.LastPingTime = Environment.TickCount;
            newCam.Rebooting = false;
            newCam.UpdatingFirmware = false;
            newCam.PropertyChanged -= OnCameraPropertyChanged;
            newCam.PropertyChanged += OnCameraPropertyChanged;
        }

        private void CheckFirmwareVersion(FoscamDevice device)
        {
            App app = (App)(App.Current);
            var info = device.CameraInfo;
            var update = app.Firmware.FindUpdate(info.SystemVersion);
            if (update != null)
            {
                info.UpdateAvailable = update;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Disconnect();
            base.OnNavigatedFrom(e);
        }

        internal void Disconnect()
        {
            Log.WriteLine("MainPage disconnecting");

            FoscamDevice.StopFindingDevices();
            FoscamDevice.DeviceAvailable -= OnDeviceAvailable;

            foreach (var cam in store.Cameras)
            {
                cam.PropertyChanged -= OnCameraPropertyChanged;
            }

            store.Cameras.CollectionChanged -= Cameras_CollectionChanged;

            CameraGrid.ItemsSource = null;
        }

        internal void Reconnect()
        {
            Log.WriteLine("MainPage reconnecting");

            this.store = DataStore.Instance;
            CameraGrid.ItemsSource = store.Cameras;
            StartSearch();
        }

        void OnDeviceError(object sender, ErrorEventArgs e)
        {
            Log.WriteLine("DeviceError: " + e.Message);
        }       

        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            var info = e.ClickedItem as CameraInfo;
            if (info.Unauthorized)
            {
                LogonPage login = new LogonPage() { UserName = info.UserName, Password = info.Password };
                if (store.Cameras.Count > 1)
                {
                    login.CheckboxVisibility = Windows.UI.Xaml.Visibility.Visible;
                }
                login.Flyout(new Action(() =>
                {
                    if (!login.Cancelled)
                    {
                        info.UserName = login.UserName;
                        info.Password = login.Password;
                        if (login.CheckBoxAllIsChecked)
                        {
                            PropagateLogonToAllCameras(info);
                        }
                        Save();
                    }
                }));
            }
            else
            {
                this.Frame.Navigate(typeof(FoscamDetailsPage), info);
            }
        }

        private void PropagateLogonToAllCameras(CameraInfo info)
        {
            foreach (CameraInfo i in this.store.Cameras)
            {
                if (i != info )
                {
                    i.UserName = info.UserName;
                    i.Password = info.Password;
                }
            }
        }

        void Save()
        {
            DataStore.Instance.SaveAsync(((FoscamExplorer.App)App.Current).CacheFolder);
        }

        private void OnDeleteSelection(object sender, RoutedEventArgs e)
        {
            CameraInfo cameraToDelete = CameraGrid.SelectedItem as CameraInfo;
            if (cameraToDelete != null)
            {
                this.store.Cameras.Remove(cameraToDelete);
                Save();
                if (this.store.Cameras.Count == 0)
                {
                    StartSearch();
                }
            }
        }

        private void OnCameraSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteButton.Visibility = (CameraGrid.SelectedItem != null) ? Visibility.Visible : Visibility.Collapsed;
        }


        public void OnSuspending()
        {
            Disconnect();
        }

    }
}
