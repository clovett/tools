using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Networking.SmartSockets
{
    public class SmartSocketListener
    {
        private int port;
        private bool stopped;
        private Socket listener;
        private string serviceName;
        private List<SmartSocketClient> clients = new List<SmartSocketClient>();

        public event EventHandler<SmartSocketClient> ClientConnected;
        public event EventHandler<SmartSocketClient> ClientDisconnected;

        /// <summary>
        /// Start listening for connections from anyone.
        /// </summary>
        /// <returns>Returns the port number we are listening on (assigned by the system)</returns>
        public int StartListening(string serviceName)
        {
            this.serviceName = serviceName;

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            listener.Bind(ep);

            IPEndPoint ip = listener.LocalEndPoint as IPEndPoint;
            this.port = ip.Port;
            listener.Listen(10);

            // now start a background thread to process incoming requests.
            Task task = new Task(new Action(Run));
            task.Start();

            task = new Task(new Action(UdpListener));
            task.Start();

            return this.port;
        }

        /// <summary>
        /// Address for UDP group.
        /// </summary>
        internal static IPAddress GroupAddress = IPAddress.Parse("226.10.10.2");

        /// <summary>
        /// Port used for UDP broadcasts.
        /// </summary>
        internal static int GroupPort = 37992;

        UdpClient udpListener;

        private void UdpListener()
        {
            var localHost = SmartSocketClient.FindLocalHostName();
            List<string> addresses = SmartSocketClient.FindLocalIpAddresses();
            if (localHost == null || addresses.Count == 0)
            {
                return; // no network.
            }

            IPEndPoint remoteEP = new IPEndPoint(GroupAddress, GroupPort);
            udpListener = new UdpClient(GroupPort);
            udpListener.JoinMulticastGroup(GroupAddress);
            while (true)
            {
                byte[] data = udpListener.Receive(ref remoteEP);
                if (data != null)
                {
                    BinaryReader reader = new BinaryReader(new MemoryStream(data));
                    int len = reader.ReadInt32();
                    string msg = reader.ReadString();
                    if (msg == this.serviceName)
                    {
                        // send response back with info on how to connect to this server.
                        IPEndPoint localEp = (IPEndPoint)listener.LocalEndPoint;
                        string addr = localEp.Port.ToString();
                        MemoryStream ms = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(ms);
                        writer.Write(addr.Length);
                        writer.Write(addr);
                        writer.Flush();
                        byte[] buffer = ms.ToArray();
                        udpListener.Send(buffer, buffer.Length, remoteEP);
                    }
                }
            }
        }

        public void Broadcast(Message message)
        {
            SmartSocketClient[] snapshot = null;
            lock (this.clients)
            {
                snapshot = this.clients.ToArray();
            }
            foreach (var client in snapshot)
            {
                client.Send(message);
            }
        }

        /// <summary>
        /// The port we are listening to.  The clients need to know this port so it defaults to 3921.
        /// </summary>
        public int Port { get { return this.port; } }

        /// <summary>
        /// Call this method on a background thread to listen to our port.
        /// </summary>
        internal void Run()
        {
            while (!stopped)
            {
                try
                {
                    Socket client = listener.Accept();
                    OnAccept(client);
                }
                catch (Exception)
                {
                    // listener was probably closed then, which means we've probably been stopped.
                    Debug.WriteLine("Listener is gone");
                }
            }
        }

        ManualResetEvent messageReceived = new ManualResetEvent(false);

        private void OnAccept(Socket client)
        {
            IPEndPoint ep1 = client.RemoteEndPoint as IPEndPoint;
            SmartSocketClient proxy = new SmartSocketClient(this, client)
            {
                Name = ep1.Address.ToString(),
                ServerName = SmartSocketClient.FindLocalHostName()
            };
            SmartSocketClient[] snapshot = null;

            lock (clients)
            {
                snapshot = clients.ToArray();
            }

            foreach (SmartSocketClient s in snapshot)
            {
                IPEndPoint ep2 = s.Socket.RemoteEndPoint as IPEndPoint;
                if (ep1 == ep2)
                {
                    // can only have one client using this end point.
                    RemoveClient(s);
                }
            }

            lock (clients)
            { 
                this.clients.Add(proxy);
            }

            if (ClientConnected != null)
            {
                ClientConnected(this, proxy);
            }
        }

        internal void RemoveClient(SmartSocketClient client)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, client);
            }
            lock (this.clients)
            {
                clients.Remove(client);
            }
        }

        /// <summary>
        /// Call this method to stop the background thread, it is good to do this before your app shuts down.
        /// This will also send a Disconnect message to all the clients so they know the server is gone.
        /// </summary>
        public void Stop()
        {
            stopped = true;
            using (listener)
            {
                try
                {
                    listener.Close();
                }
                catch { }
            }
            listener = null;

            SmartSocketClient[] snapshot = null;
            lock (clients)
            {
                snapshot = clients.ToArray();
            }

            foreach (SmartSocketClient client in snapshot)
            {
                client.Close();
            }

            lock (clients)
            {
                clients.Clear();
            }
        }
        
    }
}
