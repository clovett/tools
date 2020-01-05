using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using P2PLibrary;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace P2PServer
{
    /// <summary>
    /// This class connects a socket to the given host for the purpose of hole punching
    /// to create a p2p socket with a similar client running on another machine behind
    /// a different NAT.
    /// </summary>
    class P2PSocket
    {
        Client remote;
        string hostname;
        Socket socket;
        Socket server;
        Socket client;
        bool closed;

        public P2PSocket()
        {
        }

        public void Close()
        {
            closed = true;
            if (server != null)
            {
                server.Close();
            }
            if (socket != null)
            {
                socket.Close();
            }
        }

        public Task ListenAsync()
        {
            return Task.Factory.StartNew(Listen);
        }

        public Task P2PConnectAsync()
        {
            if (this.remote == null)
            {
                throw new Exception("Please call FindEndPoint first");
            }
            return Task.Factory.StartNew(P2PConnect);
        }

        private void Listen()
        {
            IPEndPoint localAddress = (IPEndPoint)this.socket.LocalEndPoint;
            Console.WriteLine("Listening for connections on port: {0}", localAddress.Port);
            this.server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            server.Bind(localAddress); // use the same address we used to connect to the host so that we listen on the same port.
            server.Listen(1);
            while (!closed) {
                var socket = server.Accept();
                Console.WriteLine("We have an accepted socket!!");
                socket.Close();
            }
        }

        private void P2PConnect()
        {
            Console.WriteLine("Connecting to remote server at {0} port: {1}", remote.RemoteAddress, remote.RemotePort);
            IPEndPoint localAddress = (IPEndPoint)this.socket.LocalEndPoint;
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remote.RemoteAddress), int.Parse(remote.RemotePort));
            this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            client.Bind(localAddress); // use the same address we used to connect to the host so that we listen on the same port.
            while (!closed) 
            {
                try
                {
                    server.Connect(remoteEndPoint);
                    Console.WriteLine("We have a connected socket!!");
                    socket.Close();
                } 
                catch (Exception)
                {
                    // try again!
                }
            }
        }
        
        public void Connect(string hostname, int port = 80)
        {
            this.hostname = hostname;
            var entry = System.Net.Dns.GetHostEntry(hostname);
            var addr = (from a in entry.AddressList where a.AddressFamily == AddressFamily.InterNetwork select a).FirstOrDefault();
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            this.socket.Connect(new IPEndPoint(addr.Address, port));
        }

        public void PublishEndPoint(string name)
        {
            if (this.socket == null)
            {
                throw new Exception("Please call Connect first");
            }
            IPEndPoint localAddress = (IPEndPoint)this.socket.LocalEndPoint;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://lovettsoftware.com/p2pserver.aspx?type=add");
            request.Method = "POST";
            request.ContentType = "text/json";
            using (Stream s = request.GetRequestStream())
            {
                Client c = new Client()
                {
                    Date = DateTime.Now.ToUniversalTime(),
                    Name = name,
                    LocalAddress = localAddress.Address.ToString(),
                    LocalPort = localAddress.Port.ToString()
                };

                string json = JsonConvert.SerializeObject(c);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                s.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using (var rs = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(rs, Encoding.UTF8))
                    {
                        string result = reader.ReadToEnd();
                        Console.WriteLine(result);
                    }
                }
            }
            else
            {
                Console.WriteLine("Request failed: " + response.StatusDescription);
            }
        }

        public Client FindEndPoint(string name)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://lovettsoftware.com/p2pserver.aspx?type=find");
            request.Method = "POST";
            request.ContentType = "text/json";
            using (Stream s = request.GetRequestStream())
            {

                Client c = new Client()
                {
                    Date = DateTime.Now.ToUniversalTime(),
                    Name = name
                };

                string json = JsonConvert.SerializeObject(c);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                s.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using (var rs = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(rs, Encoding.UTF8))
                    {
                        string result = reader.ReadToEnd();
                        if (result.Contains("\"error\":"))
                        {
                            throw new Exception(result);
                        }
                        this.remote = (Client)JsonConvert.DeserializeObject(result, typeof(Client));
                        return this.remote;
                    }
                }
            }
            else
            {
                throw new Exception("Request failed: " + response.StatusDescription);
            }
        }

    }
}
