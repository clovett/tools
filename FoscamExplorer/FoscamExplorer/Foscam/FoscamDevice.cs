using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

#if WINDOWS_PHONE
using CoreDispatcher = System.Windows.Threading.Dispatcher;
#else
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
#endif

namespace FoscamExplorer.Foscam
{
    public enum CameraDirection
    {
        Up,
        Down = 2,
        Left = 4,
        Right = 6
    };

    public enum CameraFps
    {
        Max = 0,
        Fps20 = 1,
        Fps15 = 3,
        Fps10 = 6,
        Fps5 = 11,
        Fps4 = 12,
        Fps3 = 13,
        Fps2 = 14,
        Fps1 = 15,
        FpsHalf = 17,
        FpsThird = 19,
        FpsQuarter = 21,
        FpsFifth = 23
    }
    
    public class FpsInfo
    {
        public string Caption;
        public CameraFps Value;
        public double MillisecondsPerFrame;

        public FpsInfo(string caption, CameraFps value, double msPerFrame)
        {
            Caption = caption;
            Value = value;
            MillisecondsPerFrame = msPerFrame;
        }

        public override string ToString()
        {
            return Caption;
        }
    };

    public class FoscamDevice
    {
        public static FpsInfo[] FpsItems = new FpsInfo[] 
        {
            new FpsInfo("Max", CameraFps.Max, 1000/20),
            new FpsInfo("20 fps", CameraFps.Fps20, 1000/20),
            new FpsInfo("15 fps", CameraFps.Fps15, 1000/15),
            new FpsInfo("10 fps", CameraFps.Fps10, 1000/10),
            new FpsInfo("5 fps", CameraFps.Fps5, 1000/5),
            new FpsInfo("4 fps",CameraFps.Fps4, 1000/4),
            new FpsInfo("3 fps", CameraFps.Fps3, 1000/3),
            new FpsInfo("2 fps", CameraFps.Fps2, 1000/2),
            new FpsInfo("1 fps", CameraFps.Fps1, 1000/1),
            new FpsInfo("1/2 fps", CameraFps.FpsHalf, 1000*2),
            new FpsInfo("1/3 fps", CameraFps.FpsThird, 1000*3),
            new FpsInfo("1/4 fps", CameraFps.FpsQuarter, 1000*4),
            new FpsInfo("1/5 fps", CameraFps.FpsFifth, 1000*5)
        };

        public CameraInfo CameraInfo { get; set; }

        private string CameraUrl
        {
            get { return string.IsNullOrEmpty(CameraInfo.IpAddress) ? null : "http://" + CameraInfo.IpAddress + "/"; }
        }

        public event EventHandler<ErrorEventArgs> Error;

        public event EventHandler<FrameReadyEventArgs> FrameAvailable;

        public static event EventHandler<FoscamDevice> DeviceAvailable;

        private MjpegDecoder mjpeg;        

        const int m_portNumber = 10000;
        const int m_offset_MAC = 0x17;
        const int m_length_MAC = 12;
        const int m_offset_Camera_IP = 0x39;
        const int m_offset_router_IP = 0x41;
        const int m_length_IP = 4;

        static byte[] m_request = 
            {
                0x4D, 0x4F, 0x5F, 0x49, 0x00, 0x00, 0x00, 0x00
                , 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04 
                , 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00
                , 0x00, 0x00, 0x01
            };

        public static void StartFindDevices()
        {
            if (findDevicesCancellationSource == null || findDevicesCancellationSource.IsCancellationRequested)
            {
                findDevicesCancellationSource = new CancellationTokenSource();
                var result = Task.Run(new Action(FindDevices));
            }
        }

        static CancellationTokenSource findDevicesCancellationSource;
        static ManualResetEvent cancelled = new ManualResetEvent(false);
        static bool findRunning;

        private static void FindDevices()
        {
            findRunning = true;
            var cancellationToken = findDevicesCancellationSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                // send out the UDP ping every few seconds.
                Guid adapter = Guid.Empty;
                foreach (var hostName in Windows.Networking.Connectivity.NetworkInformation.GetHostNames())
                {
                    if (hostName.IPInformation != null)
                    {
                        // blast it out on all local hosts, since user may be connected to the network in multiple ways
                        // and some of these hostnames might be virtual ethernets that go no where, like 169.254.80.80.
                        try
                        {
                            if (hostName.IPInformation.NetworkAdapter != null && !hostName.CanonicalName.StartsWith("169."))
                            {
                                SendUdpPing(hostName).Wait();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Ping failed: " + ex.Message);
                        }
                    }
                }
                try
                {
                    Task.Delay(3000).Wait(cancellationToken);
                }
                catch
                {
                }
            }
            cancelled.Set();
            findRunning = false;
        }

        public static void StopFindingDevices()
        {
            if (findDevicesCancellationSource != null)
            {
                findDevicesCancellationSource.Cancel();
                // wait for FindDevices to terminate so we can tear down the sockets cleanly.
                if (findRunning)
                {
                    cancelled.WaitOne(5000);
                }
            }
            foreach (var socket in sockets.Values)
            {
                // dispose the socket.
                using (socket)
                {                    
                }
            }
            sockets.Clear();
        }

        static Dictionary<string, DatagramSocket> sockets = new Dictionary<string, DatagramSocket>();

        private static async Task SendUdpPing(HostName hostName)
        {
            Debug.WriteLine(DateTime.Now.TimeOfDay.ToString() + ": SendUdpPing");
            string ipAddress = hostName.CanonicalName;
            DatagramSocket socket;
            if(!sockets.TryGetValue(ipAddress, out socket))
            {
                // setup the socket for this network adapter.
                socket = new DatagramSocket();
                socket.MessageReceived += OnDatagramMessageReceived;
                sockets[ipAddress] = socket;
                // the foscam only responds when the source port is also m_portNumber.
                await socket.BindEndpointAsync(hostName, m_portNumber.ToString());
            }

            using (IOutputStream os = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), m_portNumber.ToString()))
            {
                DataWriter writer = new DataWriter(os);
                writer.WriteBytes(m_request);
                await writer.StoreAsync();
            }
        }

        static void OnDatagramMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var remoteHost = args.RemoteAddress;

            int expectedResponseLength = m_offset_router_IP + 4;

            var reader = args.GetDataReader();
            uint bytesRead = reader.UnconsumedBufferLength;

            byte[] data = new byte[bytesRead];
            reader.ReadBytes(data);

            if (bytesRead < expectedResponseLength)
            {
                return;
            }

            FoscamDevice device = CreateDevice(data);

            if (DeviceAvailable != null)
            {
                DeviceAvailable(device, device);
            }
        }

        private static FoscamDevice CreateDevice(byte[] response)
        {
            var macAddress = GetString(response, m_offset_MAC, m_length_MAC);
            var cameraIP = GetIPAddressString(response, m_offset_Camera_IP);
            var routerIP = GetIPAddressString(response, m_offset_router_IP);

            Debug.WriteLine("Found device at " + cameraIP);

            var device = new FoscamDevice()
            {
                CameraInfo = new CameraInfo()
                {
                    Name = "Foscam: " + cameraIP.ToString(),
                    Id = macAddress,
                    IpAddress = cameraIP
                }
            };

            return device;
        }

        private static byte[] GetSubArray(byte[] array, int offset, int count)
        {
            byte[] ans = new byte[count];
            System.Array.Copy(array, offset, ans, 0, count);
            return ans;
        }

        private static string GetString(byte[] response, int offset, int count)
        {
            var buf = GetSubArray(response, offset, count);
            return Encoding.UTF8.GetString(buf, 0, buf.Length);
        }

        private static string GetIPAddressString(byte[] response, int offset)
        {
            byte[] buf = GetSubArray(response, offset, m_length_IP);
            string ipv4 = buf[0] + "." + buf[1] + "." + buf[2] + "." + buf[3];
            return ipv4;
        }

        private NetworkCredential GetCredentials()
        {
            //check the username and password
            if (CameraInfo != null && !string.IsNullOrEmpty(CameraInfo.UserName))
            {
                return new NetworkCredential(CameraInfo.UserName, CameraInfo.Password);
            }
            return null;
        }

        public async Task GetSnapshot()
        {
            string requestStr = String.Format("http://{0}/snapshot.cgi", CameraInfo.IpAddress);
            
            try
            {
                using (var stream = await GetFileRequest(requestStr))
                {
                    var frame = await WpfUtilities.LoadImageAsync(stream);
                    if (frame != null)
                    {
                        this.CameraInfo.Unauthorized = false;
                    }

                    // tell whoever's listening that we have a frame to draw
                    if (FrameAvailable != null)
                    {
                        FrameAvailable(this, new FrameReadyEventArgs { BitmapSource = frame });
                    }
                }                
            }
            catch (Exception e)
            {
                OnError(this, new ErrorEventArgs() { Message = e.Message });
                Log.WriteLine("{0}: error getting snapshot: {1}", this.ToString(), e.ToString());
            }
        }

        /// <summary>
        /// Start receiving jpeg frames via the FrameAvailable event.
        /// </summary>
        /// <param name="dispatcher">The UI dispatcher</param>
        /// <param name="sizeHint">The size you plan to display, this is just a hint</param>
        /// <param name="fps">The fps setting, defaults to maximum FPS</param>
        public void StartJpegStream(CoreDispatcher dispatcher, int sizeHint = 640, CameraFps fps = CameraFps.Max)
        {
            int resolution = (sizeHint <= 320) ? 8 : 32;

            if (CameraUrl == null)
            {
                return;
            }

            try
            {
                mjpeg = new MjpegDecoder(dispatcher);
                mjpeg.FrameReady += OnFrameReady;
                mjpeg.Error += OnError;

                string requestStr = String.Format("{0}videostream.cgi?resolution={1}&rate={2}", CameraUrl, resolution, (byte)fps);

                int msPerFrames = GetMillisecondsPerFrame(fps);

                mjpeg.GetVideoStream(new Uri(requestStr), msPerFrames, GetCredentials());
            }
            catch (Exception e)
            {
                OnError(this, new ErrorEventArgs() { Message = e.Message });
                Log.WriteLine("{0}: couldn't talk to the camera. are the arguments correct?\n exception details: {1}", this.ToString(), e.ToString());
                throw;
            }
        }

        private int GetMillisecondsPerFrame(CameraFps fps)
        {
            double milliseconds = 50;

            foreach (FpsInfo i in FpsItems)
            {
                if (i.Value >= fps)
                {
                    milliseconds = i.MillisecondsPerFrame;
                    break;
                }
            }

            return (int)milliseconds;
        }


        public async Task<PropertyBag> GetParams()
        {
            string requestStr = String.Format("http://{0}/get_params.cgi", CameraInfo.IpAddress);
            return await SendCgiRequest(requestStr);
        }

        public async Task<PropertyBag> GetCameraParams()
        {
            string requestStr = String.Format("http://{0}/get_camera_params.cgi", CameraInfo.IpAddress);
            return await SendCgiRequest(requestStr);
        }

        public async Task<PropertyBag> GetStatus()
        {
            string requestStr = String.Format("http://{0}/get_status.cgi", CameraInfo.IpAddress);
            return await SendCgiRequest(requestStr);
        }

        public async Task<PropertyBag> Move(CameraDirection dir)
        {
            string requestStr = String.Format("http://{0}/decoder_control.cgi?command={1}&onestep=1", CameraInfo.IpAddress, (int)dir);
            return await SendCgiRequest(requestStr);
        }

        public async Task<PropertyBag> ZoomIn()
        {
            string requestStr = String.Format("http://{0}/decoder_control.cgi?command=16&onestep=1", CameraInfo.IpAddress);
            return await SendCgiRequest(requestStr);
        }

        public async Task<PropertyBag> ZoomOut()
        {
            string requestStr = String.Format("http://{0}/decoder_control.cgi?command=18&onestep=1", CameraInfo.IpAddress);
            return await SendCgiRequest(requestStr);
        }

        enum ParameterType
        {
            Resolution,
            Brightness,
            Contrast,
            Mode,
            Patrol
        }

        public async Task<string> SetBrightness(byte brightness)
        {
            return await SetCameraControl(ParameterType.Brightness, brightness.ToString());
        }

        public async Task<string> SetContrast(byte contrast)
        {
            contrast = Math.Min((byte)6, contrast);
            return await SetCameraControl(ParameterType.Contrast, contrast.ToString());
        }

        private async Task<string> SetCameraControl(ParameterType paramType, string value)
        {
            string requestStr = String.Format("http://{0}/camera_control.cgi?param={1}&value={2}", CameraInfo.IpAddress, (int)paramType, value);
            var result = await SendCgiRequest(requestStr);
            return result.GetValue<string>("Error");
        }

        public async Task UploadFirmware(Stream binFile)
        {
            CameraInfo.UpdatingFirmware = true;
            try
            {
                string requestStr = String.Format("http://{0}/upgrade_firmware.cgi", CameraInfo.IpAddress);
                await PutFileRequest(requestStr, binFile);
            }
            finally
            {
                // actually, let's wait for next ping to succeed which means the reboot is finished.
                // CameraInfo.UpdatingFirmware = false;
            }
        }

        public async Task RebootDevice()
        {
            CameraInfo.Rebooting = true;
            string requestStr = String.Format("http://{0}/reboot.cgi", CameraInfo.IpAddress);
            await SendCgiRequest(requestStr);
        }

        private async Task<PropertyBag> SendCgiRequest(string url)
        {
            Debug.WriteLine(DateTime.Now.TimeOfDay.ToString() + ": " +  url);

            dynamic map = new object();
            
            PropertyBag result = new PropertyBag();

            try
            {                
                HttpClientHandler settings = new HttpClientHandler();
                settings.Credentials = GetCredentials();

                HttpClient client = new HttpClient(settings);
                using (HttpResponseMessage msg = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead))
                {
                    if (msg.StatusCode == HttpStatusCode.OK)
                    {
                        HttpContent content = msg.Content;
                        string text = await content.ReadAsStringAsync();
                        result = ParseCgiResult(text);
                    }
                    else
                    {
                        result["Error"] = msg.StatusCode.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                HttpRequestException re = ex as HttpRequestException;
                if (re != null)
                {
                    WebException we = re.InnerException as WebException;
                    if (we != null)
                    {
                        result["Error"] = we.Message;
                        result["StatusCode"] = we.Status.ToString();
                    }
                    else
                    {
                        result["Error"] = ex.Message;           
                    }
                }
                else
                {
                    result["Error"] = ex.Message;                    
                }
            }

            return result;
        }


        private async Task<Stream> GetFileRequest(string url)
        {
            Debug.WriteLine(DateTime.Now.TimeOfDay.ToString() + ": " + url);

            dynamic map = new object();

            HttpClientHandler settings = new HttpClientHandler();
            settings.Credentials = GetCredentials();

            HttpClient client = new HttpClient(settings);
            using (HttpResponseMessage msg = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead))
            {
                if (msg.StatusCode == HttpStatusCode.OK)
                {
                    HttpContent content = msg.Content;
                    using (Stream stream = await content.ReadAsStreamAsync())
                    {
                        MemoryStream ms = new MemoryStream();
                        byte[] block = new byte[64000];
                        int len = 0;
                        while ((len = stream.Read(block, 0, block.Length)) > 0)
                        {
                            ms.Write(block, 0, len);
                        }
                        ms.Seek(0, SeekOrigin.Begin);
                        return ms;
                    }
                }
                else if (msg.StatusCode == HttpStatusCode.Unauthorized)
                {
                    this.CameraInfo.Unauthorized = true;
                }
            }

            return null;
        }

        private async Task PutFileRequest(string url, Stream fileContent)
        {
            Debug.WriteLine(DateTime.Now.TimeOfDay.ToString() + ": " + url);

            dynamic map = new object();

            HttpClientHandler settings = new HttpClientHandler();
            settings.Credentials = GetCredentials();

            HttpContent content = new StreamContent(fileContent);

            HttpClient client = new HttpClient(settings);
            using (HttpResponseMessage msg = await client.PostAsync(new Uri(url), content))
            {
                if (msg.StatusCode == HttpStatusCode.OK)
                {
                    // great!
                }
                else if (msg.StatusCode == HttpStatusCode.Unauthorized)
                {
                    this.CameraInfo.Unauthorized = true;
                }
                else
                {
                    // upload failed... so now what?
                    throw new Exception("File upload to FoscamDevice failed");
                }
            }
        }


        void OnError(object sender, ErrorEventArgs e)
        {
            if (e.HttpResponse == HttpStatusCode.Unauthorized)
            {
                this.CameraInfo.Unauthorized = true;
            }

            if (Error != null)
            {
                Error(this, e);
            }
        }

        void OnFrameReady(object sender, FrameReadyEventArgs e)
        {
            if (e.BitmapSource != null)
            {
                this.CameraInfo.Unauthorized = false;
            }
            if (FrameAvailable != null)
            {
                FrameAvailable(sender, e);
            }
        }


        internal void StopStream()
        {
            if (mjpeg != null)
            {
                mjpeg.FrameReady -= OnFrameReady;
                mjpeg.Error -= OnError;
                mjpeg.StopStream();
            }
        }

        internal PropertyBag ParseCgiResult(string text)
        {
            // parses the foscam camera *.cgi result in the form:
            //var id='00626E4A3CD0';
            //var sys_ver='11.37.2.49';
            //var app_ver='2.0.10.4';
            //var alias='LivingRoom';
            //var now=1387823972;
            //var tz=28800;
            //var alarm_status=0;
            //var ddns_status=0;
            //var ddns_host='cg7906.myfoscam.org';
            //var oray_type=0;
            //var upnp_status=0;
            //var p2p_status=0;
            //var p2p_local_port=25486;
            //var msn_status=0;
            //var wifi_status=1;
            //var temperature=0.0;
            //var humidity=0;
            //var tridro_error='';

            // or arrays like this:
            //var ap_bssid=new Array();
            //var ap_ssid=new Array(); var
            //ap_mode=new Array(); var
            //ap_security=new Array();
            //ap_bssid[0]='0015ebbe2153';
            //ap_ssid[0]='ZXDSL531BII-BE2153';
            //ap_mode[0]=0;

            PropertyBag result = new PropertyBag();

            if (text.StartsWith("error:"))
            {
                result["Error"] = text;
                return result;
            }

            using (StringReader reader = new StringReader(text))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("var"))
                    {
                        line = line.Substring(3).Trim(' ', ';');
                        int equals = line.IndexOf('=');
                        if (equals >= 0)
                        {
                            string name = line.Substring(0, equals);
                            string value = line.Substring(equals + 1).Trim();
                            if (value.StartsWith("\'"))
                            {
                                // it is a string literal                                
                                result[name] = value.Trim('\'');
                            }
                            else if (value.StartsWith("new Array()"))
                            {
                                result[name] = new List<object>();
                            }
                            else
                            {
                                // numeric
                                double d = 0;
                                double.TryParse(value, out d);
                                result[name] = d;
                            }
                        }
                    }
                    else
                    {
                        // check for array syntax
                        //  ap_bssid[0]='0015ebbe2153';
                        int squareBracket = line.IndexOf('[');
                        if (squareBracket > 0)
                        {
                            squareBracket++;
                            string name = line.Substring(0, squareBracket - 1).Trim();

                            object listValue = null;
                            result.TryGetValue(name, out listValue);
                            if (listValue == null)
                            {
                                listValue = new object();
                                result[name] = listValue;
                            }
                            List<object> list = (List<object>)listValue;

                            int closeBracket = line.IndexOf(']', squareBracket);
                            if (closeBracket > squareBracket)
                            {
                                string indexString = line.Substring(squareBracket, closeBracket - squareBracket);
                                int i = 0;
                                int.TryParse(indexString, out i);
                                int equals = line.IndexOf('=', closeBracket);
                                if (equals > 0)
                                {
                                    string value = line.Substring(equals + 1).Trim(' ', ';');
                                    object parsedValue = null;
                                    if (value.StartsWith("\'"))
                                    {
                                        // it is a string literal                                
                                        parsedValue = value.Trim('\'');
                                    }
                                    else
                                    {
                                        // numeric
                                        double d = 0;
                                        double.TryParse(value, out d);
                                        parsedValue = d;
                                    }
                                    list.Add(parsedValue);
                                }
                            }
                        }
                    }

                    line = reader.ReadLine();
                }
            }

            result["result"] = text;

            return result;
        }

        internal async Task<string> Rename(string newName)
        {
            string requestStr = String.Format("http://{0}/set_alias.cgi?alias={1}", CameraInfo.IpAddress, newName);
            PropertyBag result = await SendCgiRequest(requestStr);
            string rc = result.GetValue<string>("Error");
            if (!string.IsNullOrEmpty(rc))
            {
                return rc;
            }

            rc = result.GetValue<string>("result");
            if (rc.StartsWith("ok"))
            {
                this.CameraInfo.Name = newName;
                return null;
            }
            else
            {
                return rc;
            }
        }

        internal async void StartScanWifi()
        {
            // kick off the scan.
            string requestStr = String.Format("http://{0}/wifi_scan.cgi", CameraInfo.IpAddress);
            var properties = await SendCgiRequest(requestStr);
            string rc = (string)properties["result"];
        }

        internal async Task<List<WifiNetworkInfo>> GetWifiScan()
        {
            List<WifiNetworkInfo> result = new List<WifiNetworkInfo>();

            string requestStr = String.Format("http://{0}/get_wifi_scan_result.cgi", CameraInfo.IpAddress);
            var    properties = await SendCgiRequest(requestStr);
            var error = properties.GetValue<string>("Error");
            if (!string.IsNullOrEmpty(error))
            {
                // not ready yet...
                return result;
            }
                                
            uint num = properties.GetValue<uint>("ap_number");
            for (uint i = 0; i < num; i++)
            {
                string bssid = properties.GetListItem<string>("ap_bssid", i);
                string ssid = properties.GetListItem<string>("ap_ssid", i);
                int mode = properties.GetListItem<int>("ap_mode", i);
                int security = properties.GetListItem<int>("ap_security", i);
                if (!string.IsNullOrEmpty(ssid))
                {
                    result.Add(new WifiNetworkInfo()
                    {
                        BSSID = bssid,
                        SSID = ssid,
                        Mode = (WifiMode)mode,
                        Security = (WifiSecurity)security
                    });
                }
            }

            return result;
        }



        internal async Task<string> UpdateWifiSettings()
        {
            string requestStr = null;
            var network = CameraInfo.WifiNetwork;
            if (network == null)
            {
                requestStr = String.Format("http://{0}/set_wifi.cgi?enable=0&ssid=&encrypt=0&defkey=&key1=&key2=&key3=&key4=&authtype=&keyformat=&key1_bits=&key2_bits=&key3_bits=&key4_bits=&channel=&mode=&wpa_psk=", CameraInfo.IpAddress);
            }
            else
            {
                requestStr = String.Format("http://{0}/set_wifi.cgi?enable=1&ssid={1}&encrypt={2}&defkey=&key1=&key2=&key3=&key4=&authtype=&keyformat=&key1_bits=&key2_bits=&key3_bits=&key4_bits=&channel=&mode=&wpa_psk={3}", CameraInfo.IpAddress, network.SSID, (int)network.Security, CameraInfo.WifiPassword);
            }

            var properties = await SendCgiRequest(requestStr);

            string error = properties.GetValue<string>("Error");
            return error;
        }

        internal async Task<string> ChangeUserPassword(string userName, string password, int userIndex = 1, UserPermissions permission = UserPermissions.Administrator)
        {
            if (userIndex < 1 || userIndex > 8)
            {
                throw new IndexOutOfRangeException("userIndex valid range is 1 through 8");
            }

            var parameters = await GetParams();
            StringBuilder sb = new StringBuilder(String.Format("http://{0}/set_users.cgi?", CameraInfo.IpAddress));

            // preserve the other names
            for (int i = 1; i <= 8; i++)
            {
                if (i > 1)
                {
                    sb.Append("&");
                }
                if (i == userIndex)
                {
                    sb.Append(string.Format("user{0}={1}&pwd{0}={2}&pri{0}={3}", 1, userName, password, (int)permission));
                }
                else
                {
                    string name = parameters.GetValue<string>("user" + i + "_name");
                    string pwd = parameters.GetValue<string>("user" + i + "_pwd");
                    string pri = parameters.GetValue<string>("user" + i + "_pri");
                    sb.Append(string.Format("user{0}={1}&pwd{0}={2}&pri{0}={3}", i, name, pwd, pri));
                }
            }

            var properties = await SendCgiRequest(sb.ToString());
            string error = properties.GetValue<string>("Error");
            return error;
        }
    }

    public enum UserPermissions
    {
        Visitor = 0,
        Operator = 1,
        Administrator = 2
    }

    public class WifiNetworkInfo
    {
        public string SSID { get; set; }
        public string BSSID { get; set; }
        public WifiMode Mode { get; set; }
        public WifiSecurity Security { get; set; }

        public override string ToString()
        {
            return SSID;
        }
    }

    public enum WifiMode
    {
        Infrastructure,
        AdHoc
    }

    public enum WifiSecurity
    {
        None,
        WepTkip,
        WpaAes,
        Wpa2Aes,
        Wpa2Tkip,        
    }

    public class PropertyBag : Dictionary<string, object>
    {
        internal T GetValue<T>(string p)
        {
            object v = null;
            TryGetValue(p, out v);
            if (v == null)
            {
                return default(T);
            }
            return ConvertToType<T>(v);
        }

        internal bool HasValue(string name)
        {
            object v = null;
            return TryGetValue(name, out v);
        }

        internal T GetListItem<T>(string listName, uint index)
        {
            object listObj;
            
            if (TryGetValue(listName, out listObj))
            {
                List<object> list = listObj as List<object>;
                if (list != null && index < list.Count)
                {
                    object v = list[(int)index];
                    return ConvertToType<T>(v);
                }
            }
            return default(T);
        }

        internal T ConvertToType<T>(object value)
        {
            if (value is string)
            {
                string s = (string)value;
                return ConvertStringTo<T>(s);
            }
            else
            {
                double d = (double)value;
                return ConvertDoubleTo<T>(d);
            }
        }

        internal T ConvertDoubleTo<T>(double value)
        {
            object v = (object)value;
            if (typeof(T) == typeof(double))
            {
                // done
            }
            else if (typeof(T) == typeof(int))
            {
                v = (int)value;
            }
            else if (typeof(T) == typeof(uint))
            {
                v = (uint)value;
            }
            else if (typeof(T) == typeof(float))
            {
                v = (float)value;
            }
            else if (typeof(T) == typeof(byte))
            {
                v = (byte)value;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                v = (sbyte)value;
            }
            else if (typeof(T) == typeof(short))
            {
                v = (short)value;
            }
            else if (typeof(T) == typeof(ushort))
            {
                v = (ushort)value;
            }
            else if (typeof(T) == typeof(long))
            {
                v = (long)value;
            }
            else if (typeof(T) == typeof(ulong))
            {
                v = (ulong)value;
            }
            else if (typeof(T) == typeof(string))
            {
                v = value.ToString();
            }
            else if (typeof(T) == typeof(string))
            {
                // type not supported.
                return default(T);
            }
            return (T)v;
        }

        internal T ConvertStringTo<T>(string value)
        {
            object v = (object)value;
            if (typeof(T) == typeof(string))
            {
                // done
            }
            else if (typeof(T) == typeof(int))
            {
                int i;
                int.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(uint))
            {
                uint i;
                uint.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(float))
            {
                float i;
                float.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(byte))
            {
                byte i;
                byte.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                sbyte i;
                sbyte.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(short))
            {
                short i;
                short.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(ushort))
            {
                ushort i;
                ushort.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(long))
            {
                long i;
                long.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(ulong))
            {
                ulong i;
                ulong.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime i = DateTime.MinValue;
                DateTime.TryParse(value, out i);
                v = i;
            }
            else if (typeof(T) == typeof(TimeSpan))
            {
                TimeSpan i = TimeSpan.MinValue;
                TimeSpan.TryParse(value, out i);
                v = i;
            }
            return (T)v;
        }

    }
}
