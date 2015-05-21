using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class LogBook
    {
        ObservableCollection<LogBookEntry> entries = new ObservableCollection<LogBookEntry>();

        public ObservableCollection<LogBookEntry> Entries {  get { return this.entries;  } }

    }
}
