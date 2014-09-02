using Microsoft.Journal.Controls;
using Microsoft.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Microsoft.Journal
{
    public class Journal
    {
        StorageFile file;

        ObservableCollection<JournalEntry> _entries = new ObservableCollection<JournalEntry>();

        public Journal() { }

        public ObservableCollection<JournalEntry> Entries { get { return _entries; } }


        public static async Task<Journal> LoadAsync(StorageFile file)
        {
            Journal journal = await IsolatedStorage<Journal>.LoadFromFileAsync(file);
            if (journal == null)
            {
                journal = new Journal();
            }
            journal.file = file;

            return journal;
        }

        public async Task SaveAsync()
        {
            try
            {
                if (this.file != null)
                {
                    await IsolatedStorage<Journal>.SaveToFileAsync(file, this);
                }
            }
            catch
            {
            }
        }
    }

}
