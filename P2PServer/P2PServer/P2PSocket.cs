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
        Client self; // contains our public address.
        Client remote;
        string hostname;
        List<Socket> sockets = new List<Socket>();
        IPEndPoint localAddress;
        bool closed;
        bool connected;

        public P2PSocket()
        {
        }

        public void Close()
        {
            closed = true;
            foreach (var s in this.sockets)
            {
                try
                {
                    s.Close();
                } catch { }
            }
        }

        public void ListenAsync()
        {
            // Task.Factory.StartNew(new Action(() => Listen(true))); // this is just so that P2P works when both apps are inside the same NAT.
            Task.Factory.StartNew(new Action(() => Listen(false))); 
        }

        public void P2PConnectAsync()
        {
            if (this.remote == null)
            {
                throw new Exception("Please call FindEndPoint first");
            }
            //Task.Factory.StartNew(new Action(() => P2PConnect(true))); // this is just so that P2P works when both apps are inside the same NAT.
            Task.Factory.StartNew(new Action(() => P2PConnect(false)));
        }

        private void Listen(bool useLocal)
        {
            int port;
            if (useLocal)
            {
                port = int.Parse(self.LocalPort);
            } 
            else
            {
                port = int.Parse(self.RemotePort); // remote.RemotePort);
            }
            Console.WriteLine("Listening for connections on port: {0}", port);
            while (!closed && !connected) {
                var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                IPEndPoint listenAny = new IPEndPoint(IPAddress.Any, port);
                server.Bind(listenAny); // use the same address we used to connect to the host so that we listen on the same port.
                server.Listen(1);

                var socket = server.Accept();

                Close(); // close other sockets
                this.sockets.Add(server);
                Console.WriteLine("We have accepted a socket!!");
                byte[] buffer = new byte[1000];
                int len = socket.Receive(buffer);
                string s = System.Text.Encoding.UTF8.GetString(buffer, 0, len);
                Console.WriteLine("message received: " + s);
                socket.Close();
            }
        }

        private void P2PConnect(bool useLocal)
        {
            IPEndPoint remoteEndPoint;
            if (useLocal)
            {
                Console.WriteLine("Connecting to remote server at {0} port: {1}", remote.LocalAddress, remote.LocalPort);
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(remote.LocalAddress), int.Parse(remote.LocalPort));
            } 
            else
            {
                int port = int.Parse(remote.RemotePort);
                Console.WriteLine("Connecting to remote server at {0} port: {1}", remote.RemoteAddress, port);
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(remote.RemoteAddress), port);
            }

            while (!closed) 
            {
                try
                {
                    var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                    client.Bind(localAddress); // use the same address we used to connect to the host so that we listen on the same port.
                    
                    client.Connect(remoteEndPoint);
                    connected = true;

                    Close();

                    this.sockets.Add(client);

                    client.Send(System.Text.Encoding.UTF8.GetBytes("Hello World!!"));
                    Console.WriteLine("We have a connected socket!!");
                    client.Close();
                } 
                catch (Exception ex)
                {
                    // try again!
                    Console.WriteLine(ex.Message);
                }
                System.Threading.Thread.Sleep(1000);
            }
        }
        
        public void Connect(string hostname, int port = 80)
        {
            this.hostname = hostname;
            var entry = System.Net.Dns.GetHostEntry(hostname);
            var addr = (from a in entry.AddressList where a.AddressFamily == AddressFamily.InterNetwork select a).FirstOrDefault();
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.sockets.Add(socket);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            socket.Connect(new IPEndPoint(addr.Address, port));
            this.localAddress = (IPEndPoint)socket.LocalEndPoint;
            socket.Close();
        }

        public void PublishEndPoint(string name)
        {
            if (this.localAddress == null)
            {
                throw new Exception("Please call Ping first to estabilish your local IP address and port.");
            }
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
                        this.self = (Client)JsonConvert.DeserializeObject(result, typeof(Client));
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

        public void RemoveEndPoint(string name)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://lovettsoftware.com/p2pserver.aspx?type=delete");
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
