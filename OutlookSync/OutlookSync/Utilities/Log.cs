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
        StreamWriter log;
        string fileName;
        static Log instance;

        public Log Instance
        {
            get
            {
                return instance;
            }
        }

        private Log(string fileName)
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
                    this.fileName = fileName;
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                    log = new StreamWriter(fileName);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error opening log: " + ex.Message);
                }
            }
            instance = this;
        }

        public string FileName { get { return this.fileName; } }

        public static Log OpenLog(string fileName)
        {
            return new Log(fileName);
        }

        public void CloseLog()
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
            if (instance != null)
            {
                instance.WriteLog(string.Format(message, args));
            }
        }


        public static void WriteException(string message, Exception ex)
        {
            WriteException(ex);
        }

        private static void WriteException(Exception ex)
        {
            WriteLine("Exception: {0}", ex.GetType().FullName);
            WriteLine(ex.ToString());
            WriteLine("------------------------------------------------------------------------------");
        }

        private void WriteLog(string content)
        {
            if (log != null)
            {
                log.WriteLine(content);
                log.Flush();
            }
            Debug.WriteLine(content);
        }
    }
}
