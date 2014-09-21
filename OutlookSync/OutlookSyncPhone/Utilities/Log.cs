using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutlookSync.Utilities
{
    class Log
    {
        public static void WriteLine(string message)
        {
            Debug.WriteLine(message);
        }

        public static void WriteLine(string message, params object[] args)
        {
            Debug.WriteLine(message, args);
        }
    }
}
