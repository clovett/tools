using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using FoscamExplorer.Resources;
using FoscamExplorer.Pages;

namespace FoscamExplorer
{
    public partial class MainPage : PhoneApplicationPage
    {
        DataStore store;
        
        // Constructor
        public MainPage()
        {
            this.InitializeComponent();

            this.store = DataStore.Instance;

//            DeleteButton.Visibility = Visibility.Collapsed;

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
            var quiet = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                {                    
                    Prompt.Text = store.Cameras.Count == 0  ? "Searching..." : "";
                    Save();
                }));
        }


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.WriteLine("MainPage OnNavigatedTo");

            if (store.Cameras.Count == 0)
            {
                Prompt.Text = "Searching...";
            }

            try
            {
                FoscamDevice.DeviceAvailable += OnDeviceAvailable;
                FoscamDevice.StartFindDevices();
            }
            catch (Exception ex)
            {
                Prompt.Text = "Error: " + ex.Message;
            }

            foreach (var cam in store.Cameras)
            {
                cam.PropertyChanged += OnCameraPropertyChanged;
            }

            store.Cameras.CollectionChanged += Cameras_CollectionChanged;

            CameraList.ItemsSource = store.Cameras;
        }

        async void OnDeviceAvailable(object sender, FoscamDevice e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                var newCam = store.MergeNewCamera(e.CameraInfo);
                newCam.LastPingTime = Environment.TickCount;
                newCam.PropertyChanged -= OnCameraPropertyChanged;
                newCam.PropertyChanged += OnCameraPropertyChanged;
            }));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Disconnect();

            LogonPage login = e.Content as LogonPage;
            if (login != null && this.selected != null)
            {
                login.DataContext = this.selected;
                login.UserName = this.selected.UserName;
                login.Password = this.selected.Password;
                if (store.Cameras.Count > 1)
                {
                    login.CheckboxVisibility = Visibility.Visible;
                }
                login.LoginClicked += OnCompleteLogin;
                
            }

            base.OnNavigatedFrom(e);
        }

        void OnCompleteLogin(object sender, EventArgs e)
        {
            LogonPage login = (LogonPage)sender;
            CameraInfo info = login.DataContext as CameraInfo;
            if (login != null)
            {
                if (info != null)
                {
                    info.UserName = login.UserName;
                    info.Password = login.Password;
                }
                if (login.CheckBoxAllIsChecked)
                {
                    PropagateLogonToAllCameras(info);
                }
                Save();
            }

        }

        private void Disconnect()
        {
            Log.WriteLine("MainPage disconnecting");

            FoscamDevice.StopFindingDevices();

            foreach (var cam in store.Cameras)
            {
                cam.PropertyChanged -= OnCameraPropertyChanged;
            }

            store.Cameras.CollectionChanged -= Cameras_CollectionChanged;

            CameraList.ItemsSource = null;
        }

        void OnDeviceError(object sender, ErrorEventArgs e)
        {
            Log.WriteLine("DeviceError: " + e.Message);
        }

        CameraInfo selected;
        
        private void OnCameraSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           // DeleteButton.Visibility = (CameraList.SelectedItem != null) ? Visibility.Visible : Visibility.Collapsed;        
            if (e.AddedItems.Count > 0)
            {
                selected = e.AddedItems[0] as CameraInfo;
                if (selected.Unauthorized)
                {
                    this.NavigationService.Navigate(new Uri("/Pages/LogonPage.xaml", UriKind.RelativeOrAbsolute));
                }
                else
                {
                    this.NavigationService.Navigate(new Uri("/Foscam/FoscamDetailsPage.xaml", UriKind.RelativeOrAbsolute));
                }
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
            var result = DataStore.Instance.SaveAsync(((FoscamExplorer.App)App.Current).CacheFolder);
        }

        //private void OnDeleteSelection(object sender, RoutedEventArgs e)
        //{
        //    CameraInfo cameraToDelete = CameraGrid.SelectedItem as CameraInfo;
        //    if (cameraToDelete != null)
        //    {
        //        this.store.Cameras.Remove(cameraToDelete);
        //        Save();
        //    }
        //}


        public void OnSuspending()
        {
            Disconnect();
        }
    }
}
