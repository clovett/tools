using FoscamExplorer.Foscam;
using System;
using System.Collections.Generic;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace FoscamExplorer
{
    public sealed partial class FoscamCameraPreview : UserControl
    {
        FoscamDevice device;

        public FoscamCameraPreview()
        {
            this.InitializeComponent();
            this.Unloaded += FoscamCameraPreview_Unloaded;            
        }

        void FoscamCameraPreview_Unloaded(object sender, RoutedEventArgs e)
        {
            device.CameraInfo.PropertyChanged -= OnCameraPropertyChanged;
            device.Error -= OnDeviceError;
            device.FrameAvailable -= OnFrameAvailable;
            device.StopStream();
            unloaded = true;
            StopUpdateTimer();
        }

        public CameraInfo Camera
        {
            get { return (CameraInfo)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Camera.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CameraProperty =
            DependencyProperty.Register("Camera", typeof(CameraInfo), typeof(FoscamCameraPreview), new PropertyMetadata(null, new PropertyChangedCallback(OnCameraChanged)));

        static void OnCameraChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FoscamCameraPreview)d).OnCameraChanged((CameraInfo)e.OldValue, (CameraInfo)e.NewValue);
        }

        void OnCameraChanged(CameraInfo oldCameara, CameraInfo newCamera)
        {
            if (device != null)
            {
                oldCameara.PropertyChanged -= OnCameraPropertyChanged;
                device.Error -= OnDeviceError;
                device.FrameAvailable -= OnFrameAvailable;
                device.StopStream();
            }

            if (newCamera != null)
            {
                device = new FoscamDevice() { CameraInfo = newCamera };
                device.Error += OnDeviceError;
                device.FrameAvailable += OnFrameAvailable;
                newCamera.PropertyChanged += OnCameraPropertyChanged;
                StartUpdateSnapshot(true);
                OnRotationChanged();
                if (device.CameraInfo.LastFrame != null)
                {
                    CameraImage.Source = device.CameraInfo.LastFrame as ImageSource;
                }
            }
        }

        private void ShowStaticImage(string appxUri)
        {
            // Load some static image to show instead of camera snapshot - used in error cases.
            CameraInfo info = device.CameraInfo;
            CameraImage.Source = new BitmapImage(new Uri(appxUri));

            // make sure static image has full background in case it is partially transparent.
            ErrorBorder.Background = new SolidColorBrush(Color.FromArgb(0xC0, 0, 0, 0));
        }

        private void OnRotationChanged()
        {
            int rotation = device.CameraInfo.Rotation;
            if (rotation > 0)
            {
                CameraImage.RenderTransform = new RotateTransform() { Angle = rotation, CenterX = CameraImage.Width / 2, CenterY = CameraImage.Height / 2 };
            }
            else
            {
                CameraImage.RenderTransform = null;
            }
        }

        DispatcherTimer updateTimer;

        void StartUpdateSnapshot(bool imediate)
        {
            if (updateTimer == null)
            {
                updateTimer = new DispatcherTimer();
                updateTimer.Interval = TimeSpan.FromMilliseconds(1000);
                updateTimer.Tick += UpdateSnapshot;
            }
            if (imediate)
            {
                UpdateSnapshot(this, null);
            }
            updateTimer.Start();
        }

        bool unloaded;

        async void UpdateSnapshot(object sender, object args)
        {
            updateTimer.Stop();

            if (device != null)
            {
                var info = device.CameraInfo;
                if (info.Rebooting || info.UpdatingFirmware)
                {
                    ShowStaticImage("ms-appx:/Assets/Gear.png");
                }
                else if (info.StaticImageUrl != null)
                {
                    ShowStaticImage(info.StaticImageUrl);
                    ShowError(info.StaticError);
                }
                else
                {
                    await device.GetSnapshot();
                }
            }
            if (!unloaded)
            {
                updateTimer.Start();
            }
        }

        void StopUpdateTimer()
        {
            if (updateTimer != null)
            {
                updateTimer.Stop();
            }
        }


        void OnCameraPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "UserName":
                case "Password":
                case "Fps":
                    StartUpdateSnapshot(false);
                    break;
                case "Flipped":
                    OnRotationChanged();
                    break;
                case "LastPingTime":
                    if (ErrorBorder.Visibility == Windows.UI.Xaml.Visibility.Visible && lastFrameTime + 1000 < device.CameraInfo.LastPingTime)
                    {
                        StartUpdateSnapshot(false);
                    }
                    break;
                case "StaticImageUrl":
                    ShowStaticImage(device.CameraInfo.StaticImageUrl);
                    break;
                case "StaticError":
                    ShowError(device.CameraInfo.StaticError);
                    break;
                case "Unauthorized":
                    CheckUnauthorized();
                    break;
            }
            
        }

        int lastFrameTime;

        void OnFrameAvailable(object sender, FrameReadyEventArgs e)
        {
            if (e.BitmapSource == null)
            {
                CheckUnauthorized();
            }
            else
            {
                lastFrameTime = Environment.TickCount;
                HideError();
                CameraImage.Source = e.BitmapSource;
                device.CameraInfo.LastFrame = e.BitmapSource;
            }
        }

        void CheckUnauthorized()
        {
            if (device != null && device.CameraInfo.Unauthorized)
            {
                var nowait = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(new Action(() =>
                {
                    CameraImage.Source = new BitmapImage(new Uri("ms-appx:/Assets/Padlock.png", UriKind.RelativeOrAbsolute));
                })));
            }
        }

        void OnDeviceError(object sender, ErrorEventArgs e)
        {
            if (e.HttpResponse == System.Net.HttpStatusCode.Unauthorized)
            {
                if (device != null)
                {
                    device.CameraInfo.Unauthorized = true;
                }
            }
            else if (e.HttpResponse == System.Net.HttpStatusCode.ServiceUnavailable && lastFrameTime != 0 && lastFrameTime + 1000 < Environment.TickCount)
            {
                // one retry - this handles the case where the computer comes out of sleep since we get no other events telling us this.
                lastFrameTime = Environment.TickCount;
                StartUpdateSnapshot(false);
            }
            else
            {
                ShowError(e.Message);
            }
        }

        void ShowError(string text)
        {
            ErrorBorder.Visibility = Windows.UI.Xaml.Visibility.Visible;
            ErrorMessage.Text = "" + text;
        }

        void HideError()
        {
            ErrorBorder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            ErrorMessage.Text = "";
        }

    }
}
