using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Clocks.Storage
{
    public class Data : INotifyPropertyChanged
    {
        const string DataFile = "data.xml";

        public event EventHandler HistoryChanged;

        public Data() {

            History = new List<Session>();
        }

        public List<Session> History { get; set; }

        [XmlIgnore]
        public Session Current { get; set; }

        public Session AddNew()
        {
            Session s = new Session();
            s.StartDate = DateTime.Now;
            History.Add(s);
            OnChanged();
            return s;
        }

        internal void Clear()
        {
            History.Clear();
            OnChanged();
        }

        void OnChanged()
        {
            if (HistoryChanged != null)
            {
                HistoryChanged(this, EventArgs.Empty);
            }
        }

        public static Data LoadData()
        {
            IsolatedStorage<Data> f = new IsolatedStorage<Data>();
            Data data = f.LoadFromFile(DataFile);
            if (data == null)
            {
                data = new Data();
            }
            return data;
        }

        public void SaveData()
        {
            try
            {
                IsolatedStorage<Data> f = new IsolatedStorage<Data>();
                f.SaveToFile(DataFile, this);
            }
            catch
            {
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

    }

    public class Session
    {
        public Session()
        {
            Times = new List<Test>();
        }

        public DateTime StartDate { get; set; }

        public List<Test> Times { get; set; }

        public int Correct { get; set; }
    }

    public class Test
    {
        /// <summary>
        /// The correct time shown on the clock
        /// </summary>
        [XmlAttribute]
        public DateTime Answer { get; set; }

        /// <summary>
        /// The user's attempt
        /// </summary>
        [XmlAttribute]
        public DateTime Entry { get; set; }

        /// <summary>
        /// How long it took to get this answer
        /// </summary>
        [XmlAttribute]
        public ulong ElapsedMilliseconds { get; set; }
    }

}
