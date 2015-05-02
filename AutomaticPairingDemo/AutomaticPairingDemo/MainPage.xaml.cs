using AutomaticPairingDemo.Bluetooth;
using SharedLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace AutomaticPairingDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        BluetoothConnectionManager _manager;
        bool _pairingInProgress;
        bool _unpairingInProgress;
        bool _paired;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.Unloaded += MainPage_Unloaded;
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_manager != null)
            {
                _manager.Dispose();
                _manager = null;
            }
        }


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var settings = Settings.Instance;
            if (!string.IsNullOrEmpty(settings.DeviceAddress))
            {
                DeviceAddress.Text = settings.DeviceAddress;
                DevicePinCode.Text = settings.PinCode;
            }

            UpdateButtonState();

            if (string.IsNullOrWhiteSpace(DeviceAddress.Text))
            {
                ShowMessage("Please enter device MAC address");
            }
        }

        void ShowMessage(string text)
        {
            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                MessageText.Text = text;
            }));
        }

        private void OnDeviceAddressChanged(object sender, TextChangedEventArgs e)
        {
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            if (_pairingInProgress)
            {
                PairingButton.Content = "Cancel";
                PairingButton.Tag = "Cancel";
                PairingButton.IsEnabled = true;
            }
            else
            {
                PairingButton.Content = "Start Pairing";

                if (string.IsNullOrWhiteSpace(DeviceAddress.Text))
                {
                    PairingButton.IsEnabled = false;
                    ShowMessage("Please enter device MAC address in HEX, something like this: 84eb18714757 ");
                }
                else
                {
                    PairingButton.IsEnabled = true;
                }
            }

            if (_unpairingInProgress)
            {
                UnpairButton.Content = "Cancel";
                UnpairButton.Tag = "Cancel";
                UnpairButton.IsEnabled = true;
            }
            else
            {
                UnpairButton.Content = "Unpair";

                if (_paired)
                {
                    UnpairButton.IsEnabled = true;
                }
                else
                {
                    PairingButton.Focus(FocusState.Programmatic);
                    UnpairButton.IsEnabled = false;
                }
            }
        }

        private async void OnStartPairing(object sender, RoutedEventArgs e)
        {
            string state = (string)PairingButton.Tag;

            if (state == "Cancel")
            {
                StopPairing();
            }
            else
            {

                ulong addr;
                if (!ulong.TryParse(DeviceAddress.Text, System.Globalization.NumberStyles.HexNumber, CultureInfo.CurrentCulture, out addr))
                {
                    ShowMessage("Please enter valid hexidecimal number for device address");
                    DeviceAddress.Focus(FocusState.Programmatic);
                    DeviceAddress.SelectAll();
                    return;
                }

                var settings = Settings.Instance;
                settings.DeviceAddress = DeviceAddress.Text;
                settings.PinCode = DevicePinCode.Text;
                var nowait = settings.SaveAsync();

                if (_manager == null)
                {
                    _manager = new BluetoothConnectionManager();
                }

                PairingProgress.Visibility = Visibility.Visible;
                PairingProgress.IsIndeterminate = true;

                PairingButton.Content = "Cancel";
                PairingButton.Tag = "Cancel";

                _pairingInProgress = true;
                _paired = false;

                try
                {
                    int start = Environment.TickCount;
                    ShowMessage("pairing started...");

                    // Finding and pairing a device can take a while, sometimes up to a minute.
                    await _manager.PairDeviceAsync(addr, DevicePinCode.Text, TimeSpan.FromSeconds(60));

                    int end = Environment.TickCount;
                    ShowMessage(string.Format("pairing completed in {0} seconds", (end - start) / 1000));

                    _paired = true;
                }
                catch (Exception ex)
                {
                    _pairingInProgress = false;
                    ShowMessage(ex.Message);
                }

                if (PairingButton.Tag != null)
                {
                    StopPairing();
                }
            }
        }

        private void StopPairing()
        {
            if (_manager != null)
            {
                _manager.Cancel();
                _manager.Dispose();
                _manager = null;
            }

            PairingButton.Tag = null;
            PairingButton.Content = "Start Pairing";
            PairingProgress.Visibility = Visibility.Collapsed;
            PairingProgress.IsIndeterminate = false;
            _pairingInProgress = false;
            UpdateButtonState();
        }

        private void StopUnpairing()
        {
            if (_manager != null)
            {
                _manager.Cancel();
                _manager.Dispose();
                _manager = null;
            }

            UnpairButton.Tag = null;
            UnpairButton.Content = "Unpair";
            PairingProgress.Visibility = Visibility.Collapsed;
            PairingProgress.IsIndeterminate = false;
            _unpairingInProgress = false;
            UpdateButtonState();
        }

        private async void OnStartUnpairing(object sender, RoutedEventArgs e)
        {
            string state = (string)UnpairButton.Tag;
            if (state == "Cancel")
            {
                StopPairing();
            }
            else
            {

                ulong addr;
                if (!ulong.TryParse(DeviceAddress.Text, System.Globalization.NumberStyles.HexNumber, CultureInfo.CurrentCulture, out addr))
                {
                    ShowMessage("Please enter valid hexidecimal number for device address");
                    DeviceAddress.Focus(FocusState.Programmatic);
                    DeviceAddress.SelectAll();
                    return;
                }

                if (_manager == null)
                {
                    _manager = new BluetoothConnectionManager();
                }

                PairingProgress.Visibility = Visibility.Visible;
                PairingProgress.IsIndeterminate = true;

                UnpairButton.Content = "Cancel";
                UnpairButton.Tag = "Cancel";
                _unpairingInProgress = true;

                try
                {
                    int start = Environment.TickCount;
                    ShowMessage("unpairing started...");

                    await _manager.UnpairDeviceAsync(addr, TimeSpan.FromSeconds(60));

                    int end = Environment.TickCount;
                    ShowMessage(string.Format("unpairing completed in {0} seconds", (end - start) / 1000));

                    _paired = false;
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.Message);
                }

                if (UnpairButton.Tag != null)
                {
                    StopUnpairing();
                }
            }
        }
    }
}
