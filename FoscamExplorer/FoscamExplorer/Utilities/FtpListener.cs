using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace FoscamExplorer
{
    class FtpListener
    {
        List<FtpClient> clients = new List<FtpClient>();
        StreamSocketListener listener;
        public async void Start()
        {
            listener = new StreamSocketListener();
            listener.ConnectionReceived += OnConnectionReceived;
            await listener.BindEndpointAsync(new HostName("192.168.1.116"), "21");
        }

        void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            clients.Add(new FtpClient(this, args.Socket));
        }

        public void Stop()
        {
            foreach (var c in clients)
            {
                c.Stop();
            }
        }
    }

    class FtpClient
    {
        FtpListener server;
        StreamSocket socket;
        DataReader reader;
        DataWriter writer;

        public FtpClient(FtpListener server, StreamSocket socket)
        {
            this.server = server;
            this.socket = socket;

            this.reader = new DataReader(socket.InputStream);
            this.writer = new DataWriter(socket.OutputStream);

            StringBuilder command = new StringBuilder();

            while (this.reader != null)
            {
                byte b = this.reader.ReadByte();
                if (b != '\r' && b != '\n')
                {
                    command.Append(Convert.ToChar(b));
                }
                else
                {
                    string cmd = command.ToString();
                    command.Length = 0;
                    ExecuteCommand(cmd);
                }
            }
        }

        private void ExecuteCommand(string cmd)
        {
            Debug.WriteLine(cmd);
        }

        public void Stop()
        {
            reader = null;
            writer = null;
            using (socket)
            {
                socket = null;
            }
        }
    }
}
