using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Networking;
using OutlookSyncPhone.Utilities;

namespace OutlookSyncPhone.Pages
{
    public partial class ManualConnectPage : PhoneApplicationPage
    {
        ConnectionManager conmgr;

        public ManualConnectPage()
        {
            InitializeComponent();

            ErrorMessage.Text = "";
            ConnectButton.IsEnabled = false;
        }

        public ConnectionManager ConnectionManager
        {
            get { return conmgr; }
            set { conmgr = value; }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var settings = App.Settings;
            if (settings != null)
            {
                ServerAddress.Text = "" + settings.ServerAddress;
            }
            UpdateButtonState();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            App.Settings.ServerAddress = ServerAddress.Text;
            base.OnNavigatingFrom(e);
        }

        bool connecting;

        private async void OnConnect(object sender, RoutedEventArgs e)
        {
            IPEndPoint addr;
            string text = ServerAddress.Text;
            if (!string.IsNullOrEmpty(text) && ConnectionManager.TryParseEndPoint(text, out addr))
            {
                try
                {
                    ShowProgress();

                    connecting = true;
                    ConnectButton.IsEnabled = false;

                    await conmgr.TryConnectServer(new string[] { text }, true);

                    ConnectButton.IsEnabled = true;
                    HideProgress();
                    NavigationService.GoBack();
                }
                catch (Exception ex)
                {
                    ErrorMessage.Text = ex.Message;
                    HideProgress();
                }
                connecting = false;
            }
        }

        private void ShowProgress()
        {
            Progress.Visibility = System.Windows.Visibility.Visible;
            Progress.IsIndeterminate = true;
        }

        private void HideProgress()
        {
            Progress.Visibility = System.Windows.Visibility.Collapsed;
            Progress.IsIndeterminate = false;
        }

        private void OnServerAddressChanged(object sender, TextChangedEventArgs e)
        {
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            if (!connecting)
            {
                ErrorMessage.Text = "";
                IPEndPoint addr;
                string text = ServerAddress.Text;
                ConnectButton.IsEnabled = !string.IsNullOrEmpty(text) && ConnectionManager.TryParseEndPoint(text, out addr);
            }
        }

        private void OnBackPressed(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

    }
}