using Microsoft.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chimpmunk.Model
{
    public class ModelObject : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class Job : ModelObject
    {
        private ObservableCollection<Pattern> _patterns = new ObservableCollection<Pattern>();
        private ObservableCollection<Pattern> _stockPile = new ObservableCollection<Pattern>();

        /// <summary>
        /// The requested patterns
        /// </summary>
        public ObservableCollection<Pattern> Patterns
        {
            get { return _patterns; }
            set { _patterns = value; OnPropertyChanged("Patterns"); }
        }

        /// <summary>
        /// The stock pile we can cut from
        /// </summary>
        public ObservableCollection<Pattern> StockPile
        {
            get { return _stockPile; }
            set { _stockPile = value; OnPropertyChanged("StockPile"); }
        }

        internal bool Solve()
        {
            foreach (var p in StockPile)
            {
                p.Cuts.Clear();
                p.Remaining = p.Length;
            }

            List<Pattern> overflow = new List<Pattern>();

            // fill the stock using dumb greedy algorithm.
            foreach (var p in this.Patterns)
            {
                if (!SimpleCut(p))
                {
                    overflow.Add(p);
                }
            }


            // now see if every pattern fits the stockpile.
            bool result = true;
            foreach (var p in this.Patterns)
            {
                bool found = false;
                foreach (var s in StockPile)
                {
                    if (s.Cuts.Contains(p))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        private bool SimpleCut(Pattern p)
        {
            foreach (var s in StockPile)
            {
                if (s.Remaining > p.Length)
                {
                    s.Cuts.Add(p);
                    s.Remaining -= p.Length;
                    return true;
                }
            }
            return false;
        }


        public static async Task<Job> LoadAsync()
        {
            var store = new IsolatedStorage<Job>();
            Job result = null;
            try
            {
                result = await store.LoadFromFileAsync(Windows.Storage.ApplicationData.Current.LocalFolder, "job.xml");
            }
            catch
            {
            }
            return result;
        }

        public async Task SaveAsync()
        {
            var store = new IsolatedStorage<Job>();
            await store.SaveToFileAsync(Windows.Storage.ApplicationData.Current.LocalFolder, "job.xml", this);
        }
    }

    public class Pattern : ModelObject
    {
        private double _length;
        private double _scaled;
        private string _color;
        private ObservableCollection<Pattern> _cuts = new ObservableCollection<Pattern>();

        /// <summary>
        /// The length of this cut
        /// </summary>
        public double Length
        {
            get { return _length; }
            set { _length = value; OnPropertyChanged("Length"); }
        }

        /// <summary>
        /// The length scaled to fit the UI
        /// </summary>
        public double ScaledLength
        {
            get { return _scaled; }
            set { _scaled = value; OnPropertyChanged("ScaledLength"); }
        }

        /// <summary>
        /// The color for rendering this pattern
        /// </summary>
        public string Color
        {
            get { return _color; }
            set { _color = value; OnPropertyChanged("Color"); }
        }

        /// <summary>
        /// The list of Cuts we are making in this piece of Stock
        /// </summary>
        public ObservableCollection<Pattern> Cuts
        {
            get { return _cuts; }
            set { _cuts = value; OnPropertyChanged("Cuts"); }
        }

        /// <summary>
        /// if this is a Stock this is the remaining length.
        /// </summary>
        public double Remaining { get; set; }
    }

}
