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

        const string openPrefix = "==================== Opened";

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
                    OpenAndTruncate(fileName);

                    // put opening marker in the log so we can truncate the log after a week.
                    WriteLog(string.Format("{0} {1} ========================================", openPrefix, DateTime.Now.ToString()));

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error opening log: " + ex.Message);
                }
            }
            instance = this;
        }

        private void OpenAndTruncate(string fileName)
        {

            List<string> history = new List<string>();

            // load and truncate the log so anything older than 1 week is deleted.
            DateTime truncateDate = DateTime.Now.AddDays(-7);
            if (File.Exists(fileName))
            {
                try
                {
                    bool truncate = true;
                    using (var reader = new StreamReader(fileName))
                    {
                        while (true)
                        {
                            string line = reader.ReadLine();
                            if (line == null)
                            {
                                break;
                            }
                            if (truncate && line.StartsWith(openPrefix))
                            {
                                string timestamp = line.Substring(openPrefix.Length).Replace("=", "").Trim();
                                DateTime date;
                                if (DateTime.TryParse(timestamp, out date))
                                {
                                    if (date >= truncateDate)
                                    {
                                        truncate = false;
                                    }
                                }
                            }
                            if (!truncate)
                            {
                                history.Add(line);
                            }
                        }
                    }
                }
                catch { }

                File.Delete(fileName);
            }

            log = new StreamWriter(fileName);

            foreach (string past in history)
            {
                log.WriteLine(past);
            }
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
