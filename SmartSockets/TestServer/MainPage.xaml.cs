using Microsoft.Networking.SmartSockets;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestServer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        SmartSocketListener server;
        public MainPage()
        {
            server = new Microsoft.Networking.SmartSockets.SmartSocketListener();
            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;
            var nowait = server.StartListening("SmartSocketTestService");
            this.InitializeComponent();
        }

        private void OnClientConnected(object sender, SmartSocketClient e)
        {
            Writemessage("ClientConnected: " + e.Name);
            e.MessageReceived += OnMessageReceived;
        }

        private void OnClientDisconnected(object sender, SmartSocketClient e)
        {
            Writemessage("ClientDisconnected: " + e.Name);
            e.MessageReceived -= OnMessageReceived;
        }

        private void OnMessageReceived(object sender, Message e)
        {
            Writemessage("MessageReceived: " + e.Name);
            SmartSocketClient client = (SmartSocketClient)sender;
            client.Send(new Message() { Id = e.Id, Timestamp = e.Timestamp, Name = "Server says: " + e.Name });
        }

        private void Writemessage(string msg)
        {
            RunOnUiThread(() =>
            {
                TestOutput.Document.Selection.SetRange(int.MaxValue, int.MaxValue);
                TestOutput.Document.Selection.SetText(Windows.UI.Text.TextSetOptions.None, msg + "\n");
            });
        }

        private void RunOnUiThread(Action a)
        {
            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                a();
            }));
        }

        private void OnSendTextKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && this.server != null)
            {
                string msg = SendText.Text;
                Status.Text = "";
                Writemessage("Sending: " + msg);
                this.server.Broadcast(new Message() { Name = msg });
                SendText.Text = "";
                e.Handled = true;
            }
        }
    }
}
