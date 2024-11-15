﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Microsoft.Networking
{
    public class ServerEventArgs : EventArgs
    {
        public ServerProxy Server { get; set; }
    }

    public class NoPortsAvailableException : Exception
    {
    }

    public class ConnectionManager
    {
        int m_portNumber;
        string m_udpMessage;
        string m_udpServerMessage;
        bool stopped;

        CancellationTokenSource cancellationSource;
        ManualResetEvent cancelled = new ManualResetEvent(false);
        Dictionary<string, DatagramSocket> sockets = new Dictionary<string, DatagramSocket>();
        ServerProxy server;

        public event EventHandler<ServerEventArgs> ServerFound;
        public event EventHandler<ServerExceptionEventArgs> ServerLost;

        public ConnectionManager(string applicationId, string clientName)
        {
            m_udpMessage = "Client:" + applicationId + ":" + clientName;
            m_udpServerMessage = "Server:" + applicationId;
        }

        // cannot be completely random, the PC app has to use the same seed.
        const int randomSeed = 1980527718;

        IEnumerable<int> GetRandomPortNumber()
        {
            // this is what the original phone app is expecting in case user still has old version.
            yield return 12777;

            Random r = new Random(randomSeed);

            // according to wikipedia ports 1024 to 49151 are available
            const int min = 1024;
            const int max = 49151;
            int count = 0;

            while (count++ < 20)
            {
                int port = r.Next(min, max);
                if (port != 12777)
                {
                    yield return port;
                }
            }
            yield return 0;
        }

        public void Start()
        {
            stopped = false;
            if (server != null)
            {
                server.Stop();
            }
            if (cancellationSource == null || cancellationSource.IsCancellationRequested)
            {
                cancellationSource = new CancellationTokenSource();
                var result = Task.Run(new Action(FindServer));
            }
        }

        HashSet<string> localAddresses = new HashSet<string>();
        bool findingServer;

        private void FindServer()
        {
            if (this.sockets.Count > 0)
            {
                // how could this be?
                // clear out the old sockets in case they are disposed...
                StopFindingServer();
            }

            findingServer = true;
            try
            {
                var cancellationToken = cancellationSource.Token;
                while (!cancellationToken.IsCancellationRequested)
                {
                   // send out the UDP ping every few seconds, searching each port number in sequence till we find the one
                   // the server is listening on.
                                        
                    Guid adapter = Guid.Empty;
                    foreach (var hostName in Windows.Networking.Connectivity.NetworkInformation.GetHostNames())
                    {
                        if (hostName.IPInformation != null)
                        {
                            // blast it out on all local hosts, since user may be connected to the network in multiple ways
                            // and some of these hostnames might be virtual ethernets that go no where, like 169.254.80.80.
                            try
                            {
                                if (hostName.IPInformation.NetworkAdapter != null && !hostName.CanonicalName.StartsWith("169."))
                                {
                                    localAddresses.Add(hostName.CanonicalName);

                                    foreach (int port in GetRandomPortNumber())
                                    {
                                        if (port == 0)
                                        {
                                            break;
                                        }
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            break;
                                        }
                                        // give each UDP socket it's own thread (doesn't work otherwise)
                                        Task.Run(new Action(() =>
                                        {
                                            SendUdpPing(hostName, port).Wait();
                                        }));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Ping failed: " + ex.Message);
                            }
                        }
                    }
                    try
                    {
                        // give each ping time to answer...
                        Task.Delay(5000).Wait(cancellationToken);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            cancelled.Set();
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            finally
            {
                findingServer = false;
            }
        }

        int udpPing;

        private async Task SendUdpPing(HostName hostName, int portNumber)
        {
            string ipAddress = hostName.CanonicalName + ":" + portNumber;
            DatagramSocket socket;
            lock (sockets)
            {
                sockets.TryGetValue(ipAddress, out socket);
            }
            if (socket == null)
            {
                // setup the socket for this network adapter.
                socket = new DatagramSocket();
                socket.MessageReceived += OnDatagramMessageReceived;
                lock (sockets)
                {
                    sockets[ipAddress] = socket;
                }
                await socket.BindEndpointAsync(hostName, portNumber.ToString());
            }

            using (IOutputStream os = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), portNumber.ToString()))
            {
                DataWriter writer = new DataWriter(os);
                byte[] bytes = UTF8Encoding.UTF8.GetBytes(m_udpMessage + "/" + ipAddress );
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                Debug.WriteLine("Sent UDP ping " + udpPing++ + " to " + ipAddress);
            }
            
        }

        private void OnDatagramMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {            
            var remoteHost = args.RemoteAddress.CanonicalName;

            // don't receive our own datagrams...
            if (!localAddresses.Contains(remoteHost))
            {
                Debug.WriteLine("OnDatagramMessageReceived from " + remoteHost + " on port " + args.RemotePort);

                var reader = args.GetDataReader();
                uint bytesRead = reader.UnconsumedBufferLength;

                byte[] data = new byte[bytesRead];
                reader.ReadBytes(data);

                string s = UTF8Encoding.UTF8.GetString(data, 0, data.Length);

                if (s.StartsWith(m_udpServerMessage))
                {
                    m_portNumber = int.Parse(args.RemotePort);

                    string[] addresses = s.Substring(m_udpServerMessage.Length + 1).Split('/');

                    var nowait = TryConnectServer(addresses);
                }
            }

        }

        public static bool TryParseEndPoint(string address, out IPEndPoint ep)
        {
            ep = null;
            int i = address.IndexOf(':');
            if (i > 0)
            {
                string ipaddress = address.Substring(0, i);
                string port = address.Substring(i + 1);
                IPAddress addr = null;
                if (int.TryParse(port, out i) && IPAddress.TryParse(ipaddress, out addr))
                {
                    ep = new IPEndPoint(addr, i);
                    return true;
                }
            }
            return false;
        }

        public async Task TryConnectServer(string[] addresses, bool forceReconnect = false)
        {
            if (server == null || forceReconnect)
            {
                foreach (string address in addresses)
                {
                    try
                    {
                        IPEndPoint ep;
                        if (TryParseEndPoint(address, out ep))
                        {
                            Debug.WriteLine("Trying to connect to server: " + address);
                            server = new ServerProxy();
                            await server.ConnectAsync(ep);
                            Debug.WriteLine("Connected to server : " + address);
                            server.ReadException += OnServerReadException;
                            if (ServerFound != null)
                            {
                                ServerFound(this, new ServerEventArgs() { Server = server });
                            }

                            StopFindingServer();
                        }
                        return; // done!
                    }
                    catch
                    {
                        if (!forceReconnect)
                        {
                            server = null;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                
            }
        }

        private void OnServerReadException(object sender, ServerExceptionEventArgs e)
        {
            if (!stopped)
            {
                this.server = null;
                Start();
                if (ServerLost != null)
                {
                    ServerLost(sender, e);
                }
            }
        }

        public void Stop()
        {
            stopped = true;
            server = null;
            StopFindingServer();
        }

        void StopFindingServer()
        {
            if (cancellationSource != null && findingServer)
            {
                cancellationSource.Cancel();                
                // wait for FindDevices to terminate so we can tear down the sockets cleanly.
                cancelled.WaitOne(5000);
            }
            lock (sockets)
            {
                foreach (var socket in sockets.Values)
                {
                    // dispose the socket.
                    using (socket)
                    {
                    }
                }
                sockets.Clear();
            }
        }

    }

}
