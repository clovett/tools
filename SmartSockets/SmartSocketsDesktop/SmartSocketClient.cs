using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Networking.SmartSockets
{
    public class MessageEventArgs : EventArgs
    {
        Message msg;

        public MessageEventArgs(Message msg)
        {
            this.msg = msg;
        }

        public Message Message { get { return this.msg; } }
    }

    public delegate Message MessageFactory();

    /// <summary>
    /// This class represents one client that we are talking to, and it shows up in the "sender" parameter on
    /// the MessageReceived event.
    /// </summary>
    public class SmartSocketClient
    {
        Socket client;
        NetworkStream socketStream;
        SmartSocketListener server;
        bool disconnected;
        bool receiving;
        EventHandler<Message> messageHandler;


        internal SmartSocketClient(SmartSocketListener server, Socket client)
        {
            this.client = client;
            this.socketStream = new NetworkStream(client);
            this.server = server;
            client.NoDelay = true;
        }

        internal Socket Socket  { get { return this.client; } }

        public string Name { get; set; }

        /// <summary>
        /// Find a SmartSocketListener on the local network using UDP broadcast.
        /// </summary>
        /// <returns>The connected client or null if task is cancelled.</returns>
        public static Task<SmartSocketClient> FindServerAsync(string serviceName, CancellationToken token)
        {
            return Task.Run(() =>
            {
                string localHost = FindLocalHostName();
                if (localHost == null)
                {
                    return null;
                }
                while (!token.IsCancellationRequested)
                {
                    IPEndPoint remoteEP = new IPEndPoint(SmartSocketListener.GroupAddress, SmartSocketListener.GroupPort);
                    UdpClient udpClient = new UdpClient(0);
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(serviceName.Length);
                    writer.Write(serviceName);
                    byte[] bytes = ms.ToArray();
                    udpClient.Send(bytes, bytes.Length, remoteEP);

                    CancellationTokenSource receiveTaskSource = new CancellationTokenSource();
                    Task<UdpReceiveResult> receiveTask = udpClient.ReceiveAsync();
                    if (receiveTask.Wait(5000, receiveTaskSource.Token))
                    {
                        UdpReceiveResult result = receiveTask.Result;
                        IPEndPoint serverEP = result.RemoteEndPoint;
                        byte[] buffer = result.Buffer;
                        BinaryReader reader = new BinaryReader(new MemoryStream(buffer));
                        int len = reader.ReadInt32();
                        string msg = reader.ReadString();
                        Debug.WriteLine("Found server :" + serverEP.ToString() + ": " + msg);

                        string[] parts = msg.Split(':');
                        if (parts.Length == 2)
                        {
                            string hostName = parts[0];
                            int port = int.Parse(parts[1]);
                            var entry = Dns.GetHostEntry(hostName);
                            IPAddress ipAddress = null;
                            foreach (var ip in entry.AddressList)
                            {
                                if (ip.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    ipAddress = ip;
                                    break;
                                }
                            }
                            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            client.Connect(new IPEndPoint(ipAddress, port));
                            return new SmartSocketClient(null, client)
                            {
                                Name = localHost,
                                ServerName = entry.HostName
                            };
                        }
                    }
                    else
                    {
                        receiveTaskSource.Cancel();
                    }
                }
                return null;
            });
        }

        internal static string FindLocalHostName()
        {
            try
            {
                IPHostEntry e = Dns.GetHostEntry(IPAddress.Loopback);
                return e.HostName;
            }
            catch
            {
            }
            return null;
        }


        internal static List<string> FindLocalIpAddresses()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.SupportsMulticast && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                     && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    var props = ni.GetIPProperties();
                    if (props.IsDnsEnabled || props.IsDynamicDnsEnabled)
                    {
                        IPHostEntry e = Dns.GetHostEntry(IPAddress.Loopback);
                        List<string> ipAddresses = new List<string>();
                        foreach (var addr in e.AddressList)
                        {
                            ipAddresses.Add(addr.ToString());
                        }
                        return ipAddresses;
                    }
                }
            }
            return null;
        }

        public string ServerName { get; set; }

        /// <summary>
        /// To customize the message object set this factory, this will be used to create a new Message type
        /// when messages are received from the server
        /// </summary>
        public MessageFactory MessageFactory { get; set; }


        /// <summary>
        /// This event is raised when a message is received from a client.  The "sender" on this event
        /// is the ClientProxy that the message was received from which can be used later to send
        /// messages to that client (so long as we are still connected to that client).
        /// </summary>
        public event EventHandler<Message> MessageReceived
        {
            add
            {
                if (messageHandler == null)
                {
                    messageHandler = value;
                }
                else
                {
                    messageHandler = (EventHandler<Message>)Delegate.Combine(messageHandler, value);
                }
                if (!receiving)
                {
                    receiving = true;
                    // begin the chain of receiving messages.
                    Task.Run(new Action(ReceiveThread));
                }
            }
            remove
            {
                messageHandler = (EventHandler<Message>)Delegate.Remove(messageHandler, value);
                if (messageHandler== null)
                {
                    receiving = false;
                }
            }
        }

        /// <summary>
        /// This event is raised if a socket error is detected.
        /// </summary>
        public event EventHandler<Exception> Error;

        internal void Close()
        {
            try
            {
                Send(new Message() { Type = MessageType.Disconnect});

                Thread.Sleep(1000); // give message time to get there.

                using (client)
                {
                    client.Close();
                }

                disconnected = true;
                receiving = false;
            }
            catch { }
        }

        private void OnError(Exception ex)
        {
            Exception inner = ex;
            while (inner != null)
            {
                SocketException se = inner as SocketException;
                if (se != null && se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // we're toast!
                    if (server != null) server.RemoveClient(this);
                    disconnected = true;
                }
                inner = inner.InnerException;
            }

            if (Error != null)
            {
                Error(this, ex);
            }
        }

        ManualResetEvent sent = new ManualResetEvent(false);

        /// <summary>
        /// Send a message back to the client.
        /// </summary>
        /// <param name="msg"></param>
        public void Send(Message msg)
        {
            // get the buffer containing the serialized message.
            Task.Run(() =>
            {
                // Begin sending the data to the remote device.
                try
                {
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(ms);
                    msg.Write(writer);
                    writer.Flush();
                    byte[] buffer = ms.ToArray();

                    BinaryWriter streamWriter = new BinaryWriter(socketStream, Encoding.UTF8, true);
                    streamWriter.Write(buffer.Length);
                    streamWriter.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    // is the socket dead?
                    OnError(ex);
                }
            });
        }

        private void ReceiveThread()
        {
            receiving = true;
            while (!disconnected && receiving)
            {
                try
                {
                    Message msg = null;
                    using (BinaryReader streamReader = new BinaryReader(socketStream, Encoding.UTF8, true))
                    {
                        int len = streamReader.ReadInt32();
                        byte[] block = streamReader.ReadBytes(len);
                        if (MessageFactory != null)
                        {
                            msg = MessageFactory();
                        }
                        else
                        {
                            msg = new Message();
                        }
                        msg.Read(new BinaryReader(new MemoryStream(block)));
                    }
                    OnMessageReceived(msg);
                }
                catch (EndOfStreamException eos)
                {
                    OnError(eos);
                    receiving = false;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
            receiving = false;
        }

        private void OnMessageReceived(Message msg)
        {
            if (messageHandler != null)
            {
                messageHandler(this, msg);
            }
        }

    }

}
