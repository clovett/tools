// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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

namespace LovettSoftware.Networking.SmartSockets
{
    /// <summary>
    /// This class sets up a UDP broadcaster so clients on the same network can find the server by
    /// a given string name, no fussing about with ip addresses and ports.  It then listens for
    /// new clients to connect and spins off ClientConnected messages so the app can process the
    /// server side of each conversation.  So the application server then can handle any number of
    /// clients at the same time, each client will have their own SmartSocket on different ports.
    /// If the client goes away, the ClientDisconnected event is raised so the server can cleanup.
    /// </summary>
    public class SmartSocketServer
    {
        private int port;
        private bool stopped;
        private Socket listener;
        private readonly string serviceName;
        private readonly IPAddress ipAddress;
        private readonly List<SmartSocket> clients = new List<SmartSocket>();
        private readonly SmartSocketTypeResolver resolver;
        private UdpClient udpListener;

        public event EventHandler<SmartSocket> ClientConnected;

        public event EventHandler<SmartSocket> ClientDisconnected;

        public SmartSocketServer(string name, SmartSocketTypeResolver resolver, string ipAddress = "127.0.0.1")
        {
            this.serviceName = name;
            this.resolver = resolver;
            this.ipAddress = IPAddress.Parse(ipAddress);
        }

        /// <summary>
        /// Start listening for connections from anyone.
        /// </summary>
        /// <returns>Returns the port number we are listening on (assigned by the system)</returns>
        public int StartListening()
        {
            this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(this.ipAddress, 0);
            this.listener.Bind(ep);

            IPEndPoint ip = this.listener.LocalEndPoint as IPEndPoint;
            this.port = ip.Port;
            this.listener.Listen(10);

            // now start a background thread to process incoming requests.
            Task task = new Task(new Action(this.Run));
            task.Start();

            task = new Task(new Action(this.UdpListener));
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

        private void UdpListener()
        {
            var localHost = SmartSocket.FindLocalHostName();
            List<string> addresses = SmartSocket.FindLocalIpAddresses();
            if (localHost == null || addresses.Count == 0)
            {
                return; // no network.
            }

            IPEndPoint remoteEP = new IPEndPoint(GroupAddress, GroupPort);
            this.udpListener = new UdpClient(GroupPort);
            this.udpListener.JoinMulticastGroup(GroupAddress);
            while (true)
            {
                byte[] data = this.udpListener.Receive(ref remoteEP);
                if (data != null)
                {
                    BinaryReader reader = new BinaryReader(new MemoryStream(data));
                    int len = reader.ReadInt32();
                    string msg = reader.ReadString();
                    if (msg == this.serviceName)
                    {
                        // send response back with info on how to connect to this server.
                        IPEndPoint localEp = (IPEndPoint)this.listener.LocalEndPoint;
                        string addr = localEp.ToString();
                        MemoryStream ms = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(ms);
                        writer.Write(addr.Length);
                        writer.Write(addr);
                        writer.Flush();
                        byte[] buffer = ms.ToArray();
                        this.udpListener.Send(buffer, buffer.Length, remoteEP);
                    }
                }
            }
        }

        public async Task BroadcastAsync(SocketMessage message)
        {
            SmartSocket[] snapshot = null;
            lock (this.clients)
            {
                snapshot = this.clients.ToArray();
            }

            foreach (var client in snapshot)
            {
                await client.SendAsync(message);
            }
        }

        /// <summary>
        /// The port we are listening to.  The clients need to know this port so it defaults to 3921.
        /// </summary>
        public int Port => this.port;

        /// <summary>
        /// Call this method on a background thread to listen to our port.
        /// </summary>
        internal void Run()
        {
            while (!this.stopped)
            {
                try
                {
                    Socket client = this.listener.Accept();
                    this.OnAccept(client);
                }
                catch (Exception)
                {
                    // listener was probably closed then, which means we've probably been stopped.
                    Debug.WriteLine("Listener is gone");
                }
            }
        }

        private void OnAccept(Socket client)
        {
            IPEndPoint ep1 = client.RemoteEndPoint as IPEndPoint;
            SmartSocket proxy = new SmartSocket(this, client, this.resolver)
            {
                Name = ep1.ToString(),
                ServerName = SmartSocket.FindLocalHostName()
            };

            proxy.Disconnected += this.OnClientDisconnected;

            SmartSocket[] snapshot = null;

            lock (this.clients)
            {
                snapshot = this.clients.ToArray();
            }

            foreach (SmartSocket s in snapshot)
            {
                IPEndPoint ep2 = s.Socket.RemoteEndPoint as IPEndPoint;
                if (ep1 == ep2)
                {
                    // can only have one client using this end point.
                    this.RemoveClient(s);
                }
            }

            lock (this.clients)
            {
                this.clients.Add(proxy);
            }

            if (this.ClientConnected != null)
            {
                this.ClientConnected(this, proxy);
            }
        }

        private void OnClientDisconnected(object sender, EventArgs e)
        {
            SmartSocket client = (SmartSocket)sender;
            this.RemoveClient(client);
        }

        internal void RemoveClient(SmartSocket client)
        {
            bool found = false;
            lock (this.clients)
            {
                found = this.clients.Contains(client);
                this.clients.Remove(client);
            }

            if (found && this.ClientDisconnected != null)
            {
                this.ClientDisconnected(this, client);
            }
        }

        /// <summary>
        /// Call this method to stop the background thread, it is good to do this before your app shuts down.
        /// This will also send a Disconnect message to all the clients so they know the server is gone.
        /// </summary>
        public void Stop()
        {
            this.stopped = true;
            using (this.listener)
            {
                try
                {
                    this.listener.Close();
                }
                catch (Exception)
                {
                }
            }

            this.listener = null;

            SmartSocket[] snapshot = null;
            lock (this.clients)
            {
                snapshot = this.clients.ToArray();
            }

            foreach (SmartSocket client in snapshot)
            {
                client.Close();
            }

            lock (this.clients)
            {
                this.clients.Clear();
            }
        }
    }
}
