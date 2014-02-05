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

        public async Task SaveAsync(CacheFolder cache)
        {
            try
            {
                if (!saving)
                {
                    saving = true;
                    Log.WriteLine("Saving DataStore at time : " + DateTime.Now.ToString());
                    IsolatedStorage<DataStore> store = new IsolatedStorage<DataStore>(cache);
                    await store.SaveToFileAsync(DataFile, this);
                    saving = false;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Error saving DataStore: " + ex.Message);
            }
        }

    }

    public class CameraInfo : INotifyPropertyChanged
    {
        private string id;
        private string name;
        private string userName;
        private string sysVersion;
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
    }
}
