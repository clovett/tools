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
using Microsoft.Networking.SmartSockets;
using System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        SmartSocketClient client;
        

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Status.Text = "Connecting...";

            CancellationTokenSource source = new CancellationTokenSource();
            try
            {
                this.client = await SmartSocketClient.FindServerAsync("SmartSocketTestService", source.Token);
                this.client.Error += OnClientError;
                this.client.MessageReceived += OnMessageReceived;
                Status.Text = "Connected to: " + client.ServerName;
            }
            catch (Exception ex)
            {
                Writemessage(ex.Message);
                Status.Text = "Connect failed";
            }
        }

        private void OnClientError(object sender, Exception e)
        {
            RunOnUiThread(() =>
            {
                Status.Text = e.Message;
            });
        }
        private void OnMessageReceived(object sender, Message e)
        {
            Writemessage("MessageReceived: " + e.Name);
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
            if (e.Key == Windows.System.VirtualKey.Enter && this.client != null)
            {
                string msg = SendText.Text;
                Status.Text = "";
                Writemessage("Sending: " + msg);
                this.client.Send(new Message() { Name = msg });                
                SendText.Text = "";
                e.Handled = true;
            }
        }
    }
}
