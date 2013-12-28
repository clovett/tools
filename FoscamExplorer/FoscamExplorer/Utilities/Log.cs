using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoscamExplorer
{
    class Log
    {
        static StreamWriter log;

        public static async Task OpenLog(CacheFolder cache)
        {
            var stream = await cache.SaveFileAsync("log.txt");
            log = new StreamWriter(stream.AsStreamForWrite());                
        }

        public static void CloseLog()
        {
            using (log)
            {
                if (log != null)
                {
                    WriteLine("Closing log");
                    log.Flush();
                }
                log = null;
            }
        }

        public static void WriteLine(string message, params object[] args)
        {
            if (log != null)
            {
                log.WriteLine(message, args);
                log.Flush();
            }
            Debug.WriteLine(message, args);
        }
    }
}
