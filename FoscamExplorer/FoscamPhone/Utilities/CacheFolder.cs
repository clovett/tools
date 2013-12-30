using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace FoscamExplorer
{
    public class CacheFolder
    {
        string dir;

        public CacheFolder(string path)
        {
            this.dir = path;
        }

        public async Task<FileStream> SaveFileAsync(string path)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                return store.OpenFile(path, System.IO.FileMode.Create);
            }
        }
    }
}
