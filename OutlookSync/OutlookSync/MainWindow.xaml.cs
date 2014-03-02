using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.Networking;
using OutlookSync.Model;
using System.Collections.ObjectModel;
using OutlookSync.Utilities;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace OutlookSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ConnectionManager conmgr;
        UnifiedStore store;
        public const string StoreFolder = @"LovettSoftware\OutlookSync";
        public const string StoreFileName = "store.xml";
        public const string SettingsFileName = "settings.xml";
        public const string LogFileName = "log.txt";
        Dictionary<string, ConnectedPhone> connected = new Dictionary<string, ConnectedPhone>();
        ObservableCollection<ConnectedPhone> items = new ObservableCollection<ConnectedPhone>();
        Settings settings;
        Updater updater = new Updater();
        FirewallConfig firewall;
        //WebServer webserver;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;

            Log.OpenLog(GetLogFileName());

            Debug.WriteLine("Starting time " + UnifiedStore.SyncTime);

            PhoneList.ItemsSource = items;

            firewall = new FirewallConfig(this.ExePath);
            firewall.FirewallErrorDetected += OnFirewallErrorDetected;
            
            VersionNumber.Text = string.Format(VersionNumber.Text, updater.GetCurrentVersion().ToString());

            //webserver = new WebServer();
            //webserver.Start();
        }

        bool isShowingFirewallError;

        private void OnFirewallErrorDetected(object sender, FirewallEventArgs e)
        {
            if (e.FirewallEntryMissing || e.FirewallSettingsIncorrect)
            {
                if (!isShowingFirewallError)
                {
                    isShowingFirewallError = true;

                    ShowMessage(new Action<FlowDocument>((doc) =>{

                        Paragraph p = new Paragraph();
                        p.Inlines.Add(new Run(settings.FirstLoad ? Properties.Resources.FirstLaunchFirewallSettings : Properties.Resources.FirewallNotRight));

                        Hyperlink link = new Hyperlink();
                        link.Inlines.Add(Properties.Resources.FixFirewallLinkCaption);
                        link.Cursor = Cursors.Arrow;
                        link.PreviewMouseLeftButtonDown += OnFixFirewall;
                        
                        Paragraph p2 = new Paragraph();
                        p2.Inlines.Add(link);

                        doc.Blocks.Add(p);
                        doc.Blocks.Add(p2);

                    }));
                }
            }
            else if (isShowingFirewallError)
            {
                HideMessage();
            }
        }

        string ExePath
        {
            get
            {
                Process p = Process.GetCurrentProcess();
                return  p.MainModule.FileName;
            }
        }

        void OnFixFirewall(object sender, MouseButtonEventArgs e)
        {
            firewall.FixFirewallSettings(this.ExePath, false);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F6 && conmgr != null)
            {
                ShowMessage("Server is listening on the following addresses:\n" +
                    string.Join("\n", conmgr.ServerEndPoints.ToArray()));
            }
            base.OnPreviewKeyDown(e);
        }

        string GetStoreFileName()
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StoreFolder);
            string file = System.IO.Path.Combine(dir, StoreFileName);
            return file;
        }


        string GetSettingsFileName()
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StoreFolder);
            string file = System.IO.Path.Combine(dir, SettingsFileName);
            return file;
        }

        string GetLogFileName()
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StoreFolder);
            string file = System.IO.Path.Combine(dir, LogFileName);
            return file;
        }

        async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                RichMessageDocument.FontFamily = this.FontFamily;

                settings = await Settings.LoadAsync(GetSettingsFileName());

                if (updater.IsFirstLaunch)
                {
                    settings.FirstLoad = true;
                }

                conmgr = new ConnectionManager("F657DBF0-AF29-408F-8F4A-B662D7EA4440");
                if (!conmgr.HasInternet())
                {
                    ShowMessage(Properties.Resources.NoNetwork);
                }
                else
                {
                    conmgr.StartListening(12777, 57365, 59650, 63203, 53889, 65238, 55264, 51764, 59305, 57979, 53993);

                    conmgr.MessageReceived += OnMessageReceived;
                    conmgr.ReadException += OnServerException;
                    if (conmgr != null)
                    {
                        ShowStatus(OutlookSync.Properties.Resources.WaitingForPhone);
                        store = await UnifiedStore.LoadAsync(GetStoreFileName());
                    }

                    firewall.StartCheckingFirewall();
                }

                // get new contact info from outlook ready for syncing with this phone.
                var loader = new OutlookStoreLoader();
                bool started = false;
                try
                {
                    loader.StartOutlook();
                    started = true;
                }
                catch (Exception)
                {
                    ShowMessage(Properties.Resources.NoOutlook);
                }
                if (started)
                {
                    await loader.UpdateAsync(store);
                }

            }
            catch (NoPortsAvailableException)
            {
                ShowMessage(Properties.Resources.NoPortsAvailable);
            } 
            catch (Exception ex)
            {
                UnhandledExceptionWindow uew = new UnhandledExceptionWindow();
                uew.ErrorMessage = ex.ToString();
                uew.ShowDialog();
            }      

        }

        private void OnServerException(object sender, ServerExceptionEventArgs e)
        {
            OnDisconnect(e.RemoteEndPoint);
        }

        private void OnDisconnect(System.Net.IPEndPoint endpoint)
        {
            string key = endpoint.Address.ToString();
            ConnectedPhone phone = null;
            connected.TryGetValue(key, out phone);

            if (phone != null)
            {
                phone.Connected = false;

                connected.Remove(key);

                Dispatcher.Invoke(new Action(() =>
                {
                    items.Remove(phone);
                    if (items.Count == 0)
                    {
                        ShowStatus(OutlookSync.Properties.Resources.WaitingForPhone);
                    }
                    else
                    {
                        ShowStatus("");
                    }
                    UpdateConnector();
                }));
            }
        }


        void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Log.WriteLine(DateTime.Now.ToLongTimeString() + ": message received from " + e.RemoteEndPoint.ToString());
            Log.WriteLine("  Command: " + e.Message.Command + "(" + e.Message.Parameters + ")");

            e.Response = HandleMessage(e);
        }

        async Task<Message> HandleMessage(MessageEventArgs e)
        {
            Debug.WriteLine(e.Message.Command);
            var msg = e.Message;
            switch (msg.Command)
            {
                case "Hello":
                    await OnCreatePhone(e);
                    break;
                case "Disconnect":
                    OnDisconnect(e.RemoteEndPoint);
                    break;
                case "Connect":
                    await OnConnect(e);
                    break;
                case "FinishUpdate":                    
                    await store.SaveAsync(GetStoreFileName());
                    ShowStatus(Properties.Resources.SyncComplete);
                    break;
            }

            string key = e.RemoteEndPoint.Address.ToString();
            ConnectedPhone phone = null;
            connected.TryGetValue(key, out phone);

            Message response = null;
            if (phone != null)
            {
                response = phone.HandleMessage(e);
            }

            return response;
        }

        private void ShowStatus(string msg)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                StatusMessage.Text = msg;
            }));
        }

        public void ShowMessage(string text)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                MessageBorder.Width = this.Width * 0.5;
                MessageBorder.Visibility = System.Windows.Visibility.Visible;
                RichMessageDocument.Blocks.Clear();
                Paragraph p = new Paragraph(new Run(text));
                RichMessageDocument.Blocks.Add(p);
            }));
        }

        public void ShowMessage(Action<FlowDocument> messageCreator)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                MessageBorder.Width = this.Width * 0.5;
                MessageBorder.Visibility = System.Windows.Visibility.Visible;
                RichMessageDocument.Blocks.Clear();
                messageCreator(RichMessageDocument);
            }));
        }

        public void HideMessage()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBorder.Visibility = System.Windows.Visibility.Collapsed;
                RichMessageDocument.Blocks.Clear();
            }));
        }

        bool busy;
        bool mustRestart;

        private async Task OnCreatePhone(MessageEventArgs e)
        {
            if (mustRestart) 
            {
                return;
            }

            if (busy)
            {
                // ignore this hello
                return;
            }

            if (this.store == null)
            {
                // still loading, not done yet!
                return;
            }

            if (updater.IsWaitingForUpdate)
            {
                return;
            }

            busy = true;
            try
            {
                var msg = e.Message;

                string phoneAppVersion = "";
                string phoneName = "Unknown";
                string phoneId = "";
                string parameters = msg.Parameters;
                if (parameters != null)
                {
                    string[] parts = msg.Parameters.Split('/');
                    if (parts.Length > 0)
                    {
                        phoneAppVersion = parts[0];
                    }
                    if (parts.Length > 1)
                    {
                        phoneName = parts[1];
                    }
                    if (parts.Length > 2)
                    {
                        phoneId = Uri.UnescapeDataString(parts[2]);
                    }
                }

                string key = e.RemoteEndPoint.Address.ToString();

                ConnectedPhone phone = null;
                connected.TryGetValue(key, out phone);

                if (phone == null)
                {
                    Log.WriteLine("Connecting new phone: " + e.RemoteEndPoint.ToString());

                    bool result = await CheckVersions(phoneAppVersion);

                    if (!result)
                    {
                        return;
                    }

                    HideMessage();
                    phone = new ConnectedPhone(store, this.Dispatcher, GetStoreFileName());
                    phone.IPEndPoint = e.RemoteEndPoint.ToString();
                    phone.PropertyChanged += OnPhonePropertyChanged;
                    connected[key] = phone;

                    Dispatcher.Invoke(new Action(() =>
                    {
                        phone.Name = phoneName;
                        phone.Id = phoneId;
                        items.Add(phone);
                        UpdateConnector();
                    }));

                    ShowStatus(Properties.Resources.LoadingOutlookContacts);

                    // get new contact info from outlook ready for syncing with this phone.
                    await phone.SyncOutlook();

                    ShowStatus(string.Format(Properties.Resources.LoadedCount, store.Contacts.Count));

                }
                else
                {

                    HideMessage();

                    Dispatcher.Invoke(new Action(() =>
                    {
                        // phone reconnected, so start over by toggling the "Allowed" property to ensure the
                        // AllowRemoteMachine is called which results in BroadcastEndPoints so the phone
                        // hears back from it's "hello" ping.

                        phone.Allowed = false;
                        
                        if (settings.TrustedPhones.Contains(phone.Id))
                        {
                            phone.Allowed = true;
                        }
                    }));
                }
            }
            finally
            {
                busy = false;
            }

        }

        private async Task<bool> CheckVersions(string phoneAppVersion)
        {
            bool result = true;
            Version phoneVersion = null;
            Version.TryParse(phoneAppVersion, out phoneVersion);
            Version currentAppVersion = this.updater.GetCurrentVersion();

            // only care about major/minor version numbers, the build numbers should be "compatible".
            if (phoneVersion.Major < currentAppVersion.Major || (phoneVersion.Minor < currentAppVersion.Minor))
            {                
                // PC version is newer...
                ShowMessage(string.Format(Properties.Resources.PhoneUpdateRequired, phoneAppVersion, currentAppVersion.ToString()));
                ShowStatus(Properties.Resources.WaitingForNewPhoneVersion);
                result = false;
            }
            else if (phoneVersion.Major > currentAppVersion.Major || (phoneVersion.Major == currentAppVersion.Major && phoneVersion.Minor > currentAppVersion.Minor))
            {
                // phone version is newer
                var info = await this.updater.CheckForUpdate();
                if (info != null && info.UpdateAvailable && info.AvailableVersion >= phoneVersion)
                {
                    await updater.Update();

                    ShowMessage(string.Format(Properties.Resources.UpdateRequired, phoneAppVersion, currentAppVersion.ToString()));
                    ShowStatus(Properties.Resources.WaitingForRestart);
                    result = false;
                    mustRestart = true;
                }
                else 
                {
                    ShowMessage(string.Format(Properties.Resources.UpdateMissing, phoneAppVersion, currentAppVersion.ToString()));
                    ShowStatus(Properties.Resources.WaitingForUpdate);
                    updater.BeginWatchForUpdate();
                    result = false;
                }
            }
            return result;
        }


        private void OnPhonePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ConnectedPhone phone = (ConnectedPhone)sender;
            if (e.PropertyName == "Allowed" && phone.Allowed)
            {
                if (!string.IsNullOrEmpty(phone.Id))
                {
                    if (!settings.TrustedPhones.Contains(phone.Id))
                    {
                        settings.TrustedPhones.Add(phone.Id);
                    }
                }

                // ok, user has given us the green light to connect this phone.
                if (conmgr != null)
                {
                    conmgr.AllowRemoteMachine(phone.IPEndPoint);
                }
            }
        }

        private async Task OnConnect(MessageEventArgs e)
        {
            var msg = e.Message;
            string key = e.RemoteEndPoint.Address.ToString();
            ConnectedPhone phone = null;
            connected.TryGetValue(key, out phone);

            if (phone == null)
            {
                await OnCreatePhone(e);

                if (!connected.TryGetValue(key, out phone))
                {
                    // phone not allowed.
                    return;
                }
            }

            phone.Connected = true;
        }

        private void UpdateConnector()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Connector.Connected = items.Count > 0;
            }));
        }

        protected async override void OnClosed(EventArgs e)
        {
            if (conmgr != null)
            {
                conmgr.StopListening();
            }
            firewall.StopCheckingFirewall();

            settings.FirstLoad = false;
            await settings.SaveAsync();
            Log.CloseLog();
            base.OnClosed(e);
        }

        private void OnShowHelp(object sender, ExecutedRoutedEventArgs e)
        {
            OpenUrl("http://www.lovettsoftware.com/LovettSoftware/post/2014/01/09/Outlook-Sync.aspx");
        }

        public static string StartupPath
        {
            get
            {
                Process p = Process.GetCurrentProcess();
                string exe = p.MainModule.FileName;
                return System.IO.Path.GetDirectoryName(exe);
            }
        }

        public void OpenUrl(string url)
        {            
            const int SW_SHOWNORMAL = 1;
            int rc = ShellExecute(IntPtr.Zero, "open", url, null, StartupPath, SW_SHOWNORMAL);
        }
        


        [DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
            SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int ShellExecute(IntPtr handle, string verb, string file, string args, string dir, int show);
    }
}
