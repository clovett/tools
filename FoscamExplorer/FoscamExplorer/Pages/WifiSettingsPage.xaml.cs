using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
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
    public sealed partial class WifiSettingsPage : Page
    {
        DispatcherTimer wifiScanTimer;
        FoscamDevice device;
        PropertyBag deviceParams;

        public WifiSettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += WifiSettingsPage_Loaded;
            this.Unloaded += WifiSettingsPage_Unloaded;
            MergeWifiItem(new WifiNetworkInfo() { SSID = "disabled", Security = WifiSecurity.None });
            PasswordBoxWifi.IsEnabled = false;

        }

        public FoscamDevice FoscamDevice
        {
            get { return device; }
            set { device = value; }
        }

        void WifiSettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            StopWifiScanner();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            StopWifiScanner();
        }

        async void WifiSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBoxWifi.Focus(Windows.UI.Xaml.FocusState.Programmatic);

            deviceParams = await device.GetParams();
            
            ShowParameters(deviceParams);

            CheckBoxAll.Visibility = (DataStore.Instance.Cameras.Count < 2) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowParameters(PropertyBag props)
        {           
            PasswordBoxWifi.Password = device.CameraInfo.WifiPassword;

            var network = device.CameraInfo.WifiNetwork;
            if (network != null)
            {
                ComboBoxWifi.SelectedIndex = MergeWifiItem(network);
            }

            StartWifiScan();
        }

        private void StartWifiScan()
        {
            device.StartScanWifi();
            wifiScanTimer = new DispatcherTimer();
            wifiScanTimer.Interval = TimeSpan.FromSeconds(2);
            wifiScanTimer.Tick += OnWifiScanTick;
            wifiScanTimer.Start();
        }


        private void StopWifiScanner()
        {
            if (wifiScanTimer != null)
            {
                wifiScanTimer.Tick -= OnWifiScanTick;
                wifiScanTimer.Stop();
                wifiScanTimer = null;
            }
        }

        async void OnWifiScanTick(object sender, object e)
        {           
            var found = await device.GetWifiScan();
            if (found != null)
            {
                foreach (var network in found)
                {
                    MergeWifiItem(network);
                }
            }
        }

        int MergeWifiItem(WifiNetworkInfo info)
        {
            int i = 0;
            foreach (WifiNetworkInfo item in ComboBoxWifi.Items)
            {
                if (item.SSID == info.SSID)
                {
                    item.Security = info.Security;
                    item.Mode = info.Mode;
                    item.BSSID = info.BSSID;
                    return i;
                }
                i++;
            }

            ComboBoxWifi.Items.Add(info);

            return ComboBoxWifi.Items.Count - 1;
        }

        private void OnWifiSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var network = ComboBoxWifi.SelectedItem as WifiNetworkInfo;
            PasswordBoxWifi.IsEnabled = (network.SSID != "disabled");
            ShowError("");
        }

        void ShowError(string text)
        {
            var quiet = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                ErrorMessage.Text = text;
            }));
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            SettingsPane.Show();
        }

        private void OnPasswordGotFocus(object sender, RoutedEventArgs e)
        {
            PasswordBoxWifi.SelectAll();
        }

        private void OnWifiPasswordChanged(object sender, RoutedEventArgs e)
        {

        }

        private async void OnUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            WifiNetworkInfo info = ComboBoxWifi.SelectedItem as WifiNetworkInfo;
            if (info != null && info.SSID == "disabled")
            {
                info = null;
            }

            device.CameraInfo.WifiNetwork = info;

            string error = await device.UpdateWifiSettings();
            if (error != null)
            {
                ShowError(error);
                return;
            }
            
            if (CheckBoxAll.IsChecked == true)
            {
                await UpdateWifiOnAllCameras(info);
            }

            this.CloseCurrentFlyout();
        }

        private async Task UpdateWifiOnAllCameras(WifiNetworkInfo info)
        {
            foreach (var camera in DataStore.Instance.Cameras)
            {
                if (camera != this.device.CameraInfo)
                {
                    FoscamDevice temp = new FoscamDevice() { CameraInfo = camera };
                    if (temp.CameraInfo.WifiNetwork == null || temp.CameraInfo.WifiNetwork.SSID != info.SSID || camera.WifiPassword != device.CameraInfo.WifiPassword)
                    {
                        camera.WifiPassword = device.CameraInfo.WifiPassword;
                        temp.CameraInfo.WifiNetwork = info;
                        await device.UpdateWifiSettings();
                    }
                }
            }
        }
    }
}
