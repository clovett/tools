using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace OutlookSync
{
    
    static internal class InternetExplorer
    { 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "rc")]
        public static void OpenUrl(IntPtr owner, Uri url)
        {
            Uri baseUri = new Uri(ProcessHelper.StartupPath);
            Uri resolved = new Uri(baseUri, url);
            
            // todo: support showing embedded pack:// resources in a popup page (could be useful for help content).
            const int SW_SHOWNORMAL = 1;
            int rc = ShellExecute(owner, "open", resolved.AbsoluteUri, null, ProcessHelper.StartupPath, SW_SHOWNORMAL);
        }

        public static void OpenUrl(IntPtr owner, string url)
        {
            OpenUrl(owner, new Uri(url, UriKind.RelativeOrAbsolute));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "1"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "2"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "4"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "3"), DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
            SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int ShellExecute(IntPtr handle, string verb, string file,
            string args, string dir, int show);

    }
}
