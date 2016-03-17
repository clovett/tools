using Microsoft.Networking.SmartSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace TestClientDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SmartSocketClient client;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnMainWindowLoaded;
        }

        private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            Status.Text = "Connecting...";
            SendText.Focus();

            CancellationTokenSource source = new CancellationTokenSource();
            try
            {
                this.client = await SmartSocketClient.FindServerAsync("SmartSocketTestService", source.Token);
                this.client.Error += OnClientError;
                this.client.MessageReceived += OnMessageReceived;                                
                
                if (this.client == null)
                {
                    Status.Text = "Could not find server, please ensure you're ethernet is turned on";
                }
                else
                {
                    Status.Text = "Connected to: " + client.ServerName;
                }
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
            Writemessage("ReplyReceived: " + e.Name);
        }

        private void Writemessage(string msg)
        {
            RunOnUiThread(() =>
            {
                if (TestOutput.Document.Blocks.Count == 0)
                {
                    TestOutput.Document.Blocks.Add(new Paragraph());
                }
                Paragraph p = (Paragraph)TestOutput.Document.Blocks.FirstBlock;
                p.Inlines.Add(new Run() { Text = msg });
                p.Inlines.Add(new LineBreak());
            });
        }

        private void RunOnUiThread(Action a)
        {
            Dispatcher.Invoke(a);
        }


        private void OnSendTextKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && this.client != null)
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
