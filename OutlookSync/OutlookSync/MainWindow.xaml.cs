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

namespace OutlookSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ConnectionManager conmgr = new ConnectionManager(Guid.Parse("F657DBF0-AF29-408F-8F4A-B662D7EA4440"), 12777, 12778);
        UnifiedStore store;
        const string StoreFolder = @"LovettSoftware\OutlookSync";
        const string StoreFileName = "store.xml";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        string GetStoreFileName()
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StoreFolder);
            string file = System.IO.Path.Combine(dir, StoreFileName);
            return file;
        }

        async void OnLoaded(object sender, RoutedEventArgs e)
        {
            conmgr.StartListening();
            conmgr.MessageReceived += OnMessageReceived;
            store = await UnifiedStore.LoadAsync(GetStoreFileName());

            OutlookStoreLoader loader = new OutlookStoreLoader();
            await loader.LoadAsync(store);

            await store.SaveAsync(GetStoreFileName());

            // wait for phone to connect then sync with the phone.
        }


        void OnMessageReceived(object sender, MessageEventArgs e)
        {
            Debug.WriteLine("Message received from " + e.RemoteEndPoint.ToString());
            Debug.WriteLine("  Command: " + e.Message.Command);
            
            Message m = e.Message;
            switch (m.Command)
            {
                case "GetContact":

                    int contactIndex = 0;                    
                    if (int.TryParse(m.Parameters, out contactIndex) && contactIndex < store.Contacts.Count)
                    {
                        string xml = store.Contacts[contactIndex++].ToXml();
                        e.Response = new Message() { Command = "Contact", Parameters = xml };
                    }
                    else
                    {
                        e.Response = new Message() { Command = "Done" };
                    }
                    break;
                default:
                    Debug.WriteLine("Unrecognized command");
                    break;
            }

        }

        protected override void OnClosed(EventArgs e)
        {
            conmgr.StopListening();
            base.OnClosed(e);
        }


    }
}
