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
using OutlookSyncPhone.Pages;
using Windows.Phone.System.Analytics;
using System.Reflection;

namespace OutlookSyncPhone
{
    public partial class MainPage : PhoneApplicationPage
    {
        bool offline;
        ConnectionManager conmgr;
        bool conmgrStarted;
        ServerProxy proxy;
        PhoneStoreLoader loader;
        string phoneName;
        string phoneId;
        SyncResult syncStatus;

        // Constructor
        public MainPage()
        {
            Debug.WriteLine("Starting time " + UnifiedStore.SyncTime);

            InitializeComponent();

            MessagePrompt.Text = "";

            phoneId = HostInformation.PublisherHostId;

            phoneName = Windows.Networking.Proximity.PeerFinder.DisplayName;

            DeviceNetworkInformation.NetworkAvailabilityChanged += new EventHandler<NetworkNotificationEventArgs>(OnNetworkAvailabilityChanged);

            this.SizeChanged += MainPage_SizeChanged;

            conmgr = new ConnectionManager("F657DBF0-AF29-408F-8F4A-B662D7EA4440", GetHelloMessage(), 12777);            
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

            App.Settings.ServerAddress = proxy.RemoteEndPoint.ToString();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessagePrompt.Text = AppResources.ConnectedMessage;
                Connector.Connected = true;              
            }));

            await proxy.SendMessage(new Message() { Command = "Connect", Parameters = GetHelloMessage() });

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
            if (loader == null)
            {
                // user has gone elsewhere.
                return;
            }

            Message m = e.Message;
            if (m == null)
            {
                return;
            }

            string parameters = m.Parameters;
            if (string.IsNullOrEmpty(parameters))
            {
                parameters = "";
            }

            switch (m.Command)
            {
                case "Count":
                    int max = 0;
                    int.TryParse(parameters, out max);
                    outlookContactCount = max;
                    
                    loader.StartMerge();

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessagePrompt.Text = AppResources.SyncPrompt;
                    }));

                    // start by sending our version information
                    await SendSyncMessage();
                    break;
                case "ServerSync":
                    await loader.HandleServerSync(parameters, syncStatus);
                    
                    StartSyncProgress();

                    // then start pulling updates from server.                 
                    if (!await GetNextItem())
                    {
                        // done pulling, start sending.
                        goto case "StartPushing";
                    }
                    break;
                case "Contact":
                    {
                        // server responded to GetNextContact 
                        var result = await loader.MergeContact(parameters);
                        if (result.Item1 == MergeResult.NewEntry)
                        {
                            syncStatus.ServerInserted.Add(new SyncItem(result.Item2));
                            UpdateTiles();
                        }
                        else if (result.Item1 == MergeResult.Merged)
                        {
                            syncStatus.ServerUpdated.Add(new SyncItem(result.Item2));
                            UpdateTiles();
                        }

                        if (!await GetNextItem())
                        {
                            // done pulling, start sending.
                            goto case "StartPushing";
                        }
                    }
                    break;
                case "Appointment":
                    {
                        // server responded to GetNextAppointment
                        var result = await loader.MergeAppointment(parameters);
                        if (result.Item1 == MergeResult.NewEntry)
                        {
                            syncStatus.ServerInserted.Add(new SyncItem(result.Item2));
                            UpdateTiles();
                        }
                        else if (result.Item1 == MergeResult.Merged)
                        {
                            syncStatus.ServerUpdated.Add(new SyncItem(result.Item2));
                            UpdateTiles();
                        }

                        if (!await GetNextItem())
                        {
                            // done pulling, start sending.
                            goto case "StartPushing";
                        }
                    }
                    break;
                case "StartPushing":
                    // start sending updates to server, server responds with "Updated"
                    if (!await SendNextUpdate())
                    {
                        await FinishUpdate();
                    }
                    break;
                case "UpdatedContact":
                    // received a new outlook id for this a contact.
                    await loader.UpdateContactRemoteId(parameters);

                    if (!await SendNextUpdate())
                    {
                        await FinishUpdate();
                    }
                    break;
                case "UpdatedAppointment":
                    // received a new outlook id for this a contact.
                    loader.UpdateAppointmentOutlookId(parameters);

                    if (!await SendNextUpdate())
                    {
                        await FinishUpdate();
                    }
                    break;

            }
        }

        private void StartSyncProgress()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateTiles();

                // start sync progress indicator
                LoadProgress.Visibility = System.Windows.Visibility.Visible;
                LoadProgress.IsIndeterminate = false;
                LoadProgress.Minimum = 0;
                LoadProgress.Maximum = syncStatus.TotalChanges;
                LoadProgress.Value = 0;
            }));
        }

        private async Task FinishUpdate()
        {
            if (loader == null)
            {
                // user has gone away
                return;
            }
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
            if (loader == null)
            {
                // user has gone away.
                return;
            }
            var syncReport = loader.GetSyncMessage();
            await proxy.SendMessage(new Message() { Command = "SyncMessage", Parameters = syncReport .ToXml() });
        }

        private async Task<bool> SendNextUpdate()
        {
            if (loader == null)
            {
                // user has gone away
                return false;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadProgress.Value++;
            }));

            var item = loader.GetNextItemToSend();
            while (item != null)
            {
                if (item.Type == "C")
                {
                    UnifiedContact contact = loader.UnifiedStore.FindContactById(item.Id);
                    if (contact != null)
                    {
                        await proxy.SendMessage(new Message() { Command = "UpdateContact", Parameters = contact.ToXml() });
                        return true;
                    }
                }
                else
                {
                    UnifiedAppointment appointment = loader.UnifiedStore.FindAppointmentByPhoneId(item.PhoneId);
                    if (appointment != null)
                    {
                        await proxy.SendMessage(new Message() { Command = "UpdateAppointment", Parameters = loader.GetSyncXml(appointment) });
                        return true;
                    }
                }
                
                // try again then..
                item = loader.GetNextItemToSend();
            }
            return false;
        }

        private async Task UpdateServer(UnifiedContact moreRecent)
        {            
            await proxy.SendMessage(new Message() { Command = "UpdateContact", Parameters = moreRecent.ToXml() });
        }

        private async Task<bool> GetNextItem()
        {
            if (loader == null)
            {
                // user has gone away
                return false;
            }

            SyncItem item = loader.GetNextItemToUpdate();
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadProgress.Value++;
            }));

            if (proxy != null && item != null)
            {
                if (item.Type == "C")
                {
                    await proxy.SendMessage(new Message() { Command = "GetContact", Parameters = item.Id });
                }
                else
                {
                    await proxy.SendMessage(new Message() { Command = "GetAppointment", Parameters = item.Id });
                }
            }

            return item != null;
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

                    AdGrid.Children.Clear();
                    AdGrid.Children.Add(adControl);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Exception: error creating ad control???");
            }
        }

        private void OnAdControlError(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            Debug.WriteLine("OnAdControlError: " + e.Error.Message);
            return;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {

            SetupAds();

            if (loader == null)
            {
                MessagePrompt.Text = AppResources.LoadingContacts;
                ShowLoadProgress();
                loader = new PhoneStoreLoader();
                await loader.LoadContacts();

#if SUPPORT_APPOINTMENTS
                MessagePrompt.Text = AppResources.LoadingAppointments;
                await loader.LoadAppointments();
#endif
                if (loader != null)
                {
                    syncStatus = loader.GetLocalSyncResult();
                    UpdateTiles();
                    HideLoadProgress();
                    MessagePrompt.Text = AppResources.LaunchPrompt;
                }
            }

            if (!DeviceNetworkInformation.IsNetworkAvailable)
            {
                MessagePrompt.Text = AppResources.NoNetwork;
            }
            else
            {
                StartConnectionManager();
            }


            base.OnNavigatedTo(e);
        }

        void StartConnectionManager()
        {
            if (!conmgrStarted)
            {
                conmgrStarted = true;
                conmgr.ServerFound += OnServerFound;
                conmgr.ServerLost += OnServerLost;
                conmgr.Start();
            }
        }

        string GetHelloMessage()
        {
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            var version = nameHelper.Version;

            // sometimes the phoneId contains slashes which confuses our uri format here.
            phoneId = Uri.EscapeDataString(phoneId);

            return version.ToString() + "/" + phoneName + "/" + phoneId;
        }

        private void UpdateTiles()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AnimateCount(InsertIndicator, syncStatus.GetTotalInserted());
                AnimateCount(UpdateIndicator, syncStatus.GetTotalUpdated());
                AnimateCount(UnchangedIndicator, syncStatus.Unchanged);
                AnimateCount(DeleteIndicator, syncStatus.GetTotalDeleted());
            }));
        }

        void AnimateCount(SyncProgressControl ctrl, List<SyncItem> list)
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.To = list == null ? 0 : list.Count;
            animation.Duration = new Duration(TimeSpan.FromSeconds(.3));

            ctrl.Tag = list;

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

            HideLoadProgress();

            ReportPage page = e.Content as ReportPage;
            if (page != null)
            {
                page.ContactList = selectedList;
                page.PageTitle = selectedTile.SubText;
                page.TitleBackground = selectedTile.TileBackground;
                return;
            }

            SettingsPage settingsPage = e.Content as SettingsPage;
            if (settingsPage != null)
            {
                return;
            }

            ManualConnectPage mcp = e.Content as ManualConnectPage;
            if (mcp != null)
            {
                mcp.ConnectionManager = this.conmgr;
                return;
            }

            // going elsewhere? Then we need to unload.
            this.loader = null;

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
            else
            {
                Debug.WriteLine("closing, not connected");
            }

            if (conmgrStarted)
            {
                conmgrStarted = false;
                conmgr.ServerFound -= OnServerFound;
                conmgr.ServerLost -= OnServerLost;
                conmgr.Stop();
            }

            DeviceNetworkInformation.NetworkAvailabilityChanged -= new EventHandler<NetworkNotificationEventArgs>(OnNetworkAvailabilityChanged);
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
                StartConnectionManager();
            }
        }

        SyncProgressControl selectedTile;
        List<SyncItem> selectedList;

        private void OnIndicatorClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SyncProgressControl ctrl = (SyncProgressControl)sender;
            selectedTile = ctrl;
            selectedList = (List<SyncItem>)ctrl.Tag;
            if (selectedList != null && selectedList.Count > 0)
            {
                this.NavigationService.Navigate(new Uri("/Pages/ReportPage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void OnAboutClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Pages/ManualConnectPage.xaml", UriKind.RelativeOrAbsolute));
        }

    }
}