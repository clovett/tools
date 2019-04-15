using RssLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssMonitorConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            var monitor = new FeedMonitor();
            var updated = monitor.GetUpdatedFeeds();
            monitor.SendEmails(updated);
        }
    }
}
