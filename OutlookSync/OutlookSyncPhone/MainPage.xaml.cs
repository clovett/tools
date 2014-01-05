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
using OutlookSyncPhone.Utilities;
using System.Windows.Media.Animation;

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
            Debug.WriteLine("Starting time " + UnifiedStore.SyncTime);

            InitializeComponent();

            MessagePrompt.Text = "";

            phoneName = Windows.Networking.Proximity.PeerFinder.DisplayName;

            DeviceNetworkInformation.NetworkAvailabilityChanged += new EventHandler<NetworkNotificationEventArgs>(OnNetworkAvailabilityChanged);

            this.SizeChanged += MainPage_SizeChanged;
        }

        void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double w = e.NewSize.Width * 0.8;
            TileGrid.Width = TileGrid.Height = w;
        }

        async void OnServerFound(object sender, ServerEventArgs e)
        {
            proxy = e.Server;
            proxy.MessageReceived += OnMessageReceived;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessagePrompt.Text = AppResources.ConnectedMessage;
                Connector.Connected = true;              
            }));

            await proxy.SendMessage(new Message() { Command = "Connect", Parameters = phoneName });

        }

        private void OnServerLost(object sender, ServerExceptionEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessagePrompt.Text = AppResources.ConnectionLost;
                Connector.Connected = false;                
            }));
        }

        int outlookContactCount;

        async void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Message m = e.Message;
            switch (m.Command)
            {
                case "Count":
                    int max = 0;
                    int.TryParse(m.Parameters, out max);
                    outlookContactCount = max;
                    
                    loader.StartMerge();

                    // start by sending our version information
                    await SendSyncMessage();
                    break;
                case "ServerSync":
                    SyncResult result = await loader.HandleServerSync(m.Parameters);
                    
                    StartSyncProgress(result);

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

        SyncResult syncProgress;

        private void StartSyncProgress(SyncResult result)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateTiles(result);

                syncProgress = result;

                // start sync progress indicator
                LoadProgress.Visibility = System.Windows.Visibility.Visible;
                LoadProgress.IsIndeterminate = false;
                LoadProgress.Minimum = 0;
                LoadProgress.Maximum = result.PhoneInserted + result.PhoneUpdated + result.ServerInserted + result.ServerUpdated;
                LoadProgress.Value = 0;
            }));
        }

        private async Task FinishUpdate()
        {
            if (proxy != null)
            {
                await proxy.SendMessage(new Message() { Command = "FinishUpdate" });
            }

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

        private async Task SendSyncMessage()
        {
            var syncReport = loader.GetSyncMessage();
            await proxy.SendMessage(new Message() { Command = "SyncMessage", Parameters = syncReport .ToXml() });
        }

        private async Task<bool> SendNextUpdate()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadProgress.Value++;
            }));

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
                LoadProgress.Value++;
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

            UpdateTiles(loader.GetLocalSyncResult());

            HideLoadProgress();

            conmgr = new ConnectionManager("F657DBF0-AF29-408F-8F4A-B662D7EA4440", phoneName, 12777);
            conmgr.ServerFound += OnServerFound;
            conmgr.ServerLost += OnServerLost;
            conmgr.Start();
            
            MessagePrompt.Text = AppResources.LaunchPrompt;

            base.OnNavigatedTo(e);
        }

        private void UpdateTiles(SyncResult syncResult)
        {
            AnimateCount(InsertIndicator, syncResult.PhoneInserted + syncResult.ServerInserted);
            AnimateCount(UpdateIndicator, syncResult.PhoneUpdated + syncResult.ServerUpdated);
            AnimateCount(UnchangedIndicator, syncResult.Unchanged);
            AnimateCount(DeleteIndicator, syncResult.PhoneDeleted + syncResult.ServerDeleted);
        }

        void AnimateCount(SyncProgressControl ctrl, int count)
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.To = count;
            animation.Duration = new Duration(TimeSpan.FromSeconds(1));

            Storyboard s = new Storyboard();
            Storyboard.SetTarget(animation, ctrl);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Count"));
            s.Children.Add(animation);
            s.Begin();
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