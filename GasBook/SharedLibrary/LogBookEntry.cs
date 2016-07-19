using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SharedLibrary
{
    public class LogBookEntry : INotifyPropertyChanged
    {
        DateTimeOffset date;
        decimal amount;
        decimal gallons;
        decimal miles;
        decimal mpg;
        string gps;

        public DateTimeOffset Date
        {
            get
            {
                return date;
            }

            set
            {
                if (date != value)
                {
                    date = value;
                    OnPropertyChanged("Date");
                }
            }
        }

        public decimal Amount
        {
            get
            {
                return amount;
            }

            set
            {
                if (amount != value)
                {
                    amount = value;
                    OnPropertyChanged("Amount");
                }
            }
        }

        public decimal Gallons
        {
            get
            {
                return gallons;
            }

            set
            {
                if (gallons != value)
                {
                    gallons = value;
                    OnPropertyChanged("Gallons");
                }
            }
        }

        public decimal Miles
        {
            get
            {
                return miles;
            }

            set
            {
                if (miles != value)
                {
                    miles = value;
                    OnPropertyChanged("Miles");

                    // compute mpg
                    if (gallons!= 0)
                    {
                        decimal mpg = value / gallons;
                        if (mpg.IsClose(this.mpg, 0.01M))
                        {
                            this.mpg = mpg;
                            OnPropertyChanged("Mpg");
                        }
                    }
                }
            }
        }

        public decimal Mpg
        {
            get
            {
                return mpg;
            }

            set
            {
                if (mpg != value)
                {
                    mpg = value;
                    OnPropertyChanged("Mpg");

                    // compute miles
                    if (gallons != 0)
                    {
                        decimal miles = value * gallons;
                        if (miles.IsClose(this.miles, 0.01M))
                        {
                            this.miles = miles;
                            OnPropertyChanged("Miles");
                        }
                    }
                }
            }
        }

        public string Gps
        {
            get
            {
                return gps;
            }

            set
            {
                if (gps != value)
                {
                    gps = value;
                    OnPropertyChanged("Gps");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            UiDispatcher.RunOnUIThread(new Action(() => {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
            }));
        }
    }
}
