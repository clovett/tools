using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutlookSync
{
    class Log
    {
        static StreamWriter log;

        public static void OpenLog(string fileName)
        {
            if (log == null)
            {
                try
                {
                    string dir = Path.GetDirectoryName(fileName);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    log = new StreamWriter(fileName);
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


        public static void WriteException(string message, Exception ex)
        {
            if (log != null)
            {
                log.WriteLine(message);
                log.Flush();
            }
            Debug.WriteLine(message);
            WriteException(ex);
        }

        private static void WriteException(Exception ex)
        {
            WriteLine("Exception: {0}", ex.GetType().FullName);
            WriteLine(ex.ToString());
            WriteLine("------------------------------------------------------------------------------");
        }
    }
}
