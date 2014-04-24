using FoscamExplorer.Foscam;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FoscamExplorer
{
    /// <summary>
    /// This class contains the app data that is persisted.
    /// </summary>
    [XmlRoot("Data")]
    public class DataStore : INotifyPropertyChanged
    {
        const string DataFile = "data.xml";
        private ObservableCollection<CameraInfo> cameras;

        public event PropertyChangedEventHandler PropertyChanged;

        public DataStore()
        {
            Instance = this;
            Cameras = new ObservableCollection<CameraInfo>();
        }

        /// <summary>
        /// We load only one of these per process, so it's handy to keep a reference here.
        /// </summary>
        public static DataStore Instance { get; private set; }


        [XmlIgnore]
        public bool IsOffline { get; set; }

        [DataMember]
        public ObservableCollection<CameraInfo> Cameras
        {
            get { return cameras; }
            set { cameras = value; OnPropertyChanged("Cameras"); }
        }

        public CameraInfo MergeNewCamera(CameraInfo cam)
        {
            foreach (CameraInfo i in Cameras)
            {
                if (i.Id == cam.Id)
                {
                    // same camera, so grab the new IpAddress and
                    // keep everything else.
                    i.IpAddress = cam.IpAddress;

                    // use our existing CameraInfo object since it is already bound to the UI.
                    return i;
                }
            }

            if (cam.IpAddress != null)
            {
                Log.WriteLine("Found new Foscam Camera at " + cam.IpAddress.ToString());
            }

            // new camera
            cam.IsNew = true;
            Cameras.Add(cam);

            return cam;
        }

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }


        public static async Task<DataStore> LoadAsync(CacheFolder cache)
        {
            IsolatedStorage<DataStore> store = new IsolatedStorage<DataStore>(cache);
            DataStore data = await store.LoadFromFileAsync(DataFile);
            if (data == null)
            {
                data = new DataStore();
            }
            else
            {
                foreach (var info in data.Cameras.ToArray())
                {
                    if (info.StaticImageUrl != null)
                    {
                        data.Cameras.Remove(info);
                    }
                }
            }
            return data;
        }

        bool saving;
        bool savePending;

        public async void SaveAsync(CacheFolder cache)
        {
            savePending = true;
            if (!saving)
            {
                saving = true;
                try
                {
                    savePending = false;
                    Log.WriteLine("Saving DataStore at time : " + DateTime.Now.ToString());
                    IsolatedStorage<DataStore> store = new IsolatedStorage<DataStore>(cache);
                    await store.SaveToFileAsync(DataFile, this);
                    if (savePending)
                    {
                        SaveAsync(cache);
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Error saving DataStore: " + ex.Message);
                }
                finally
                {
                    saving = false;
                }
            }
        }

    }

    public class CameraInfo : INotifyPropertyChanged
    {
        private string id;
        private string name;
        private string userName;
        private string sysVersion;
        private string webUiVersion;
        private string password;
        private string address;
        private bool unauthorized;
        private WifiNetworkInfo network;
        private string wifipassword;
        private int rotation;
        private byte brightness;
        private byte contrast;
        private byte fps;
        private int lastPing;
        private int lastFrame;
        private string imageUrl;
        private string error;
        private bool isNew;
        private bool updating;
        private bool rebooting;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        [DataMember]
        public string Id
        {
            get { return id; }
            set
            {
                if (this.id != value)
                {
                    this.id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        [DataMember]
        public string SystemVersion
        {
            get { return sysVersion; }
            set
            {
                if (this.sysVersion != value)
                {
                    this.sysVersion = value;
                    OnPropertyChanged("SystemVersion");
                }
            }
        }


        [DataMember]
        public string WebUiVersion
        {
            get { return webUiVersion; }
            set
            {
                if (this.webUiVersion != value)
                {
                    this.webUiVersion = value;
                    OnPropertyChanged("WebUiVersion");
                }
            }
        }

        [DataMember]
        public string Name
        {
            get { return name; }
            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        [DataMember]
        public string UserName
        {
            get { return userName; }
            set
            {
                if (this.userName != value)
                {
                    this.userName = value;
                    OnPropertyChanged("UserName");
                }
            }
        }

        [DataMember]
        public string Password
        {
            get { return this.password; }
            set
            {
                if (this.password != value)
                {
                    this.password = value;
                    OnPropertyChanged("Password");
                }
            }
        }


        [DataMember]
        public string IpAddress
        {
            get { return this.address; }
            set
            {
                if (this.address != value)
                {
                    this.address = value;
                    OnPropertyChanged("IpAddress");
                }
            }
        }

        [DataMember]
        public int Rotation
        {
            get { return this.rotation; }
            set
            {
                if (this.rotation != value)
                {
                    this.rotation = value;
                    OnPropertyChanged("Rotation");
                }
            }
        }

        [DataMember]
        public bool Unauthorized
        {
            get { return this.unauthorized; }
            set
            {
                if (this.unauthorized != value)
                {
                    this.unauthorized = value;
                    OnPropertyChanged("Unauthorized");
                }
            }
        }

        [DataMember]
        public WifiNetworkInfo WifiNetwork
        {
            get { return this.network; }
            set
            {
                if (this.network != value)
                {
                    this.network = value;
                    OnPropertyChanged("WifiNetwork");
                }
            }
        }

        [DataMember]
        public string WifiPassword
        {
            get { return this.wifipassword; }
            set
            {
                if (this.wifipassword != value)
                {
                    this.wifipassword = value;
                    OnPropertyChanged("WifiPassword");
                }
            }
        }

        [DataMember]
        public byte Brightness
        {
            get { return this.brightness; }
            set
            {
                if (this.brightness != value)
                {
                    this.brightness = value;
                    OnPropertyChanged("Brightness");
                }
            }
        }

        [DataMember]
        public byte Contrast
        {
            get { return this.contrast; }
            set
            {
                if (this.contrast != value)
                {
                    this.contrast = value;
                    OnPropertyChanged("Contrast");
                }
            }
        }

        [DataMember]
        public bool IsNew
        {
            get { return this.isNew; }
            set
            {
                if (this.isNew != value)
                {
                    this.isNew = value;
                    OnPropertyChanged("IsNew");
                }
            }
        }

        /// <summary>
        /// The frames per second, a number from 0 to 23.  Where 0 is full speed and 23 is 1 frame every 5 seconds.
        /// </summary>
        [DataMember]
        public byte Fps
        {
            get { return this.fps; }
            set
            {
                if (this.fps != value)
                {
                    this.fps = value;
                    OnPropertyChanged("Fps");
                }
            }
        }

        [DataMember]
        public int LastPingTime
        {
            get { return this.lastPing; }
            set
            {
                if (this.lastPing != value)
                {
                    this.lastPing = value;
                    OnPropertyChanged("LastPingTime");
                }
            }
        }

        [DataMember]
        public int LastFrameTime
        {
            get { return this.lastFrame; }
            set
            {
                if (this.lastFrame != value)
                {
                    this.lastFrame = value;
                    OnPropertyChanged("LastFrameTime");
                }
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public bool UpdatingFirmware
        {
            get { return this.updating; }
            set
            {
                if (this.updating != value)
                {
                    this.updating = value;
                    OnPropertyChanged("Updating");
                }
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public bool Rebooting
        {
            get { return this.rebooting; }
            set
            {
                if (this.rebooting != value)
                {
                    this.rebooting = value;
                    OnPropertyChanged("Rebooting");
                }
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public string StaticImageUrl
        {
            get { return this.imageUrl; }
            set
            {
                if (this.imageUrl != value)
                {
                    this.imageUrl = value;
                    OnPropertyChanged("StaticImageUrl");
                }
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public string StaticError
        {
            get { return this.error; }
            set
            {
                if (this.error != value)
                {
                    this.error = value;
                    OnPropertyChanged("StaticError");
                }
            }
        }

        // Cache of last image fetched.
        [XmlIgnore]
        [IgnoreDataMember]
        public object LastFrame { get; set; }

        [XmlIgnore]
        [IgnoreDataMember]
        public  Update UpdateAvailable { get; set; }
    }
}
