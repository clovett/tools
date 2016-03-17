using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Microsoft.Networking.SmartSockets
{
    public delegate Message MessageFactory();

    public sealed class SmartSocketClient
    {
        StreamSocket socket;
        BinaryReader reader;
        BinaryWriter writer;
        bool closed;
        EventHandler<Message> messageHandler;
        bool receiving;
        SmartSocketListener server;
        
        internal SmartSocketClient(SmartSocketListener server, StreamSocket socket)
        {
            this.server = server;
            this.socket = socket;
            this.reader = new BinaryReader(socket.InputStream.AsStreamForRead(), Encoding.UTF8, true);
            this.writer = new BinaryWriter(socket.OutputStream.AsStreamForWrite(), Encoding.UTF8, true);
        }

        public string Name { get; set; }

        public string ServerName { get; set; }

        /// <summary>
        /// To customize the message object set this factory, this will be used to create a new Message type
        /// when messages are received from the server
        /// </summary>
        public MessageFactory MessageFactory { get; set; }

        /// <summary>
        /// Find a SmartSocketListener on the local network using UDP broadcast.
        /// </summary>
        /// <param name="serviceName">The service we are looking for</param>
        /// <param name="token">The cancellation token to cause this method to stop looking</param>
        /// <returns>The connected client or null if task is cancelled.</returns>
        public static async Task<SmartSocketClient> FindServerAsync(string serviceName, CancellationToken token)
        {
            NetworkAdapter adapter;
            HostName localName = SmartSocketListener.GetLocalHostName(out adapter);
            string serverPort = null;
            string serverIp = null;
            var dgramSocket = new DatagramSocket();            
            await dgramSocket.BindServiceNameAsync("", adapter);
            dgramSocket.MessageReceived += (/*DatagramSocket*/ sender, /*DatagramSocketMessageReceivedEventArgs*/ args) =>
            {
                serverIp = args.RemoteAddress.CanonicalName;
                using (var stream = args.GetDataStream().AsStreamForRead())
                {
                    BinaryReader reader = new BinaryReader(stream);
                    int len = reader.ReadInt32();
                    serverPort = reader.ReadString();
                }
            };

            while (!token.IsCancellationRequested)
            {
                if (serverPort != null && serverIp != null)
                {
                    // ah! server has responded then
                    StreamSocket socket = new StreamSocket();
                    await socket.ConnectAsync(new HostName(serverIp), serverPort);
                    var client = new SmartSocketClient(null, socket);
                    client.Name = socket.Information.LocalAddress.CanonicalName;
                    client.ServerName = socket.Information.RemoteHostName.CanonicalName;
                    return client;
                }

                using (var stream = await dgramSocket.GetOutputStreamAsync(new HostName(SmartSocketListener.GroupAddress.ToString()), SmartSocketListener.GroupPort.ToString()))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream.AsStreamForWrite()))
                    {
                        // send the magic message needed to make the server respond.
                        writer.Write(serviceName.Length);
                        writer.Write(serviceName);
                        writer.Flush();
                    }
                }

                // send out a ping every 1 second until we find it.
                await Task.Delay(1000);
            }
            return null;
        }

        public void Close()
        {
            closed = true;
            receiving = false;
            using (this.reader)
            {
                this.reader = null;
            }
            using (this.writer)
            {
                this.writer = null;
            }
            using (this.socket)
            {
                this.socket = null;
            }
        }

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
                if (messageHandler == null)
                {
                    receiving = false;
                }
            }
        }

        public event EventHandler<Exception> Error;

        private void OnMessageReceive(Message m)
        {
            if (messageHandler != null)
            {
                messageHandler(this, m);
            }
        }

        private void ReceiveThread()
        {
            receiving = true;
            while (!closed && receiving)
            {
                try
                {
                    if (this.reader == null)
                    {
                        break;
                    }

                    Message m = null;
                    if (MessageFactory != null)
                    {
                        m = MessageFactory();
                    }
                    else
                    {
                        m = new Message();
                    }
                    int len = this.reader.ReadInt32();
                    byte[] block = this.reader.ReadBytes(len);

                    BinaryReader reader = new BinaryReader(new MemoryStream(block));
                    m.Read(reader);
                    OnMessageReceive(m);                    

                } 
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
            receiving = false;
        }

        private void OnError(Exception ex)
        {
            if ((uint)ex.HResult == 0x80072746) // SocketError.ConnectionReset
            {
                if (server != null)
                {
                    // we're toast!
                    server.RemoveClient(this);
                }
                receiving = false;
            }
            if (Error != null)
            {
                Error(this, ex);
            }
        }


        public void Send(Message message)
        {
            Task.Run(() =>
            {
                try
                {
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(ms);
                    message.Write(writer);
                    byte[] buffer = ms.ToArray();
                    int len = buffer.Length;
                    this.writer.Write(len);
                    this.writer.Write(buffer, 0, len);
                    this.writer.Flush();
                }
                catch (Exception e)
                {
                    OnError(e);
                }
            });
        }
    }
}
