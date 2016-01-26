using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MyCloudShare
{
    class NetworkResource
    {
        public static IEnumerable<NetResource> GetConnectedDrives()
        {
            IntPtr hEnum;

            Int32 cbBuffer = 16384;
            IntPtr buffer = Marshal.AllocCoTaskMem(cbBuffer);
            if (buffer == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
            Int32 cEntries = -1;
            Int32 entrySize = Marshal.SizeOf(typeof(NetResource));

            int hr = WNetOpenEnum((Int32)ResourceScope.RESOURCE_CONNECTED, (Int32)ResourceType.RESOURCETYPE_DISK, (Int32)ResourceUsage.RESOURCEUSAGE_ALL, IntPtr.Zero, out hEnum);
            if (hr != 0)
            {
                throw new Exception(string.Format("WNetOpenEnum failed {0}", hr.ToString("x")));
            }
            else
            {
                do
                {
                    hr = WNetEnumResource(hEnum, ref cEntries, buffer, ref cbBuffer);
                    if (hr == 0)
                    {
                        for (int i = 0; i < cEntries; i++)
                        {
                            IntPtr ptr = buffer + (i * entrySize);
                            NetResource nr = Marshal.PtrToStructure<NetResource>(ptr);
                            yield return nr;
                        }
                    }
                    else if (hr != ERROR_NO_MORE_ITEMS)
                    {
                        throw new Exception(String.Format("WNetEnumResource failed with error {0}", hr.ToString("x")));
                    }
                    else
                    {
                        break;
                    }

                }
                while (hr != ERROR_NO_MORE_ITEMS);
            }

            Marshal.FreeCoTaskMem(buffer);
            WNetCloseEnum(hEnum);
        }

        internal static void RestoreConnection(string localDrive)
        {
            int hr = WNetRestoreSingleConnectionW(IntPtr.Zero, localDrive, true);
            if (hr != 0)
            {
                throw new Exception(String.Format("WNetRestoreSingleConnectionW failed with error {0}", hr.ToString("x")));
            }
        }

        internal static void ConnectShare(string localName, string remoteName)
        {
            NetResource res = new NetResource()
            {
                dwType = (Int32)ResourceType.RESOURCETYPE_DISK,
                sLocalName = localName,
                sRemoteName = remoteName
            };
            int hr = WNetAddConnection3(IntPtr.Zero, ref res, null, null, 0);
            if (hr != 0)
            {
                throw new Exception(string.Format("WNetAddConnection3 failed with error {0}", hr.ToString("x")));
            }
        }

        internal static void DisconnectLocalShare(string localName)
        {
            int hr = WNetCancelConnection(localName, true);
            if (hr != 0)
            {
                throw new Exception(string.Format("WNetCancelConnection failed with error {0}", hr.ToString("x")));
            }
        }

        const Int32 ERROR_NO_MORE_ITEMS = 259;

        [DllImport("mpr.dll")]
        internal static extern int WNetAddConnection3(IntPtr hwnd, ref NetResource pstNetRes, string psPassword, string psUsername, Int32 piFlags);

        [DllImport("mpr.dll")]
        internal static extern int WNetEnumResource(IntPtr hEnum, ref Int32 count, IntPtr buffer, ref Int32 bufferSize);

        [DllImport("mpr.dll")]
        internal static extern int WNetOpenEnum(Int32 scope, Int32 type, Int32 usage, IntPtr pstNetRes, out IntPtr hEnum);

        [DllImport("mpr.dll")]
        internal static extern int WNetCloseEnum(IntPtr hEnum);

        [DllImport("mpr.dll")]
        internal static extern int WNetCancelConnection(string lpName, bool fForce);

        [DllImport("mpr.dll")]
        internal static extern int WNetRestoreSingleConnectionW(IntPtr hwndParent, string lpDevice, bool fUseUI);
    }

    enum ResourceScope
    {
        RESOURCE_CONNECTED = 1,
        RESOURCE_GLOBALNET = 2,
        RESOURCE_REMEMBERED = 3,
        RESOURCE_RECENT = 0x00000004,
        RESOURCE_CONTEXT = 0x00000005
    }

    enum ResourceType
    {
        RESOURCETYPE_ANY = 0x00000000,
        RESOURCETYPE_DISK = 0x00000001,
        RESOURCETYPE_PRINT = 0x00000002,
        RESOURCETYPE_RESERVED = 0x00000008,
        //RESOURCETYPE_UNKNOWN  =  0xFFFFFFFF
    }

    enum ResourceUsage
    {
        RESOURCEUSAGE_CONNECTABLE = 0x00000001,
        RESOURCEUSAGE_CONTAINER = 0x00000002,
        RESOURCEUSAGE_NOLOCALDEVICE = 0x00000004,
        RESOURCEUSAGE_SIBLING = 0x00000008,
        RESOURCEUSAGE_ATTACHED = 0x00000010,
        RESOURCEUSAGE_ALL = (RESOURCEUSAGE_CONNECTABLE | RESOURCEUSAGE_CONTAINER | RESOURCEUSAGE_ATTACHED)
        //RESOURCEUSAGE_RESERVED      = 0x80000000
    }

    enum ResourceDisplayType
    {
        RESOURCEDISPLAYTYPE_GENERIC = 0x00000000,
        RESOURCEDISPLAYTYPE_DOMAIN = 0x00000001,
        RESOURCEDISPLAYTYPE_SERVER = 0x00000002,
        RESOURCEDISPLAYTYPE_SHARE = 0x00000003,
        RESOURCEDISPLAYTYPE_FILE = 0x00000004,
        RESOURCEDISPLAYTYPE_GROUP = 0x00000005,
        RESOURCEDISPLAYTYPE_NETWORK = 0x00000006,
        RESOURCEDISPLAYTYPE_ROOT = 0x00000007,
        RESOURCEDISPLAYTYPE_SHAREADMIN = 0x00000008,
        RESOURCEDISPLAYTYPE_DIRECTORY = 0x00000009,
        RESOURCEDISPLAYTYPE_TREE = 0x0000000A,
        RESOURCEDISPLAYTYPE_NDSCONTAINER = 0x0000000B
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NetResource
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string sLocalName;
        public string sRemoteName;
        public string sComment;
        public string sProvider;
    }

}
