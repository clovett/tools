using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace FoscamExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FoscamDetailsPage : Page, ISuspendable
    {
        FoscamDevice device;
        ImageManipulationGesture gesture;
        PropertyBag deviceParams;

        public FoscamDetailsPage()
        {
            this.InitializeComponent();
            this.ErrorMessage.Text = "";

            gesture = new ImageManipulationGesture();
            gesture.DragVectorChanged += OnDragVectorChanged;
            gesture.ZoomDirectionChanged += OnZoomChanged;
            gesture.Start(CameraImage);

            //var manager = new Windows.Media.MediaExtensionManager();
            //manager.RegisterSchemeHandler("AsfStreamSource", "asf");
        }


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // bring up camera details page...
            Log.WriteLine("FoscamDetailsPage OnNavigatedTo");

            CameraInfo camera = (CameraInfo)e.Parameter;
            device = new FoscamDevice() { CameraInfo = camera };
            
            device.Error += OnDeviceError;
            device.FrameAvailable += OnFrameAvailable;
            camera.PropertyChanged += OnCameraPropertyChanged;
            device.StartJpegStream();
                        
            this.DataContext = camera;

            deviceParams = await device.GetParams();
            ShowParameters(deviceParams);

            OnRotationChanged();

            SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;

            var p = await device.GetCameraParams();
            if (p.HasValue("brightness"))
            {
                device.CameraInfo.Brightness = p.GetValue<byte>("brightness");
            }
            if (p.HasValue("contrast"))
            {
                device.CameraInfo.Contrast = p.GetValue<byte>("contrast");
            }
        }


        private void Reconnect()
        {
            if (device != null)
            {
                device.StopStream();
                device.StartJpegStream();
            }
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Disconnect();
            base.OnNavigatedFrom(e);
        }

        private void Disconnect()
        {
            Log.WriteLine("FoscamDetailsPage disconnecting");
            SettingsPane.GetForCurrentView().CommandsRequested -= OnCommandsRequested;
            FinishUpdate();
            StopMoving();
            StopUpdate();

            if (device != null)
            {
                device.Error -= OnDeviceError;
                device.FrameAvailable -= OnFrameAvailable;                
                device.CameraInfo.PropertyChanged -= OnCameraPropertyChanged;
                device.StopStream();
            }
        }

        void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            SettingsCommand accountsCommand = new SettingsCommand("AccountsPageId", "Camera Account", OnChangePasswordCommand);
            args.Request.ApplicationCommands.Add(accountsCommand);
        }

        private void OnChangePasswordCommand(IUICommand command)
        {
            ChangeUserPassword();
        }

        void ChangeUserPassword()
        {
            string user = deviceParams.GetValue<string>("user1_name");
            string pswd = deviceParams.GetValue<string>("user1_pwd");

            LogonPage login = new LogonPage() {
                Title = "Camera Account",
                SignOnButtonCaption = "Update",
                UserName = user,
                Password = pswd 
            };
            login.Flyout(new Action(async () =>
            {
                if (!login.Cancelled && user != login.UserName || pswd != login.Password)
                {
                    await ChangeUserNamePassword(login.UserName, login.Password);
                }
            }));
        }

        private async Task ChangeUserNamePassword(string userName, string password)
        {
            var result = await device.ChangeUserPassword(userName, password);
            if (!string.IsNullOrEmpty(result))
            {
                ShowError(result);
            }
            else
            {
                ShowError("updated");

                device.CameraInfo.UserName = userName;
                device.CameraInfo.Password = password;
            }
        }

        async Task Save()
        {
            await DataStore.Instance.SaveAsync(((FoscamExplorer.App)App.Current).CacheFolder); ;
        }

        private void ShowParameters(PropertyBag props)
        {
            string alias = props.GetValue<string>("alias");
            if (alias != device.CameraInfo.Name)
            {
                device.CameraInfo.Name = alias;
            }

            int wifi_enable = props.GetValue<int>("wifi_enable");
            string wifi_ssid = props.GetValue<string>("wifi_ssid");
            WifiSecurity wifi_encrypt = (WifiSecurity)props.GetValue<int>("wifi_encrypt");
            int wifi_authtype = props.GetValue<int>("wifi_authtype");
            int wifi_keyformat = props.GetValue<int>("wifi_keyformat");

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

            if (!string.IsNullOrEmpty(wifi_ssid))
            {
                var network = new WifiNetworkInfo()
                {
                    SSID = wifi_ssid,
                    Mode = WifiMode.Infrastructure,
                    Security = wifi_encrypt
                };

                device.CameraInfo.WifiNetwork = network;
            }            

        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        async void OnCameraPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "UserName":
                case "Password":
                case "Fps":
                    Reconnect();
                    break;
                case "Flipped":
                    OnRotationChanged();
                    break;
            }

            // save the changes
            await Save();
        }

        int lastFrameTime;

        void OnFrameAvailable(object sender, FrameReadyEventArgs e)
        {
            lastFrameTime = Environment.TickCount;
            CameraImage.Source = e.BitmapSource;
        }

        void OnDeviceError(object sender, ErrorEventArgs e)
        {
            if (e.HttpResponse == System.Net.HttpStatusCode.Unauthorized)
            {
                CameraImage.Source = new BitmapImage(new Uri("ms-appx:/Assets/Padlock.png", UriKind.RelativeOrAbsolute));
            }
            else if (e.HttpResponse == System.Net.HttpStatusCode.ServiceUnavailable && lastFrameTime != 0 && lastFrameTime + 1000 < Environment.TickCount)
            {
                // one retry - this handles the case where the computer comes out of sleep since we get no other events telling us this.
                lastFrameTime = Environment.TickCount;
                Reconnect();
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

        DispatcherTimer updateTimer;

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
                updateTimer.Tick -= OnUpdateTick;
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

            ShowError("updated");
        }

        Vector moveDirection;
        Vector movedSoFar;
        int steps;
        DispatcherTimer moveTimer;
        int moveTimerId;
        Path arrowHead;
        int msPerMove;
        int lastMove;

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
                Point downPos = gesture.MouseDownPosition;
                Point endPos = new Point(downPos.X + e.X, downPos.Y + e.Y);
                Vector v = new Vector(downPos, endPos);
                double length = v.Length;
                double msDelay = (10000 / length);
                if (msDelay < 1) msDelay = 1;
                if (msDelay > 1000) msDelay = 1000;

                StartMoving((int)msDelay);

                moveTimer.Interval = TimeSpan.FromMilliseconds(1);

                if (arrowHead == null)
                {
                    arrowHead = new Path() { Fill = new SolidColorBrush(Color.FromArgb(0xA0, 0xF0, 0xF0, 0xFF)) };
                    ImageOverlay.Children.Add(arrowHead);
                }

                UpdatePathGeometry(arrowHead, downPos, endPos);
            }
        }

        private void UpdatePathGeometry(Path arrowHead, Point start, Point end)
        {
            arrowHead.Data = GeometryUtilities.CreateFatArrow(end, start, 10, 16, 16);
        }

        private void StartMoving(int interval)
        {
            if (moveTimer == null)
            {
                moveTimerId++;
                moveTimer = new DispatcherTimer();
                moveTimer.Tick += OnMoveTick;
                moveTimer.Interval = TimeSpan.FromMilliseconds(1);
                moveTimer.Start();
            }
            msPerMove = interval;
        }


        async void OnMoveTick(object sender, object e)
        {
            int id = this.moveTimerId;
            DispatcherTimer timer = this.moveTimer;
            if (timer != null)
            {
                int now = Environment.TickCount;
                int diff = now - lastMove;
                if (diff < msPerMove)
                {
                    return;
                }
                lastMove = now;
                
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
                        Log.WriteLine("Error moving camera: " + error);
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

        private void OnWifiSettings(object sender, RoutedEventArgs e)
        {
            WifiSettingsPage page = new WifiSettingsPage()
            {
                FoscamDevice = this.device
            };

            page.Flyout(new Action(() =>
            {
            }));
        }

        private void OnUserAccountSettings(object sender, RoutedEventArgs e)
        {
            ChangeUserPassword();
        }

        private void OnRotateCamera(object sender, RoutedEventArgs e)
        {
            device.CameraInfo.Flipped = !device.CameraInfo.Flipped;
        }

        private void OnRotationChanged()
        {
            bool flip = device.CameraInfo.Flipped;
            if (flip)
            {
                CameraImage.RenderTransform = new RotateTransform() { Angle = 180, CenterX = CameraImage.Width / 2, CenterY = CameraImage.Height / 2 };
            }
            else
            {
                CameraImage.RenderTransform = null;
            }
        }

        private void OnCameraSettings(object sender, RoutedEventArgs e)
        {
            CameraSettingsPage page = new CameraSettingsPage()
            {
                FoscamDevice = this.device
            };

            page.Flyout(new Action(() =>
            {
            }));
        }


        public void OnSuspending()
        {
            Disconnect();
        }
    }
}
