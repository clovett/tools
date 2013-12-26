using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FoscamExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FoscamDetailsPage : Page
    {
        FoscamDevice device;
        ImageManipulationGesture gesture;

        public FoscamDetailsPage()
        {
            this.InitializeComponent();
            this.ErrorMessage.Text = "";

            MergeWifiItem(new WifiNetworkInfo() { SSID = "disabled", Security = WifiSecurity.None });
            PasswordBoxWifi.IsEnabled = false;

            gesture = new ImageManipulationGesture();
            gesture.DragVectorChanged += OnDragVectorChanged;
            gesture.ZoomDirectionChanged += OnZoomChanged;
            gesture.Start(CameraImage);
        }


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // bring up camera details page...

            CameraInfo camera = (CameraInfo)e.Parameter;
            device = new FoscamDevice() { CameraInfo = camera };
            //var result = await device.GetParams();

            device.Error += OnDeviceError;
            device.FrameAvailable += OnFrameAvailable;
            camera.PropertyChanged += OnCameraPropertyChanged;
            device.StartJpegStream();

            this.DataContext = camera;

            PropertyBag props = await device.GetParams();
            ShowParameters(props);            
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StopWifiScanner();
            FinishUpdate();
            StopMoving();
            base.OnNavigatedFrom(e);
        }

        bool updatingParameters;

        private void ShowParameters(PropertyBag props)
        {
            updatingParameters = true;
            try
            {
                string alias = props.GetValue<string>("alias");
                if (alias != device.CameraInfo.Name)
                {
                    device.CameraInfo.Name = alias;
                }

                string ipAddress = props.GetValue<string>("ip");
                int wifi_enable = props.GetValue<int>("wifi_enable");
                string wifi_ssid = props.GetValue<string>("wifi_ssid");
                WifiSecurity wifi_encrypt = (WifiSecurity)props.GetValue<int>("wifi_encrypt");
                int wifi_authtype = props.GetValue<int>("wifi_authtype");
                int wifi_keyformat = props.GetValue<int>("wifi_keyformat");

                if (!string.IsNullOrEmpty(wifi_ssid))
                {
                    var network = new WifiNetworkInfo()
                    {
                        SSID = wifi_ssid,
                        Mode = WifiMode.Infrastructure,
                        Security = wifi_encrypt
                    };

                    device.CameraInfo.WifiNetwork = network;

                    int i = MergeWifiItem(network);

                    if (wifi_enable == 1)
                    {
                        ComboBoxWifi.SelectedIndex = i;
                        PasswordBoxWifi.IsEnabled = true;
                    }
                }

                if (wifi_enable == 0)
                {
                    ComboBoxWifi.SelectedIndex = 0;
                }

                // ignore all this WEP crap, hopefully user doesn't have 
                string wifi_defkey = props.GetValue<string>("wifi_defkey");
                string wifi_key1 = props.GetValue<string>("wifi_key1");
                string wifi_key2 = props.GetValue<string>("wifi_key2");
                string wifi_key3 = props.GetValue<string>("wifi_key3");
                string wifi_key4 = props.GetValue<string>("wifi_key4");

                string wifi_key1_bits = props.GetValue<string>("wifi_key1_bits");
                string wifi_key2_bits = props.GetValue<string>("wifi_key2_bits");
                string wifi_key3_bits = props.GetValue<string>("wifi_key3_bits");
                string wifi_key4_bits = props.GetValue<string>("wifi_key4_bits");

                // this is where mode 4 key shows up
                string wifi_wpa_psk = props.GetValue<string>("wifi_wpa_psk");

                switch (wifi_encrypt)
                {
                    case WifiSecurity.None:
                        break;
                    case WifiSecurity.WepTkip:
                        break;
                    case WifiSecurity.WpaAes:
                        break;
                    case WifiSecurity.Wpa2Aes:
                        break;
                    case WifiSecurity.Wpa2Tkip:
                        device.CameraInfo.WifiPassword = wifi_wpa_psk;
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                updatingParameters = false;
            }

            StartWifiScan();
        }

        int MergeWifiItem(WifiNetworkInfo info)
        {
            int i = 0;
            foreach (WifiNetworkInfo item in ComboBoxWifi.Items)
            {
                if (item.SSID == info.SSID)
                {
                    item.Security = info.Security;
                    item.Mode = info.Mode;
                    item.BSSID = info.BSSID;
                    return i;
                }
                i++;
            }

            ComboBoxWifi.Items.Add(info);

            return ComboBoxWifi.Items.Count - 1;
        }

        DispatcherTimer wifiScanTimer;

        private void StartWifiScan()
        {
            device.StartScanWifi();
            wifiScanTimer = new DispatcherTimer();
            wifiScanTimer.Interval = TimeSpan.FromSeconds(2);
            wifiScanTimer.Tick += OnWifiScanTick;
            wifiScanTimer.Start();
        }


        private void StopWifiScanner()
        {
            if (wifiScanTimer != null)
            {
                wifiScanTimer.Tick -= OnWifiScanTick;
                wifiScanTimer.Stop();
                wifiScanTimer = null;
            }
        }

        async void OnWifiScanTick(object sender, object e)
        {
            updatingParameters = true;
            try
            {
                var found = await device.GetWifiScan();
                if (found != null)
                {
                    foreach (var network in found)
                    {
                        MergeWifiItem(network);
                    }
                }
            }
            finally
            {
                updatingParameters = false;
            }
        }


        private void GoBack(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        void OnCameraPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }

        void OnFrameAvailable(object sender, FrameReadyEventArgs e)
        {
            CameraImage.Source = e.BitmapSource;
        }

        void OnDeviceError(object sender, ErrorEventArgs e)
        {
            if (e.HttpResponse == System.Net.HttpStatusCode.Unauthorized)
            {
                CameraImage.Source = new BitmapImage(new Uri("ms-appx:/Assets/Padlock.png", UriKind.RelativeOrAbsolute));
            }
            else
            {
                ShowError(e.Message);
            }
        }

        void ShowError(string text)
        {
            var quiet = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                ErrorMessage.Text = text;
            }));
        }

        private void OnNameChanged(object sender, TextChangedEventArgs e)
        {
            // send new name to the camera 
            StartDelayedUpdate();
        }

        private void OnWifiSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!updatingParameters)
            {
                var network = ComboBoxWifi.SelectedItem as WifiNetworkInfo;
                PasswordBoxWifi.IsEnabled = (network.SSID != "disabled");
                ShowError("");
                wifiChanged = true;
                StartDelayedUpdate();
            }
        }

        private void OnWifiPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!updatingParameters && PasswordBoxWifi.Password != this.device.CameraInfo.WifiPassword)
            {
                ShowError("");
                wifiChanged = true;
                StartDelayedUpdate();
            }
        }

        DispatcherTimer updateTimer;
        bool wifiChanged;

        private void StartDelayedUpdate()
        {
            if (updateTimer == null)
            {
                updateTimer = new DispatcherTimer();
                updateTimer.Interval = TimeSpan.FromSeconds(3);
                updateTimer.Tick += OnUpdateTick;
            }
            updateTimer.Stop();
            updateTimer.Start();
        }

        private void StopUpdate()
        {
            if (updateTimer != null)
            {
                updateTimer.Tick -= OnWifiScanTick;
                updateTimer.Stop();
                updateTimer = null;
            }
        }

        private void FinishUpdate()
        {
            if (updateTimer != null)
            {
                // do it now then!
                OnUpdateTick(this, null);
            }
        }

        private async void OnUpdateTick(object sender, object e)
        {
            StopUpdate();
            
            WifiNetworkInfo info = ComboBoxWifi.SelectedItem as WifiNetworkInfo;
            if (info != null && info.SSID == "disabled")
            {
                info = null; 
            }

            device.CameraInfo.WifiNetwork = info;

            string newName = TextBoxName.Text.Trim();
            if (newName.Length > 20)
            {
                newName = newName.Substring(0, 20);
            }
            CameraInfo camera = this.device.CameraInfo;
            if (camera.Name != newName)
            {
                string rc = await device.Rename(newName);
                if (!string.IsNullOrEmpty(rc))
                {
                    ShowError(rc);
                    return;
                }
            }

            if (wifiChanged)
            {
                wifiChanged = false;
                string error = await device.UpdateWifiSettings();
                if (error != null)
                {
                    ShowError(error);
                    return;
                }
            }

            ShowError("updated");
        }

        Vector moveDirection;
        Vector movedSoFar;
        int steps;
        DispatcherTimer moveTimer;
        int moveTimerId;
        Windows.UI.Xaml.Shapes.Path arrowHead;

        private async void OnZoomChanged(object sender, double zoomDirection)
        {
            FoscamDevice d = this.device;
            if (d != null)
            {
                if (zoomDirection < 0)
                {
                    await d.ZoomOut();
                }
                else
                {
                    await d.ZoomIn();
                }
            }
        }

        void OnDragVectorChanged(object sender, Vector e)
        {
            moveDirection = e;
            moveDirection.Normalize();
            movedSoFar = new Vector(0, 0);
            steps = 0;
            if (e.X == 0 && e.Y == 0)
            {
                // stop moving
                StopMoving();
                if (arrowHead != null)
                {
                    ImageOverlay.Children.Remove(arrowHead);
                    arrowHead = null;
                }
            }
            else
            {                
                StartMoving();
                if (arrowHead == null)
                {
                    arrowHead = new Windows.UI.Xaml.Shapes.Path() { Fill = new SolidColorBrush(Color.FromArgb(0xA0, 0xF0, 0xF0, 0xFF)) };
                    ImageOverlay.Children.Add(arrowHead);
                }
                UpdatePathGeometry(arrowHead, e);
            }
        }

        private void UpdatePathGeometry(Windows.UI.Xaml.Shapes.Path arrowHead, Vector vector)
        {
            Point downPos = gesture.MouseDownPosition;
            Point endPos = new Point(downPos.X + vector.X, downPos.Y + vector.Y);
            arrowHead.Data = GeometryUtilities.CreateFatArrow(endPos, downPos, 10, 16, 16);
        }

        private void StartMoving()
        {
            if (moveTimer == null)
            {
                moveTimerId++;
                moveTimer = new DispatcherTimer();
                moveTimer.Interval = TimeSpan.FromMilliseconds(1);
                moveTimer.Tick += OnMoveTick;
                moveTimer.Start();
            }
        }


        async void OnMoveTick(object sender, object e)
        {
            int id = this.moveTimerId;
            DispatcherTimer timer = this.moveTimer;
            if (timer != null)
            {
                timer.Stop();

                // try and make movedSoFar match moveDirection,
                // this is similar to a pixel line drawing algorithm.

                FoscamDevice d = this.device;
                if (d != null)
                {
                    CameraDirection direction = CameraDirection.Right;

                    steps++;

                    // this is where we should have moved to.
                    Point projected = new Point(moveDirection.X * steps, moveDirection.Y * steps);                    

                    if (projected.X.IsAlmost(0))
                    {
                        // vertical
                        direction = (projected.Y < 0) ? CameraDirection.Down : CameraDirection.Up;
                    }
                    else if (projected.Y.IsAlmost(0))
                    {
                        // horizontal
                        direction = (projected.X < 0) ? CameraDirection.Left : CameraDirection.Right;
                    }
                    else if (Math.Abs(projected.Y) > Math.Abs(projected.X))
                    {
                        // favor the vertical
                        if (Math.Abs(movedSoFar.Y) > Math.Abs(projected.Y))
                        {
                            // then time to step in the X direction
                            direction = (projected.X < 0) ? CameraDirection.Left : CameraDirection.Right;
                        }
                        else
                        {
                            // step in the Y direction.
                            direction = (projected.Y < 0) ? CameraDirection.Down : CameraDirection.Up;
                        }
                    }
                    else
                    {
                        // favor the horizontal
                        if (Math.Abs(movedSoFar.X) > Math.Abs(projected.X))
                        {
                            // then time to step in the Y direction
                            direction = (projected.Y < 0) ? CameraDirection.Down : CameraDirection.Up;
                        }
                        else
                        {
                            // step in the X direction.
                            direction = (projected.X < 0) ? CameraDirection.Left : CameraDirection.Right;
                        }
                    }
                    switch (direction)
                    {
                        case CameraDirection.Up:
                            movedSoFar.Y--;
                            break;
                        case CameraDirection.Down:
                            movedSoFar.Y++;
                            break;
                        case CameraDirection.Left:
                            movedSoFar.X--;
                            break;
                        case CameraDirection.Right:
                            movedSoFar.X++;
                            break;
                    }
                    var result = await d.Move(direction);

                    var error = result.GetValue<string>("error");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.WriteLine("Error moving camera: " + error);
                    }
                }
                else
                {
                    StopMoving();
                }


                if (this.moveTimer != null && id == this.moveTimerId)
                {
                    timer.Start();
                }
            }
        }

        private void StopMoving()
        {
            if (moveTimer != null)
            {
                moveTimer.Tick -= OnMoveTick;
                moveTimer.Stop();
                moveTimer = null;
            }
        }

    }
}
