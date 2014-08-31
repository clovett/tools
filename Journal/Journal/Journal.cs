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

    public class JournalEntry : INotifyPropertyChanged
    {
        private bool isSelected;
        private DateTime startTime;
        private int seconds;
        private string title;


        public JournalEntry() { }


        public DateTime StartTime
        {
            get { return startTime; }
            set
            {
                if (startTime != value)
                {
                    startTime = value;
                    OnPropertyChanged("StartTime");
                }
            }
        }



        public int Seconds
        {
            get { return seconds; }
            set
            {
                if (seconds != value)
                {
                    seconds = value;
                    OnPropertyChanged("Seconds");
                    OnPropertyChanged("Duration");
                }
            }
        }


        public TimeSpan Duration
        {
            get { return TimeSpan.FromSeconds(seconds); }
        }

        public string Title
        {
            get { return title; }
            set
            {
                if (title != value)
                {
                    this.title = value;
                    OnPropertyChanged("Title");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
