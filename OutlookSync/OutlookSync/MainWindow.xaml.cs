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
        Random random = new Random();
        ObservableCollection<ConnectedPhone> items = new ObservableCollection<ConnectedPhone>();
        int code;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;

            Log.OpenLog(GetLogFileName());

            if (Debugger.IsAttached)
            {
                code = 111111;
            }
            else
            {
                code = random.Next(100000, 999999);
            }
            CodeText.Text = code.ToString();

            PhoneList.ItemsSource = items;
            conmgr = new ConnectionManager("F657DBF0-AF29-408F-8F4A-B662D7EA4440:" + code, 12777, 12778);
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
            StatusMessage.Text = Properties.Resources.LoadingOutlookContacts;

            conmgr.StartListening();
            conmgr.MessageReceived += OnMessageReceived;
            conmgr.ReadException += OnServerException;
            store = await UnifiedStore.LoadAsync(GetStoreFileName());

            OutlookStoreLoader loader = new OutlookStoreLoader();
            await loader.UpdateAsync(store);

            await store.SaveAsync(GetStoreFileName());

            StatusMessage.Text = string.Format(Properties.Resources.LoadedCount, store.Contacts.Count);
            
            // wait for phone to connect then sync with the phone.
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
            
            var msg = e.Message;
            if (msg.Command == "Disconnect")
            {
                OnDisconnect(e.RemoteEndPoint);
            }
            else if (msg.Command == "Connect")
            {
                OnConnect(e);
            }

            string key = e.RemoteEndPoint.Address.ToString();
            ConnectedPhone phone = null;
            connected.TryGetValue(key, out phone);
            if (phone != null)
            {
                phone.HandleMessage(e);
            }
        }

        private void OnConnect(MessageEventArgs e)
        {
            var msg = e.Message;
            string key = e.RemoteEndPoint.Address.ToString();
            ConnectedPhone phone = null;
            connected.TryGetValue(key, out phone);

            if (phone == null)
            {
                phone = new ConnectedPhone(store, this.Dispatcher);
                connected[key] = phone;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    phone.Name = msg.Parameters;
                    items.Add(phone);
                    UpdateConnector();
                }));
            }
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
