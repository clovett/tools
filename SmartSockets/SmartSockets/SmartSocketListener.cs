using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using System.IO;
using Windows.Networking;
using System.Net;

namespace Microsoft.Networking.SmartSockets
{
    /// <summary>
    /// This class provides an auto-reconnecting stream socket server for simple bi-direction
    /// message passing.  
    /// </summary>
    public sealed class SmartSocketListener
    {
        bool connected;
        NetworkAdapter _adapter;
        string _localAddress;
        StreamSocketListener listener;
        List<SmartSocketClient> clients = new List<SmartSocketClient>();
        string serviceName;

        public SmartSocketListener()
        {
            NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
        }

        public void Close()
        {
            SmartSocketClient[] snapshot = null;
            lock (this.clients)
            {
                snapshot = this.clients.ToArray();
                this.clients.Clear();
            }
            foreach (SmartSocketClient client in snapshot)
            {
                client.Close();
            }
        }

        async void OnNetworkStatusChanged(object sender)
        {
            if (!connected)
            {
                await CheckNetworkProfiles();
            }
        }

        internal static HostName GetLocalHostName(out NetworkAdapter adapter)
        {
            adapter = null;
            var inetProfile = NetworkInformation.GetInternetConnectionProfile();
            if (inetProfile != null)
            {
                foreach (var name in NetworkInformation.GetHostNames())
                {
                    var ipinfo = name.IPInformation;
                    // Phone client is broadcasting to ipv4.
                    if (ipinfo != null && name.Type == Windows.Networking.HostNameType.Ipv4)
                    {
                        if (ipinfo.NetworkAdapter.NetworkAdapterId == inetProfile.NetworkAdapter.NetworkAdapterId)
                        {
                            adapter = ipinfo.NetworkAdapter;
                            return name;
                        }
                    }
                }
            }
            return null;
        }

        async Task CheckNetworkProfiles()
        {
            HostName found = GetLocalHostName(out _adapter);
            if (found != null)
            {
                string ipAddress = found.CanonicalName;
                _localAddress = ipAddress;
                if (AdapterFound != null)
                {
                    AdapterFound(this, ipAddress);
                }

                this.listener = new StreamSocketListener();
                this.listener.ConnectionReceived += OnConnectionReceived;
                await listener.BindServiceNameAsync(""); // empty string means pick any available local port.

                // also listen for UDP datagrams.
                StartDatagramListener(_adapter);

                connected = true;
            }
        }

        public void Broadcast(Message message)
        {
            SmartSocketClient[] snapshot = null;
            lock(this.clients)
            {
                snapshot = this.clients.ToArray();
            }
            foreach (var client in snapshot)
            {
                client.Send(message);
            }
        }

        /// <summary>
        /// Address for UDP group.
        /// </summary>
        internal static IPAddress GroupAddress = IPAddress.Parse("226.10.10.2");

        /// <summary>
        /// Port used for UDP broadcasts.
        /// </summary>
        internal static int GroupPort = 37992;

        DatagramSocket dgramSocket;

        private async void StartDatagramListener(NetworkAdapter adapter)
        {
            dgramSocket = new DatagramSocket();
            dgramSocket.MessageReceived += OnDatagramMessageReceived;
            await dgramSocket.BindServiceNameAsync(GroupPort.ToString(), adapter);
            dgramSocket.JoinMulticastGroup(new HostName(GroupAddress.ToString()));
        }

        private async void OnDatagramMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            HostName localHost = GetLocalHostName(out _adapter);
            using (var stream = args.GetDataStream().AsStreamForRead())
            {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    uint len = reader.ReadUInt32();
                    string msg = reader.ReadString();

                    if (msg == serviceName)
                    {
                        // ooh, this is for us then, so let's respond
                        DatagramSocket udpSocket = new DatagramSocket();
                        await udpSocket.ConnectAsync(args.RemoteAddress, args.RemotePort);
                        using (var output = udpSocket.OutputStream.AsStreamForWrite())
                        {
                            string addr = listener.Information.LocalPort;
                            using (BinaryWriter writer = new BinaryWriter(output, Encoding.UTF8, true))
                            {
                                writer.Write(addr.Length);
                                writer.Write(addr);
                            }
                        }
                    }
                }
            }
        }

        private void OnReceiveDatagram(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var phoneClient = args.RemoteAddress;
            var phonePort = args.RemotePort;
        }

        public event EventHandler<SmartSocketClient> ClientConnected;
        public event EventHandler<SmartSocketClient> ClientDisconnected;
        public event EventHandler<string> AdapterFound;

        public async Task StartListening(string serviceName)
        {
            this.serviceName = serviceName;
            await CheckNetworkProfiles();
        }

        void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var socket = args.Socket;
            var client = new SmartSocketClient(this, socket);

            client.Name = socket.Information.RemoteHostName.CanonicalName;
            client.ServerName = socket.Information.LocalAddress.CanonicalName;
            lock (this.clients)
            {
                this.clients.Add(client);
            }
            OnClientConnected(client);
        }
        
        private void OnClientDisconnected(SmartSocketClient client)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, client);
            }
        }

        private void OnClientConnected(SmartSocketClient client)
        {
            if (ClientConnected != null)
            {
                ClientConnected(this, client);
            }
        }

        internal void RemoveClient(SmartSocketClient client)
        {
            lock (this.clients)
            {
                this.clients.Remove(client);
            }

            OnClientDisconnected(client);
        }
    }
}
