using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Journal
{
    public class Journal
    {
        ObservableCollection<JournalEntry> _entries = new ObservableCollection<JournalEntry>();

        public Journal() { }

        public ObservableCollection<JournalEntry> Entries { get { return _entries; } }

    }

    public class JournalEntry
    {
        public JournalEntry() { }

        public DateTime StartTime { get; set; }
        public string Title { get; set; }
    }
}
