using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Microsoft.Networking
{
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(IPEndPoint from, Message msg)
        {
            Message = msg;
            RemoteEndPoint = from;
        }

        public IPEndPoint RemoteEndPoint { get; set; }

        public Message Message { get; set; }

        // if this is set it will be sent back to the remote end point.
        public Message Response { get; set; }
    }
}
