using AutomaticPairingDemo.Bluetooth;
using System;
using System.Collections.Generic;
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
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
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
                    ShowMessage("Please enter device MAC address");
                }
                else
                {
                    PairingButton.IsEnabled = true;
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
                if (_manager == null)
                {
                    _manager = new BluetoothConnectionManager();
                }

                PairingProgress.Visibility = Visibility.Visible;
                PairingProgress.IsIndeterminate = true;
                PairingButton.Content = "Cancel";

                try
                {
                    await _manager.PairAsync("123", TimeSpan.FromSeconds(10000));

                    ShowMessage("pairing successful");
                }
                catch (Exception ex)
                {
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
            PairingButton.Tag = null;
            PairingButton.Content = "Start Pairing";
            PairingProgress.Visibility = Visibility.Collapsed;
            PairingProgress.IsIndeterminate = false;
        }
    }
}
