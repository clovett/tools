using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace FileDateTakenSorter
{
    class Program
    {
        static void Main(string[] args)
        {
            Sort(args[0]).GetAwaiter().GetResult();
        }

        static async Task Sort(string directory)
        {
            SortedList<DateTime, string> sortedList = new SortedList<DateTime, string>();
            foreach (string fileName in System.IO.Directory.GetFiles(directory))
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(fileName);
                ImageProperties properties = await file.Properties.GetImagePropertiesAsync();
                var date = properties.DateTaken;
                sortedList.Add(date.LocalDateTime, fileName);
            }

            int index = 1;
            foreach (var pair in sortedList)
            {
                var path = pair.Value;
                string name = "IMG_" + index.ToString("D3");
                string ext = Path.GetExtension(path);
                string dir = Path.GetDirectoryName(path);
                string newName = Path.Combine(dir, name + ext);
                File.Move(path, newName);
                index++;
            }

            return;
        }
    }
}
