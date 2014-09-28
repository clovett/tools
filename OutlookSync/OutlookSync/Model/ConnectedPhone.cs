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
using System.Xml.Linq;

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
        bool connected;
        string id;
        SyncResult result;
        bool synced;
        string syncError;

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
            try
            {
                UnifiedStore.UpdateSyncTime();
                loader = new OutlookStoreLoader();
                loader.StartOutlook();
                await loader.UpdateAsync(store);
                await Save();
                this.SyncStatus = new SyncResult(loader.GetLocalSyncMessage(), false);
            }
            catch (Exception ex)
            {
                SyncError = ex.Message;
            }
        }

        public bool InSync
        {
            get { return synced; }
            set
            {
                if (synced != value)
                {
                    synced = value;
                    OnPropertyChanged("InSync");
                }
            }
        }

        public string SyncError
        {
            get { return syncError; }
            set
            {
                if (syncError != value)
                {
                    syncError = value;
                    OnPropertyChanged("SyncError");
                }
            }
        }

        public string IPEndPoint
        {
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

        public SyncResult SyncStatus
        {
            get { return result; }
            set
            {
                if (result != value)
                {
                    result = value;
                    OnPropertyChanged("SyncStatus");
                }
            }
        }

        public bool Connected
        {
            get { return connected; }
            set
            {
                if (connected != value)
                {
                    connected = value;
                    OnPropertyChanged("Connected");
                }
            }
        }

        public string Id
        {
            get { return id; }
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        public string Name 
        {
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
                case "Hello":
                    break;

                case "Connect":                    
                    string phoneAppVersion = "";
                    string phoneName = "Unknown";
                    string phoneId = "";
                    string parameters = m.Parameters;
                    if (parameters != null)
                    {
                        string[] parts = m.Parameters.Split('/');
                        if (parts.Length > 0)
                        {
                            phoneAppVersion = parts[0];
                        }
                        if (parts.Length > 1)
                        {
                            phoneName = parts[1];
                        }
                        if (parts.Length > 2)
                        {
                            phoneId = Uri.UnescapeDataString(parts[2]);
                        }
                    }

                    this.Name = phoneName;
                    this.Id = phoneId;

                    response = new Message() { Command = "Count", Parameters = store.Contacts.Count.ToString() + "," + UnifiedStore.SyncTime };
                    break;

                case "Disconnect":
                    response = new Message() { Command = "Disconnect" };
                    break;

                case "UpdateContact":                    
                    Current++;                    
                    Maximum = Math.Max(Current, Maximum);
                    return UpdateContact(m.Parameters);

                case "UpdateAppointment":
                    Current++;
                    Maximum = Math.Max(Current, Maximum);
                    return UpdateAppointment(m.Parameters);

                case "SyncMessage":
                    return HandleSync(m.Parameters);

                case "GetContact":

                    string contactId = m.Parameters;
                    if (!string.IsNullOrEmpty(contactId))
                    {
                        Current++;
                        Maximum = Math.Max(Current, Maximum);
                        UnifiedContact uc = store.FindContactById(contactId);
                        string xml = "null";
                        if (uc != null)
                        {
                            Log.WriteLine("Sending contact to phone: [{0}]", uc.DisplayName);
                            xml = uc.ToXml();
                        }
                        response = new Message() { Command = "Contact", Parameters = xml };
                    }
                    else
                    {
                        response = new Message() { Command = "Contact", Parameters = null };
                    }
                    break;

                case "GetAppointment":

                    string appointmentId = m.Parameters;
                    if (!string.IsNullOrEmpty(appointmentId))
                    {
                        Current++;
                        Maximum = Math.Max(Current, Maximum);
                        UnifiedAppointment ua = store.FindAppointmentById(appointmentId);
                        string xml = "null";
                        if (ua != null)
                        {
                            xml = loader.GetSyncXml(ua);
                        }
                        response = new Message() { Command = "Appointment", Parameters = xml };
                    }
                    else
                    {
                        response = new Message() { Command = "Appointment", Parameters = null };
                    }
                    break;

                case "FinishUpdate":
                    this.InSync = true;
                    break;

                default:
                    Log.WriteLine("Unrecognized command: {0}", m.Command);
                    break;
            }
            return response;
        }

        private Message HandleSync(string xml)
        {
            Log.WriteLine("### Begin sync for phone: " + this.Name);

            // this message tells us what happened on the phone, what was deleted, inserted, and 
            // the latest version numbers on phone contacts.
            SyncMessage msg = SyncMessage.Parse(xml);

            int identical;
            SyncMessage response = loader.PhoneSync(msg, SyncStatus, out identical);

            OnPropertyChanged("SyncStatus");

            Current += identical;

            return new Message() { Command = "ServerSync", Parameters = response.ToXml() };
        }        

        private Message UpdateContact(string xml)
        {
            UnifiedContact fromPhone = UnifiedContact.Parse(xml);
            if (fromPhone != null)
            {
                Log.WriteLine("Updating contact from phone: [{0}]", fromPhone.CompleteName);

                string id = fromPhone.OutlookEntryId;
                if (!string.IsNullOrEmpty(id))
                {
                    UnifiedContact cached = this.store.FindContactById(fromPhone.OutlookEntryId);
                    bool inserted = false;
                    if (cached == null)
                    {
                        // Oh, user added a new contact then!
                        inserted = true;
                        cached = fromPhone;
                    }
                    else
                    {
                        cached.Merge(fromPhone);
                    }

                    // update the total version number.
                    cached.VersionNumber = cached.GetHighestVersionNumber();

                    // push this to Outlook.
                    string newId = loader.UpdateContact(cached);
                    if (inserted)
                    {
                        cached.OutlookEntryId = newId;
                        SyncStatus.PhoneInserted.Add(new SyncItem(cached));
                        this.store.Contacts.Add(cached);
                    }
                    else
                    {
                        SyncStatus.PhoneUpdated.Add(new SyncItem(cached));
                    }
                    OnPropertyChanged("SyncStatus");

                    return new Message() { Command = "UpdatedContact", Parameters = id + "=>" + newId };
                }
            }

            return new Message() { Command = "UpdatedContact", Parameters = "Parse error or missing id" };
        }

        private Message UpdateAppointment(string xml)
        {
            UnifiedAppointment fromPhone = UnifiedAppointment.Parse(xml);
            if (fromPhone != null)
            {
                string id = fromPhone.PhoneId;
                if (!string.IsNullOrEmpty(id))
                {
                    UnifiedAppointment cached = this.store.FindAppointmentByPhoneId(id);
                    if (cached == null)
                    {
                        // Oh, user added a new contact then!
                        // if a phone sync fails for any reason we could end up with duplicate entries 
                        // so we have to try and match entries here.
                        cached = this.store.FindMatchingAppointment(fromPhone);
                        if (cached == null)
                        {
                            cached = fromPhone;
                            this.store.Appointments.Add(cached);
                        }
                    }
                    else
                    {
                        cached.Merge(fromPhone);
                    }

                    // update the total version number.
                    cached.VersionNumber = cached.GetHighestVersionNumber();

                    // push this to Outlook.
                    string newId = loader.UpdateAppointment(cached);
                    cached.Id = newId;

                    cached.ClearValue("Details"); // don't persist this field.

                    SyncStatus.PhoneUpdated.Add(new SyncItem(cached));
                    OnPropertyChanged("SyncStatus");

                    return new Message() { Command = "UpdatedAppointment", Parameters = id + "=>" + newId };
                }
            }

            return new Message() { Command = "UpdatedAppointment", Parameters = "Parse error or missing id" };
        }

    }
}
