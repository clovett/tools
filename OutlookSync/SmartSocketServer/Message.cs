using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Networking
{
    public class Message
    {
        public Message()
        {
        }

        public string Command { get; set; }

        public string Parameters { get; set; }
    }
}
