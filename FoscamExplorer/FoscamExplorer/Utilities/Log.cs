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
            if (log == null)
            {
                try
                {
                    var file = await cache.CreateFileAsync("log.txt");
                    log = new StreamWriter(await file.OpenStreamForWriteAsync());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error opening log: " + ex.Message);
                }
            }
        }

        public static void CloseLog()
        {
            using (log)
            {
                if (log != null)
                {
                    lock (log)
                    {
                        WriteLine("Closing log");
                        log.Flush();
                    }
                }
                log = null;
            }
        }

        public static void WriteLine(string message, params object[] args)
        {
            if (log != null)
            {
                lock (log)
                {
                    log.WriteLine(message, args);
                    log.Flush();
                }
            }
            Debug.WriteLine(message, args);
        }
    }
}
