//-----------------------------------------------------------------------
// <copyright file="TaskModel.cs" company="Lovett Software">
//   (c) Lovett Software.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace TimeKeeper
{
    public class TaskModel : DependencyObject
    {
        ObservableCollection<TaskItem> tasks = new ObservableCollection<TaskItem>();
        string fileName;
        string baseUri;
        DispatcherTimer timer;
        DateTime today;

        public TaskModel(string baseUri)
        {
            today = DateTime.Today;
            if (string.IsNullOrEmpty(baseUri) || !Directory.Exists(baseUri))
            {
                throw new FileNotFoundException("TaskModel constructor needs an existing directory");
            }
            this.baseUri = baseUri;            
            PropertyMetadata m = TaskItem.StartTimeProperty.GetMetadata(this);
            tasks.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnTasksChanged);
            // Create timer for auto-incrementing the current task duration.
            this.timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, new EventHandler(OnTick), Dispatcher.CurrentDispatcher);
        }

        public string FileName
        {
            get
            {
                return this.fileName;
            }
        }

        public DateTime Date
        {
            get { return today; }
            set {
                if (value != today)
                {
                    Save();
                    today = value;
                    if (value != DateTime.Today)
                    {
                        this.timer.Stop();
                    }
                    else
                    {
                        this.timer.Start();
                    }
                    Reload();
                }
            }
        }

        void OnTasksChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (TaskItem item in e.NewItems)
                {
                    item.Model = this;
                }
            }
        }

        public ObservableCollection<TaskItem> Tasks
        {
            get { return this.tasks; }
        }

        public TaskItem GetNextTask(TaskItem task)
        {
            int i = tasks.IndexOf(task);
            if (i+1 < tasks.Count)
            {
                return tasks[i + 1];
            }
            return null;
        }

        public TaskItem GetPreviousTask(TaskItem task)
        {
            int i = tasks.IndexOf(task);
            if (i -1 >= 0)
            {
                return tasks[i - 1];
            }
            return null;
        }

        public string BaseUri
        {
            get { return this.baseUri; }
            set { this.baseUri = value; Load();  }
        }

        public DateTime LastWriteTime { get; set; }


        public void Reload()
        {
            Load();
            OnReloaded();
        }

        public void Load()
        {
            string todayFile = today.ToString("yyyy-MM-dd") + ".xml";
            string path = System.IO.Path.Combine(this.baseUri, todayFile);
            this.fileName = path;
            Tasks.Clear(); // todo: merge changes...
            if (File.Exists(path))
            {
                this.LastWriteTime = File.GetLastWriteTime(path);
                XDocument doc = XDocument.Load(path);
                foreach (XElement task in doc.Root.Elements())
                {
                    TaskItem item = new TaskItem();
                    if (item.Load(task))
                    {
                        Tasks.Add(item);
                    }
                }
                // validate the StartTime/Durations to make sure they don't overlap.
                foreach (TaskItem item in Tasks)
                {
                    item.Validate();
                }
            }
            else
            {
            }
            OnLoaded();
            // add place holder for new item at the end
            if (tasks.Count == 0)
            {
                AppendNewTask();
            }
        }

        public void AppendNewTask()
        {
            Tasks.Add(new TaskItem() { StartTime = DateTime.Now, Model = this });
        }        


        public event EventHandler Loaded;

        void OnLoaded()
        {
            if (Loaded != null)
            {
                Loaded(this, null);
            }
        }

        public event EventHandler Reloaded;

        void OnReloaded()
        {
            if (Reloaded != null)
            {
                Reloaded(this, null);
            }
        }

        void OnTick(object sender, EventArgs e)
        {             
            // add place holder for new item at the end
            if (tasks.Count == 0)
            {
                AppendNewTask();
            }
            
            if (DateTime.Now.DayOfWeek != this.today.DayOfWeek)
            {
                // then we just ticked over midnight, so save what we have
                // and load new day...
                Save();
                Date = DateTime.Today;
                Load();
            }
            else
            {
                TaskItem last = tasks[tasks.Count - 1];
                last.Duration = DateTime.Now - last.StartTime;
            }
        }

        public void Save()
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                XDocument doc = new XDocument();
                XElement tasks = new XElement("Tasks");
                foreach (TaskItem item in Tasks)
                {
                    item.Save(tasks);
                }
                doc.Add(tasks);
                doc.Save(fileName);
                this.LastWriteTime = DateTime.Now;
            }
        }


        public static DependencyProperty TotalTimeProperty = DependencyProperty.Register("TotalTime", typeof(TimeSpan), typeof(TaskModel));

        public TimeSpan TotalTime
        {
            get { return (TimeSpan)GetValue(TotalTimeProperty); }
            set { SetValue(TotalTimeProperty, value); }
        }

        internal void UpdateTotalTime()
        {
            TimeSpan span = new TimeSpan(0, 0, 0);
            foreach (TaskItem item in Tasks)
            {
                span += item.Duration;
            }
            TotalTime = span;
        }

    }

    public class TaskItem : DependencyObject
    {
        const string InitialText = "Please enter name of the task you are working on...";

        public TaskItem()
        {
            StartTime = DateTime.Now;
            Description = InitialText;
            IsNew = true;
        }

        public TaskModel Model { get; set; }

        public static DependencyProperty IsNewProperty = DependencyProperty.Register("IsNew", typeof(bool), typeof(TaskItem));

        public bool IsNew
        {
            get { return (bool)GetValue(IsNewProperty); }
            set { SetValue(IsNewProperty, value); }
        }

        public static DependencyProperty IsErrorProperty = DependencyProperty.Register("IsError", typeof(bool), typeof(TaskItem));

        public bool IsError
        {
            get { return (bool)GetValue(IsErrorProperty); }
            set { SetValue(IsErrorProperty, value); }
        }

        public static DependencyProperty StartTimeProperty = DependencyProperty.Register("StartTime", typeof(DateTime), typeof(TaskItem),
            new PropertyMetadata(new PropertyChangedCallback(OnStartTimeChanged)));

        public DateTime StartTime
        {
            get { return (DateTime)GetValue(StartTimeProperty); }
            set { if (value != this.StartTime) SetValue(StartTimeProperty, value); }
        }

        internal void Validate()
        {
            if (this.Model != null)
            {
                TaskItem previous = this.Model.GetPreviousTask(this);
                if (previous != null)
                {
                    // adjust it's duration to match new start time of this item.
                    if (previous.StartTime + previous.Duration > this.StartTime)
                    {
                        if (this.StartTime < previous.StartTime)
                        {
                            this.IsError = true;
                        }
                        else
                        {
                            this.IsError = false;
                            TimeSpan diff = (previous.StartTime + previous.Duration) - this.StartTime;
                            if (previous.Duration > diff)
                            {
                                previous.Duration -= diff;
                            }
                            else
                            {
                                previous.Duration = ErrorTimeSpan;
                            }
                        }
                    }
                    else if (previous.StartTime + previous.Duration < this.StartTime)
                    {
                        // then there's a gap, so make the previous item duration longer.
                        previous.Duration = this.StartTime - previous.StartTime;
                    }

                    // Now since we moved our start time our duration also need updating.
                    TaskItem next = this.Model.GetNextTask(this);
                    if (next != null)
                    {
                        if (this.StartTime + this.Duration != next.StartTime && this.StartTime < next.StartTime)
                        {
                            this.Duration = next.StartTime - this.StartTime;
                        }
                    }
                }
            }
        }

        static TimeSpan ErrorTimeSpan = new TimeSpan(0, 0, 0);

        static void OnStartTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TaskItem item = (TaskItem)d;
            if (!item.loading) item.Validate();
        }

        public static DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(TaskItem),
            new PropertyMetadata(OnDescriptionChanged));

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { if (value != this.Description) SetValue(DescriptionProperty, value); }
        }

        static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TaskItem task = (TaskItem)d;
            task.IsNew = (string)e.NewValue == InitialText; 
        }

        public static DependencyProperty DurationProperty = DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(TaskItem),
            new PropertyMetadata(new PropertyChangedCallback(OnDurationChanged)));

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { if (value != this.Duration) SetValue(DurationProperty, value); }
        }

        static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TaskItem task = (TaskItem)d;
            if (!task.loading) task.ValidateDuration();
        }

        void ValidateDuration()
        {            
            this.IsError = this.Duration == ErrorTimeSpan;
            if (this.Model != null)
            {
                TaskItem next = this.Model.GetNextTask(this);
                if (next != null)
                {
                    // adjust it's duration to match new start time of this item.
                    if (this.StartTime + this.Duration != next.StartTime)
                    {
                        next.StartTime = this.StartTime + this.Duration;
                    }
                }
                TaskItem previous = this.Model.GetPreviousTask(this);
                if (previous != null && previous.StartTime > this.StartTime) 
                {
                    this.IsError = true;
                }                    
                this.Model.UpdateTotalTime();
            }
        }

        bool loading;

        public bool Load(XElement task)
        {
            if (task.Name.LocalName == "Task")
            {
                loading = true;
                this.StartTime = XmlConvert.ToDateTime((string)task.Attribute("StartTime"), XmlDateTimeSerializationMode.RoundtripKind);
                this.Description = (string)task.Attribute("Description");
                this.Duration = XmlConvert.ToTimeSpan((string)task.Attribute("Duration"));
                loading = false;
                return true;
            }
            return false;
        }

        public void Save(XElement parent)
        {
            if (!string.IsNullOrEmpty(this.Description))
            {
                parent.Add(new XElement("Task",
                    new XAttribute("StartTime", XmlConvert.ToString(this.StartTime, XmlDateTimeSerializationMode.RoundtripKind)),
                    new XAttribute("Description", this.Description),
                    new XAttribute("Duration", XmlConvert.ToString(this.Duration))));
            }
        }

    }
}
