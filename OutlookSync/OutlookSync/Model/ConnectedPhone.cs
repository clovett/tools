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
        bool allowed;
        string ipEndPoint;

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
            await Save();
        }
        
        public string IPEndPoint {
            get { return ipEndPoint; }
            set
            {
                if (ipEndPoint != value)
                {
                    ipEndPoint = value;
                    OnPropertyChanged("IPEndPoint");
                }
            }
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

        public bool Allowed
        {
            get { return allowed; }
            set
            {
                if (allowed != value)
                {
                    allowed = value;
                    OnPropertyChanged("Allowed");
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

                case "UpdateContact":                    
                    Current++;
                    Maximum = Math.Max(Current, Maximum);
                    return UpdateContact(m.Parameters);

                case "SyncMessage":
                    return HandleSync(m.Parameters);

                case "GetContact":

                    string contactId = m.Parameters;
                    if (!string.IsNullOrEmpty(contactId))
                    {
                        Current++;
                        Maximum = Math.Max(Current, Maximum);
                        UnifiedContact uc = store.FindOutlookEntry(contactId);
                        string xml = "null";
                        if (uc != null)
                        {
                            xml = uc.ToXml();
                        }
                        response = new Message() { Command = "Contact", Parameters = xml };
                    }
                    else
                    {
                        response = new Message() { Command = "Contact", Parameters = null };
                    }
                    break;

                case "FinishUpdate":
                    break;

                default:
                    Log.WriteLine("Unrecognized command: {0}", m.Command);
                    break;
            }
            return response;
        }

        private Message HandleSync(string xml)
        {
            // this message tells us what happened on the phone, what was deleted, inserted, and 
            // the latest version numbers on phone contacts.
            SyncMessage msg = SyncMessage.Parse(xml);

            int identical;
            SyncMessage response = loader.PhoneSync(msg, out identical);

            Current += identical;

            return new Message() { Command = "ServerSync", Parameters = response.ToXml() };
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
