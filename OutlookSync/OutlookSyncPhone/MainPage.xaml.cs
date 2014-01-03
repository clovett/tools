using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using OutlookSyncPhone.Resources;
using System.Diagnostics;
using Microsoft.Networking;
using System.Text;
using OutlookSync;
using System.Xml;
using System.IO;
using Windows.Phone.PersonalInformation;
using System.Threading.Tasks;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Info;

namespace OutlookSyncPhone
{
    public partial class MainPage : PhoneApplicationPage
    {
        bool offline;
        ConnectionManager conmgr;
        ServerProxy proxy;
        PhoneStoreLoader loader;
        int contactIndex;
        string phoneName;

        // Constructor
        public MainPage()
        {

            InitializeComponent();

            phoneName = Windows.Networking.Proximity.PeerFinder.DisplayName;

            if (Debugger.IsAttached)
            {
                ConnectCode.Text = "111111";
            }
            DeviceNetworkInformation.NetworkAvailabilityChanged += new EventHandler<NetworkNotificationEventArgs>(OnNetworkAvailabilityChanged);
        }

        async void OnServerFound(object sender, ServerEventArgs e)
        {
            proxy = e.Server;
            proxy.MessageReceived += OnMessageReceived;

            contactIndex = 0;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessagePrompt.Text = AppResources.ConnectedMessage;
                Connector.Connected = true;
                SyncIndicator.Visibility = System.Windows.Visibility.Visible;
                SyncIndicator.Current = contactIndex;
            }));

            await proxy.SendMessage(new Message() { Command = "Connect", Parameters = phoneName });

        }

        private void OnServerLost(object sender, ServerExceptionEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessagePrompt.Text = AppResources.ConnectionLost;
                Connector.Connected = false;
                SyncIndicator.Visibility = System.Windows.Visibility.Collapsed;
                PrepareCodeBox();
            }));
        }

        int contactCount;

        async void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Message m = e.Message;
            switch (m.Command)
            {
                case "Count":
                    int max = 0;
                    int.TryParse(m.Parameters, out max);
                    contactCount = max;
                    await GetNextContact();
                    break;
                case "Contact":
                    await loader.MergeContact(m.Parameters);
                    await GetNextContact();
                    break;
                case "NoMoreContacts":
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessagePrompt.Text = AppResources.AllDone;
                    }));
                    break;
            }
        }

        private async Task GetNextContact()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncIndicator.Maximum = contactCount;
                SyncIndicator.Current = contactIndex;
            }));

            if (proxy != null)
            {
                await proxy.SendMessage(new Message() { Command = "GetContact", Parameters = contactIndex.ToString() });
            }
            contactIndex++;
        }

        Microsoft.Advertising.Mobile.UI.AdControl adControl;

        void SetupAds()
        {
            try
            {
                // user has no license to remove ads, so add them dynamically.
                // We do this after the up has launched so that the app startup is faster.
                if (adControl == null)
                {
                    adControl = new Microsoft.Advertising.Mobile.UI.AdControl();
                    adControl.Name = "bannerAd1";
                    adControl.Width = 480;
                    adControl.Height = 80;
                    adControl.AdUnitId = "10338338";
                    adControl.ApplicationId = "78e6a089-a787-42a2-9433-88c65eaa5706";
                    adControl.ErrorOccurred += OnAdControlError;
                    AdGrid.Children.Add(adControl);
                }
            }
            catch (Exception)
            {
            }
        }

        private void OnAdControlError(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            return;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            SetupAds();
            loader = new PhoneStoreLoader();
            await loader.Open();
            PrepareCodeBox();
            base.OnNavigatedTo(e);
        }

        private void PrepareCodeBox()
        {
            SyncIndicator.Visibility = System.Windows.Visibility.Collapsed;

            this.offline = !DeviceNetworkInformation.IsNetworkAvailable;
            if (this.offline)
            {
                MessagePrompt.Text = AppResources.NoNetwork;
                CodePanel.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                MessagePrompt.Text = AppResources.EnterCode;
                CodePanel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (proxy != null)
            {
                try
                {
                    await proxy.SendMessage(new Message() { Command = "Disconnect", Parameters = phoneName });
                }
                catch
                {

                }
                proxy = null;
            }

            if (conmgr != null)
            {
                conmgr.ServerFound -= OnServerFound;
                conmgr.ServerLost -= OnServerLost;
                conmgr.Stop();
            }
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}

        void OnNetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            // if we are online now then logon and catch up with pending requests
            if (e.NotificationType == NetworkNotificationType.InterfaceConnected && this.offline)
            {
                this.offline = false;

                MessagePrompt.Text = AppResources.EnterCode;
                CodePanel.Visibility = System.Windows.Visibility.Visible;
            }
            else if (e.NotificationType == NetworkNotificationType.InterfaceDisconnected)
            {
                this.offline = true;
                MessagePrompt.Text = AppResources.NoNetwork;
                CodePanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            MessagePrompt.Text = AppResources.Connecting;
            CodePanel.Visibility = System.Windows.Visibility.Collapsed;

            string code = ConnectCode.Text;

            conmgr = new ConnectionManager("F657DBF0-AF29-408F-8F4A-B662D7EA4440:" + code, 12777, 12778);
            conmgr.ServerFound += OnServerFound;
            conmgr.ServerLost += OnServerLost;
            conmgr.Start();
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/SettingsPage.xaml", UriKind.RelativeOrAbsolute));
        }

    }
}