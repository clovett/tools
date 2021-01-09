using LovettSoftware.Utilities;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MyFitness.Model
{
    public class CalendarModel : NotifyingObject
    {
        bool isDirty;
        ObservableCollection<CalendarMonth> months = new ObservableCollection<CalendarMonth>();

        public CalendarModel()
        {
            months.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (CalendarMonth item in e.OldItems)
                {
                    item.PropertyChanged -= OnMonthPropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (CalendarMonth item in e.NewItems)
                {
                    item.PropertyChanged += OnMonthPropertyChanged;
                }
            }
        }

        private void OnMonthPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsDirty = true;
        }

        public bool IsDirty
        {
            get { return isDirty; }
            set
            {
                if (isDirty != value)
                {
                    isDirty = value;
                    NotifyPropertyChanged("IsDirty");
                }
            }
        }

        public ObservableCollection<CalendarMonth> Months
        {
            get => months;
            set
            {
                if (months != null)
                {
                    months.CollectionChanged -= OnCollectionChanged;
                    foreach (var item in months)
                    {
                        item.PropertyChanged -= OnMonthPropertyChanged;
                    }
                }
                months = value;
                if (months != null)
                {
                    months.CollectionChanged += OnCollectionChanged;
                    foreach (var item in months)
                    {
                        item.PropertyChanged += OnMonthPropertyChanged;
                    }
                }
            }
        }

        internal Task LoadAsync(string filename)
        {
            // do this on a background task so the UI is not blocked.
            return Task.Run(() =>
            {
                try
                {
                    string contents = System.IO.File.ReadAllText(filename);
                    CalendarModel m = JsonConvert.DeserializeObject<CalendarModel>(contents);
                    var months = m.months;
                    foreach (var item in months)
                    {
                        item.Deselect();
                    }
                    m.Months = null;
                    this.Months = months;
                }
                catch
                {
                    // ignore corrupt models.
                }
            });
        }

        public void Save(string filename)
        {
            System.Diagnostics.Debug.WriteLine("Saving model: " + filename);
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            System.IO.File.WriteAllText(filename, json);
            IsDirty = false;
        }

        internal CalendarMonth GetOrCreateCurrentMonth()
        {
            DateTime today = DateTime.Today;
            CalendarMonth m = months.Where(i => i.Start.Year == today.Year && i.Start.Month == today.Month).FirstOrDefault();
            if (m == null)
            {
                m = new CalendarMonth() { Start = today };
                this.months.Add(m);
            }
            return m;
        }
    }


    // fill a 7x5 grid with days, some days might belong to previous or next month.
    public class CalendarMonth : NotifyingObject
    {
        DateTime start;
        ObservableCollection<CalendarDay> days = new ObservableCollection<CalendarDay>();

        public CalendarMonth()
        {
            days.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (CalendarDay item in e.OldItems)
                {
                    item.PropertyChanged -= OnDayPropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (CalendarDay item in e.NewItems)
                {
                    item.PropertyChanged += OnDayPropertyChanged;
                }
            }
        }

        private void OnDayPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // tell the model that something changed in one of our days.
            // (except for selection changes which are UI properties only).
            if (e.PropertyName != "IsSelected")
            {
                NotifyPropertyChanged("DayChanged");
            }
        }

        public DateTime Start
        {
            get { return start; }
            set
            {
                if (this.start != value)
                {
                    this.start = value;
                    NotifyPropertyChanged("Start");
                }
            }
        }

        public void PopulateIfEmpty()
        {
            if (days.Count == 0)
            {
                DateTime d = new DateTime(this.start.Year, this.start.Month, 1);
                while (d.Month == this.start.Month)
                {
                    days.Add(new CalendarDay() { Date = d });
                    d = d.AddDays(1);
                }
            }
        }

        internal void Deselect()
        {
            foreach (var day in this.Days)
            {
                day.IsSelected = false;
                foreach (var note in day.Notes)
                {
                    note.IsSelected = false;
                }
            }
        }

        public ObservableCollection<CalendarDay> Days
        {
            get => days;
            set => days = value;
        }
    }

    public class CalendarDay : NotifyingObject
    {
        string summary;
        string icon;
        DateTime date;
        bool isSelected;
        ObservableCollection<CalendarNote> notes = new ObservableCollection<CalendarNote>();

        public CalendarDay()
        {
            notes.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (CalendarNote item in e.OldItems)
                {
                    item.PropertyChanged -= OnNotePropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (CalendarNote item in e.NewItems)
                {
                    item.PropertyChanged += OnNotePropertyChanged;
                }
            }
        }

        private void OnNotePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // tell the model that something changed in one of our days
            // (except for selection changes which are UI properties only).
            if (e.PropertyName != "IsSelected")
            {
                NotifyPropertyChanged("NoteChanged");
            }
        }


        public DateTime Date
        {
            get { return date; }
            set
            {
                if (date != value)
                {
                    date = value;
                    NotifyPropertyChanged("Date");
                }
            }
        }

        public string Summary
        {
            get { 
                if (string.IsNullOrEmpty(summary) && IsSelected)
                {
                    return "summary";
                }

                return summary; 
            }
            set
            {
                if (summary != value)
                {
                    summary = value;
                    NotifyPropertyChanged("Summary");
                }
            }
        }

        // return a char from Segoe UI Symbol font E1xx unicode range.
        public string Icon
        {
            get { return icon; }
            set
            {
                if (icon != value)
                {
                    icon = value;
                    NotifyPropertyChanged("Icon");
                }
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                    NotifyPropertyChanged("Summary");
                }
            }
        }

        public ObservableCollection<CalendarNote> Notes
        {
            get => notes;
            set => notes = value;
        }

        public bool IsToday
        {
            get
            {
                return this.Date.Date == DateTime.Today.Date;
            }
        }
    }

    public class CalendarNote : NotifyingObject
    {
        string label;
        bool isSelected;
        bool isNew;

        public string Label
        {
            get { return label; }
            set
            {
                if (label != value)
                {
                    label = value;
                    NotifyPropertyChanged("Label");
                }
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }

        public bool IsNew
        {
            get { return isNew; }
            set
            {
                if (isNew != value)
                {
                    isNew = value;
                    NotifyPropertyChanged("IsNew");
                }
            }
        }
    }
}
