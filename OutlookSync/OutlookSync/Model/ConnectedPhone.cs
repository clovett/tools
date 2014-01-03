using Microsoft.Networking;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OutlookSync.Model
{
    class ConnectedPhone : INotifyPropertyChanged
    {
        UnifiedStore store;
        string name;
        int max;
        int current;
        Dispatcher dispatcher;

        public ConnectedPhone(UnifiedStore store, Dispatcher dispatcher)
        {
            this.store = store;
            this.dispatcher = dispatcher;
            this.max = store.Contacts.Count;
        }

        public string Name {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public int Maximum
        {
            get { return max; }
            set
            {
                if (max != value)
                {
                    max = value;
                    OnPropertyChanged("Maximum");
                }
            }
        }


        public int Current
        {
            get { return current; }
            set
            {
                if (current != value)
                {
                    current = value;
                    OnPropertyChanged("Current");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }));
            }
        }

        internal void HandleMessage(MessageEventArgs e)
        {
            var m = e.Message;
            switch (m.Command)
            {
                case "Connect":
                    Name = m.Parameters;
                    e.Response = new Message() { Command = "Count", Parameters = store.Contacts.Count.ToString() };
                    break;
                case "Disconnect":
                    e.Response = new Message() { Command = "Disconnect" };
                    break;
                case "GetContact":

                    int contactIndex = 0;
                    if (int.TryParse(m.Parameters, out contactIndex) && contactIndex < store.Contacts.Count)
                    {
                        Current = contactIndex;
                        string xml = store.Contacts[contactIndex].ToXml();
                        e.Response = new Message() { Command = "Contact", Parameters = xml };
                    }
                    else
                    {
                        e.Response = new Message() { Command = "NoMoreContacts" };
                    }
                    break;
                default:
                    Log.WriteLine("Unrecognized command: {0}", m.Command);
                    break;
            }
        }
    }
}
