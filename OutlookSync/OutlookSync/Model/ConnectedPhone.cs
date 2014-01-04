using Microsoft.Networking;
using Microsoft.Office.Interop.Outlook;
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
        OutlookStoreLoader loader;
        UnifiedStore store;
        string name;
        int max;
        int current;
        Dispatcher dispatcher;
        string fileName;
        List<string> localDeletes;

        public ConnectedPhone(UnifiedStore store, Dispatcher dispatcher, string fileName)
        {
            this.store = store;
            this.dispatcher = dispatcher;
            this.max = store.Contacts.Count;
            this.fileName = fileName;
        }

        private async Task Save()
        {
            await store.SaveAsync(fileName);
        }

        public async Task SyncOutlook()
        {
            loader = new OutlookStoreLoader();
            await loader.UpdateAsync(store);
            localDeletes = new List<string>(loader.GetLocalDeletes());
            await Save();
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
                dispatcher.BeginInvoke(new System.Action(() =>
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }));
            }
        }

        internal Message HandleMessage(MessageEventArgs e)
        {
            Message response = null;
            var m = e.Message;
            switch (m.Command)
            {
                case "Connect":
                    Name = m.Parameters;
                    response = new Message() { Command = "Count", Parameters = store.Contacts.Count.ToString() };
                    break;

                case "Disconnect":
                    response = new Message() { Command = "Disconnect" };
                    break;

                case  "DeleteContact":
                    return DeleteContact(m.Parameters);

                case "UpdateContact":
                    return UpdateContact(m.Parameters);

                case "GetNextDelete":
                    return GetNextDelete();

                case "GetContact":

                    int contactIndex = 0;
                    if (int.TryParse(m.Parameters, out contactIndex) && contactIndex < store.Contacts.Count)
                    {
                        Current = contactIndex;
                        string xml = store.Contacts[contactIndex].ToXml();
                        response = new Message() { Command = "Contact", Parameters = xml };
                    }
                    else
                    {
                        response = new Message() { Command = "NoMoreContacts" };
                    }
                    break;
                default:
                    Log.WriteLine("Unrecognized command: {0}", m.Command);
                    break;
            }
            return response;
        }

        private Message GetNextDelete()
        {
            if (localDeletes != null && localDeletes.Count > 0)
            {
                string id = localDeletes[0];
                localDeletes.RemoveAt(0);
                return new Message() { Command = "ServerDelete", Parameters = id };
            }
            return new Message() { Command = "NoMoreDeletes" };
        }


        private Message DeleteContact(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                loader.DeleteContact(id);
            }

            return new Message() { Command = "Deleted" };
        }

        private Message UpdateContact(string xml)
        {
            UnifiedContact fromPhone = UnifiedContact.Parse(xml);
            if (fromPhone != null)
            {
                string id = fromPhone.OutlookEntryId;
                if (!string.IsNullOrEmpty(id))
                {
                    UnifiedContact cached = this.store.FindOutlookEntry(fromPhone.OutlookEntryId);
                    if (cached == null)
                    {
                        // Oh, user added a new contact then!
                        cached = fromPhone;
                        this.store.Contacts.Add(cached);
                    }
                    else
                    {
                        cached.Merge(fromPhone);
                    }

                    // push this to Outlook.
                    string newId = loader.UpdateContact(cached);

                    return new Message() { Command = "Updated", Parameters = id + "=>" + newId };
                }
            }

            return new Message() { Command = "Updated", Parameters = "Parse error or missing id" };
        }
    }
}
