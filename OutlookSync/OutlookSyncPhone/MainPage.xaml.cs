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
using OutlookSync.Model;

namespace OutlookSyncPhone
{
    public partial class MainPage : PhoneApplicationPage
    {
        bool offline;
        ConnectionManager conmgr;
        ServerProxy proxy;
        PhoneStoreLoader loader;
        string phoneName;

        // Constructor
        public MainPage()
        {
            Debug.WriteLine("Starting time " + Environment.TickCount);

            InitializeComponent();

            phoneName = Windows.Networking.Proximity.PeerFinder.DisplayName;

            DeviceNetworkInformation.NetworkAvailabilityChanged += new EventHandler<NetworkNotificationEventArgs>(OnNetworkAvailabilityChanged);
        }

        async void OnServerFound(object sender, ServerEventArgs e)
        {
            proxy = e.Server;
            proxy.MessageReceived += OnMessageReceived;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessagePrompt.Text = AppResources.ConnectedMessage;
                Connector.Connected = true;
                SyncIndicator.Visibility = System.Windows.Visibility.Visible;
                SyncIndicator.Current = 0;
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
                    
                    loader.StartMerge();

                    // start by sending our version information
                    await SendSyncMessage();
                    break;
                case "ServerSync":
                    await loader.HandleServerSync(m.Parameters);
                    // then start pulling updates from server.                 
                    if (!await GetNextContact())
                    {
                        // done pulling, start sending.
                        goto case "StartPulling";
                    }
                    break;
                case "Contact":
                    // server responded to GetNextContact 
                    await loader.MergeContact(m.Parameters);
                    if (!await GetNextContact())
                    {
                        // done pulling, start sending.
                        goto case "StartPulling";
                    }
                    break;
                case "StartPulling":
                    loader.FinishMerge();

                    // start sending updates to server, server responds with "Updated"
                    if (!await SendNextUpdate())
                    {
                        await FinishUpdate();
                    }
                    break;
                case "Updated":
                    // received a new outlook id for this a contact.
                    await loader.UpdateRemoteId(e.Message.Parameters);

                    if (!await SendNextUpdate())
                    {
                        await FinishUpdate();
                    }
                    break;

            }
        }

        private async Task FinishUpdate()
        {
            await proxy.SendMessage(new Message() { Command = "FinishUpdate" });

            // our cache has been updated, so save it!
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                MessagePrompt.Text = AppResources.SavingUpdates;
                ShowLoadProgress();

                await loader.Save();

                HideLoadProgress();
                MessagePrompt.Text = AppResources.AllDone;
            }));
        }

        private async Task<bool> SendSyncMessage()
        {
            var syncReport = loader.GetSyncMessage();
            if (syncReport != null)
            {
                await proxy.SendMessage(new Message() { Command = "SyncMessage", Parameters = syncReport .ToXml() });
                return true;
            }
            return false;
        }

        private async Task<bool> SendNextUpdate()
        {
            var c = loader.GetNextContactToSend();
            if (c != null)
            {
                await proxy.SendMessage(new Message() { Command = "UpdateContact", Parameters = c.ToXml() });
                return true;
            }
            return false;
        }

        private async Task UpdateServer(UnifiedContact moreRecent)
        {            
            await proxy.SendMessage(new Message() { Command = "UpdateContact", Parameters = moreRecent.ToXml() });
        }

        private async Task<bool> GetNextContact()
        {
            string id = loader.GetNextContactToUpdate();
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncIndicator.Maximum = contactCount;
                SyncIndicator.Current = contactCount - loader.RemainingContactsToUpdate;
            }));

            if (proxy != null && id != null)
            {               
                await proxy.SendMessage(new Message() { Command = "GetContact", Parameters = id });
            }

            return id != null;
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
            ShowLoadProgress();
            MessagePrompt.Text = AppResources.LoadingStore;

            SetupAds();

            loader = new PhoneStoreLoader();
            await loader.Open();

            HideLoadProgress();

            conmgr = new ConnectionManager("F657DBF0-AF29-408F-8F4A-B662D7EA4440", phoneName, 12777);
            conmgr.ServerFound += OnServerFound;
            conmgr.ServerLost += OnServerLost;
            conmgr.Start();
            
            MessagePrompt.Text = AppResources.LaunchPrompt;

            base.OnNavigatedTo(e);
        }

        private void ShowLoadProgress()
        {
            LoadProgress.Visibility = System.Windows.Visibility.Visible;
            LoadProgress.IsIndeterminate = true;
        }

        private void HideLoadProgress()
        {
            LoadProgress.Visibility = System.Windows.Visibility.Collapsed;
            LoadProgress.IsIndeterminate = false;
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
            HideLoadProgress();
        }

        void OnNetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            // if we are online now then logon and catch up with pending requests
            if (e.NotificationType == NetworkNotificationType.InterfaceConnected && this.offline)
            {
                this.offline = false;
                MessagePrompt.Text = AppResources.LaunchPrompt;
            }
            else if (e.NotificationType == NetworkNotificationType.InterfaceDisconnected)
            {
                this.offline = true;
                MessagePrompt.Text = AppResources.NoNetwork;
            }
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/SettingsPage.xaml", UriKind.RelativeOrAbsolute));
        }

    }
}