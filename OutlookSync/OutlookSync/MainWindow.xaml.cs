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

namespace OutlookSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ConnectionManager conmgr;
        UnifiedStore store;
        const string StoreFolder = @"LovettSoftware\OutlookSync";
        const string StoreFileName = "store.xml";
        const string LogFileName = "log.txt";
        Dictionary<string, ConnectedPhone> connected = new Dictionary<string, ConnectedPhone>();
        ObservableCollection<ConnectedPhone> items = new ObservableCollection<ConnectedPhone>();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;

            Log.OpenLog(GetLogFileName());

            Debug.WriteLine("Starting time " + UnifiedStore.SyncTime);

            PhoneList.ItemsSource = items;
            conmgr = new ConnectionManager("F657DBF0-AF29-408F-8F4A-B662D7EA4440", 12777);
        }

        string GetStoreFileName()
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StoreFolder);
            string file = System.IO.Path.Combine(dir, StoreFileName);
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
            StatusMessage.Text = OutlookSync.Properties.Resources.WaitingForPhone;

            conmgr.StartListening();
            conmgr.MessageReceived += OnMessageReceived;
            conmgr.ReadException += OnServerException;
            store = await UnifiedStore.LoadAsync(GetStoreFileName());
            
            // wait for phone to connect then sync with the phone.
            FirewallConfig fc = new FirewallConfig();
            await fc.CheckSettings();
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

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    items.Remove(phone);
                    UpdateConnector();
                }));
            }
        }


        void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Log.WriteLine("Message received from " + e.RemoteEndPoint.ToString());
            Log.WriteLine("  Command: " + e.Message.Command + "(" + e.Message.Parameters + ")");

            e.Response = HandleMessage(e);
        }

        async Task<Message> HandleMessage(MessageEventArgs e)
        {
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

        private async Task OnCreatePhone(MessageEventArgs e)
        {
            if (this.store == null)
            {
                // still loading, not done yet!
                return;
            }
            var msg = e.Message;
            string key = e.RemoteEndPoint.Address.ToString();
            ConnectedPhone phone = null;
            connected.TryGetValue(key, out phone);

            if (phone == null)
            {
                phone = new ConnectedPhone(store, this.Dispatcher, GetStoreFileName());
                phone.IPEndPoint = e.RemoteEndPoint.ToString();
                phone.PropertyChanged += OnPhonePropertyChanged;
                connected[key] = phone;

                string phoneName = "Unknown";
                if (msg.Parameters != null)
                {
                    string[] parts = msg.Parameters.Split('/');
                    if (parts.Length > 0)
                    {
                        phoneName = parts[0];
                    }
                }

                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    phone.Name = phoneName;
                    items.Add(phone);
                    UpdateConnector();
                }));
            }
            else
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    // phone reconnected, so start over.
                    if (!phone.Connected)
                    {
                        phone.Allowed = false;
                    }
                }));
            }
        }

        private void OnPhonePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ConnectedPhone phone = (ConnectedPhone)sender;
            if (e.PropertyName == "Allowed" && phone.Allowed)
            {
                // ok, user has given us the green light to connect this phone.
                conmgr.AllowRemoteMachine(phone.IPEndPoint);
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
                // sorry, this phone is not allowed...
                return;
            }

            await this.Dispatcher.BeginInvoke(new Action(() =>
            {
                phone.Connected = true;
                StatusMessage.Text = Properties.Resources.LoadingOutlookContacts;
            }));

            // get new contact info from outlook ready for syncing with this phone.
            await phone.SyncOutlook();

            await this.Dispatcher.BeginInvoke(new Action(() =>
            {
                StatusMessage.Text = string.Format(Properties.Resources.LoadedCount, store.Contacts.Count);
            }));
        }

        private void UpdateConnector()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Connector.Connected = items.Count > 0;
            }));
        }

        protected override void OnClosed(EventArgs e)
        {
            conmgr.StopListening();
            Log.CloseLog();
            base.OnClosed(e);
        }

    }
}
