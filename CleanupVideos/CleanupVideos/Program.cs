using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace CleanupVideos
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
            p.Run();
        }

        int files;

        private void Run()
        {

            string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            if (pictures == videos)
            {
                Console.WriteLine("Cannot cleanup videos because pictures and videos library are in the same place, namely:");
                Console.WriteLine(pictures);
                return;
            }

            ScanForVideos(pictures, videos);

            Console.WriteLine("Checked {0} files", files);

            Console.WriteLine("Found the following file types:");

            foreach (var pair in map)
            {
                Console.WriteLine("  " + pair.Key + " => " + pair.Value);
            }

            return;
        }

        private void ScanForVideos(string picDir, string videoDir)
        {
            foreach (string file in Directory.GetFiles(picDir))
            {
                files++;
                if (IsVideo(file))
                {
                    Move(file, videoDir);
                }
            }
            foreach (string dir in Directory.GetDirectories(picDir))
            {
                string tail = Path.GetFileName(dir);
                string target = Path.Combine(videoDir, tail);
                ScanForVideos(dir, target);
            }
        }

        private void Move(string file, string videoDir)
        {
            string destination = Path.Combine(videoDir, Path.GetFileName(file));
            if (File.Exists(destination))
            {
                Console.WriteLine("### Destination video already exists");
                Console.WriteLine("  Cannot move vide from: " + file);
                Console.WriteLine("  To destination: " + destination);
            }
            else
            {
                if (!Directory.Exists(videoDir))
                {
                    Directory.CreateDirectory(videoDir);
                }
                Console.WriteLine("Moving '{0}' to: {1}", Path.GetFileName(file), videoDir);
                File.Move(file, destination);
            }
        }

        private bool IsVideo(string fileName)
        {
            string fileType = GetFileType(fileName);
            if (fileType == null)
            {
                return false;
            }
            string lower = fileType.ToLowerInvariant();
            return lower == "video clip" || lower == "quicktime movie" || lower == "mp4 video";
        }

        Dictionary<string, string> map = new Dictionary<string, string>();
        
        private string GetFileType(string file)
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            string fileType;
            if (!map.TryGetValue(extension, out fileType))
            {
                IntPtr buffer = Marshal.AllocCoTaskMem(692); // C++ says it is 692 bytes.       
                for (int i = 0; i < 692; i++ )
                {
                    Marshal.WriteByte(buffer + i, 0);
                }
                int hr = SHGetFileInfo(extension, 0, buffer, 692, SHGFI_USEFILEATTRIBUTES | SHGFI_TYPENAME);
                if (hr == 1)
                {
                    // skip hIcon, iIcon and dwAttributes and the szDisplayName fields.
                    int size = Marshal.SizeOf(typeof(SHFILEINFO));
                    IntPtr szTypeName = buffer + size + (MAX_PATH * 2);
                    fileType = Marshal.PtrToStringUni(szTypeName);
                    Marshal.FreeCoTaskMem(buffer);
                    map[extension] = fileType;
                }
            }
            return fileType;
        }

        const int SHGFI_TYPENAME          = 0x000000400;
        const int SHGFI_USEFILEATTRIBUTES = 0x000000010;
        const int MAX_PATH          = 260;

        [StructLayout(LayoutKind.Sequential)]
        struct SHFILEINFO {
          IntPtr /*HICON*/ hIcon;
          int   iIcon;
          int /*DWORD*/ dwAttributes;
          //char szDisplayName[MAX_PATH];
          //char szTypeName[80];
        };


        [DllImport("Shell32", CharSet = CharSet.Unicode)]
        extern static int SHGetFileInfo(string pszPath, int dwFileAttributes, IntPtr psfi, uint cbFileInfo, uint uFlags);

    }
}
