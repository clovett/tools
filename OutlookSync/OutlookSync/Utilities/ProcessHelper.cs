using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;

namespace OutlookSync
{
    public static class ProcessHelper
    {
        public static string StartupPath
        {
            get
            {
                Process p = Process.GetCurrentProcess();
                string exe = p.MainModule.FileName;
                return Path.GetDirectoryName(exe);
            }
        }
    }
}
