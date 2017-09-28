using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Specialized;

namespace whois {
    class Program {
        static void Main(string[] args) {
            StringCollection filter = new StringCollection();
            ArrayList files = new ArrayList();
            for (int i = 0, n = args.Length; i<n; i++) {
                string arg = args[i];
                if (arg[0] == '/' || arg[0] == '-')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "from":
                            filter.Add(args[++i]);
                            break;
                    }
                }
                else
                {
                    files.Add(arg);
                }
            }
            foreach (string file in files)
            {
                Process(file, filter);
            }
        }
        static void Process(string arg, StringCollection filter)
        {
            if (File.Exists(arg)) {
                StreamReader sr = new StreamReader(arg);
                for (string line = sr.ReadLine(); line != null; line = sr.ReadLine()) {
                    // 192.168.1.107 65.59.234.166 www 
                    string[] sa = line.Split(' ');
                    if (sa != null && sa.Length >= 3 && (sa[2] == "http" || sa[2] == "https")) {
                        if (filter.Count == 0 || filter.Contains(sa[0]))
                        {
                            string ip = sa[1];
                            WhoIs(ip);
                        }
                    }
                }
            } else {
                WhoIs(arg);
            }
        }

        static string WhoIs(string address) {
            try {
                IPAddress ip = IPAddress.Parse(address);
                IPHostEntry e = Dns.GetHostEntry(ip);
                Console.WriteLine(address + ": " + e.HostName);
                return e.HostName;
            } catch (Exception e) {
                return address + ": error " + e.Message;
            }
        }
    }
}
