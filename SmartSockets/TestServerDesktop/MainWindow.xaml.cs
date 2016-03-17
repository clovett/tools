using Microsoft.Networking.SmartSockets;
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

namespace TestServerDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SmartSocketListener server;
        public MainWindow()
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
            e.Error += OnClientError;
        }

        private void OnClientError(object sender, Exception e)
        {
            Writemessage(e.Message);
        }

        private void OnClientDisconnected(object sender, SmartSocketClient e)
        {
            Writemessage("ClientDisconnected: " + e.Name);
            e.MessageReceived -= OnMessageReceived;
            e.Error -= OnClientError;
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
            if (e.Key == Key.Enter && this.server != null)
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